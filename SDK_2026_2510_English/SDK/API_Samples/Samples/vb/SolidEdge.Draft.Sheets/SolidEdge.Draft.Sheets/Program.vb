' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
' ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
' PARTICULAR PURPOSE.

' Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

Imports SolidEdgeDraft
Imports System.Text
Imports System.Runtime.InteropServices

Friend Class Program
	<STAThread> _
	Shared Sub Main(ByVal args() As String)
		Dim application As SolidEdgeFramework.Application = Nothing
		Dim documents As SolidEdgeFramework.Documents = Nothing
		Dim draftDocument As SolidEdgeDraft.DraftDocument = Nothing
		Dim sheets As SolidEdgeDraft.Sheets = Nothing
		Dim sheet As SolidEdgeDraft.Sheet = Nothing
		Dim sections As SolidEdgeDraft.Sections = Nothing
		Dim section As SolidEdgeDraft.Section = Nothing

		Try
			Console.WriteLine("Registering OleMessageFilter.")

			' Register with OLE to handle concurrency issues on the current thread.
			OleMessageFilter.Register()

			Console.WriteLine("Connecting to Solid Edge.")

			' Connect to or start Solid Edge.
			application = ConnectToSolidEdge(True)

			' Make sure user can see the GUI.
			application.Visible = True

			' Bring Solid Edge to the foreground.
			application.Activate()

			' Get a reference to the documents collection.
			documents = application.Documents

			' Note: these two will throw exceptions if no document is open.
			'application.ActiveDocument
			'application.ActiveDocumentType;

			If (documents.Count > 0) AndAlso (application.ActiveDocumentType = SolidEdgeFramework.DocumentTypeConstants.igDraftDocument) Then
				' Get a reference to the documents collection.
				draftDocument = CType(application.ActiveDocument, SolidEdgeDraft.DraftDocument)

				' Get a reference to the sheets collection.
				sheets = draftDocument.Sheets

				' Get a reference to the active sheet.
				sheet = draftDocument.ActiveSheet
				Console.WriteLine("DraftDocument.ActiveSheet: {0}", sheet.Name)

				' Get a reference to all sections.
				sections = draftDocument.Sections

				' Get a reference to the active section.
				section = draftDocument.ActiveSection
				Console.WriteLine("DraftDocument.ActiveSection: {0}", section.Type)

				Console.WriteLine()
				ProcessSheets(draftDocument.Sheets)
				Console.WriteLine()
				ProcessSections(draftDocument.Sections)
				Console.WriteLine()
				ProcessWorkingSectionDrawingViews(sections.WorkingSection)
				Console.WriteLine()
			Else
				Console.WriteLine("Draft file not open.")
			End If
		Catch ex As System.Exception
#If DEBUG Then
			System.Diagnostics.Debugger.Break()
#End If
			Console.WriteLine(ex.Message)
		Finally
			Console.WriteLine("Unregistering OleMessageFilter.")
			OleMessageFilter.Revoke()
		End Try
	End Sub

	Private Shared Sub ProcessSheets(ByVal sheets As SolidEdgeDraft.Sheets)
		Console.WriteLine("--> {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())

		Dim sheet As SolidEdgeDraft.Sheet = Nothing

		For i As Integer = 1 To sheets.Count
			sheet = sheets.Item(i)
			Console.WriteLine(vbTab & "Sheet.Name: {0}", sheet.Name)
		Next i

		Console.WriteLine("<-- {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())
	End Sub

	Private Shared Sub ProcessSections(ByVal sections As SolidEdgeDraft.Sections)
		Console.WriteLine("--> {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())

		Dim section As SolidEdgeDraft.Section = Nothing

		For i As Integer = 1 To sections.Count
			section = sections.Item(i)
			Console.WriteLine(String.Format(vbTab & "Section.Type: {0}", section.Type))

			ProcessSectionSheets(section.Sheets)
		Next i

		Console.WriteLine("<-- {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())
	End Sub

	Private Shared Sub ProcessSectionSheets(ByVal sectionSheets As SolidEdgeDraft.SectionSheets)
		Dim sheet As SolidEdgeDraft.Sheet = Nothing

		For i As Integer = 1 To sectionSheets.Count
			sheet = sectionSheets.Item(i)
			Console.WriteLine(vbTab & "Sheet.Name: {0}", sheet.Name)
		Next i
	End Sub

	Private Shared Sub ProcessWorkingSectionDrawingViews(ByVal section As SolidEdgeDraft.Section)
		Console.WriteLine("--> {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())

		Dim sectionSheets As SolidEdgeDraft.SectionSheets = Nothing
		Dim sheet As SolidEdgeDraft.Sheet = Nothing
		Dim drawingViews As SolidEdgeDraft.DrawingViews = Nothing
		Dim drawingView As SolidEdgeDraft.DrawingView = Nothing

		If section.Type = SolidEdgeDraft.SheetSectionTypeConstants.igWorkingSection Then
			sectionSheets = section.Sheets

			For i As Integer = 1 To sectionSheets.Count
				sheet = sectionSheets.Item(i)
				Console.WriteLine(vbTab & "Sheet.Name: {0}", sheet.Name)

				drawingViews = sheet.DrawingViews
				For j As Integer = 1 To sectionSheets.Count
					drawingView = drawingViews.Item(j)
					Console.WriteLine(vbTab & "DrawingView.Name: {0}", drawingView.Name)

					ProcessDrawingViewModelLink(drawingView)
					ProcessDrawingViewModelMembers(drawingView)
				Next j
			Next i
		Else
			Console.WriteLine("Section '{0}' is not an igWorkingSection.", section)
		End If

		Console.WriteLine("<-- {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())
	End Sub

	Private Shared Sub ProcessDrawingViewModelLink(ByVal drawingView As SolidEdgeDraft.DrawingView)
		Console.WriteLine("--> {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())

		Dim modelLink As SolidEdgeDraft.ModelLink = Nothing
		Dim solidEdgeDocument As SolidEdgeFramework.SolidEdgeDocument = Nothing

		Try
			' drawingView.ModelLink will throw exception if link is not found.
			modelLink = TryCast(drawingView.ModelLink, SolidEdgeDraft.ModelLink)
			Console.WriteLine(vbTab & "ModelLink.FileName: {0}", modelLink.FileName)

			solidEdgeDocument = TryCast(modelLink.ModelDocument, SolidEdgeFramework.SolidEdgeDocument)

			If solidEdgeDocument IsNot Nothing Then
				Console.WriteLine(vbTab & "ModelDocument.Type: {0}", solidEdgeDocument.Type)
				Select Case solidEdgeDocument.Type
					Case SolidEdgeFramework.DocumentTypeConstants.igAssemblyDocument
					Case SolidEdgeFramework.DocumentTypeConstants.igPartDocument
					Case SolidEdgeFramework.DocumentTypeConstants.igSheetMetalDocument
				End Select
			End If
		Catch
			Console.WriteLine(vbTab & "ModelLink.FileName: Not found")
		End Try

		Console.WriteLine("<-- {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())
	End Sub

	Private Shared Sub ProcessDrawingViewModelMembers(ByVal drawingView As SolidEdgeDraft.DrawingView)
		Console.WriteLine("--> {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())

		ProcessModelMembers(drawingView.ModelMembers, 1)

		Console.WriteLine("<-- {0}", System.Reflection.MethodBase.GetCurrentMethod().ToString())
	End Sub

	''' <summary>
	''' Recursive function to traverse ModelMembers.
	''' </summary>
	Private Shared Sub ProcessModelMembers(ByVal modelMembers As SolidEdgeDraft.ModelMembers, ByVal level As Integer)
		Dim indent As New String(ControlChars.Tab, level)

		Dim modelMember As SolidEdgeDraft.ModelMember = Nothing

		For i As Integer = 1 To modelMembers.Count
			modelMember = modelMembers.Item(i)
			Console.WriteLine("{0}ModelMember: '{1}' ({2})", indent, modelMember.ComponentName, modelMember.ComponentType)

			ProcessModelMembers(modelMember.ModelMembers, level + 1)
		Next i
	End Sub

	''' <summary>
	''' Connects to a running instance of Solid Edge.
	''' </summary>
	Private Shared Function ConnectToSolidEdge() As SolidEdgeFramework.Application
		Return ConnectToSolidEdge(False)
	End Function

	''' <summary>
	''' Connects to a running instance of Solid Edge with an option to start if not running.
	''' </summary>
	Private Shared Function ConnectToSolidEdge(ByVal startIfNotRunning As Boolean) As SolidEdgeFramework.Application
		Try
			' Attempt to connect to a running instance of Solid Edge.
			Return CType(Marshal.GetActiveObject("SolidEdge.Application"), SolidEdgeFramework.Application)
		Catch ex As System.Runtime.InteropServices.COMException
			' Failed to connect.
			If ex.ErrorCode = -2147221021 Then ' MK_E_UNAVAILABLE
				If startIfNotRunning Then
					' Start Solid Edge.
					Return CType(Activator.CreateInstance(Type.GetTypeFromProgID("SolidEdge.Application")), SolidEdgeFramework.Application)
				Else
					Throw New System.Exception("Solid Edge is not running.")
				End If
			Else
				Throw
			End If
		Catch
			Throw
		End Try
	End Function
End Class


