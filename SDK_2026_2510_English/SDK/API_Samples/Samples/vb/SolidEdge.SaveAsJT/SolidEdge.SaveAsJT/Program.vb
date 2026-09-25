' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
' ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
' PARTICULAR PURPOSE.

' Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks

Friend Class Program
	<STAThread> _
	Shared Sub Main(ByVal args() As String)
		Dim application As SolidEdgeFramework.Application = Nothing
		Dim solidEdgeDocument As SolidEdgeFramework.SolidEdgeDocument = Nothing

		Try
			Console.WriteLine("Registering OleMessageFilter.")

			' Register with OLE to handle concurrency issues on the current thread.
			OleMessageFilter.Register()

			Console.WriteLine("Connecting to Solid Edge.")

			' Connect to or start Solid Edge.
			application = ConnectToSolidEdge(False)

			' Make sure user can see the GUI.
			application.Visible = True

			' Bring Solid Edge to the foreground.
			application.Activate()

			' Get a reference to the documents collection.
			solidEdgeDocument = CType(application.ActiveDocument, SolidEdgeFramework.SolidEdgeDocument)

			Console.WriteLine("Determining document type.")

			Select Case solidEdgeDocument.Type
				Case SolidEdgeFramework.DocumentTypeConstants.igAssemblyDocument, SolidEdgeFramework.DocumentTypeConstants.igPartDocument, SolidEdgeFramework.DocumentTypeConstants.igSheetMetalDocument, SolidEdgeFramework.DocumentTypeConstants.igWeldmentAssemblyDocument, SolidEdgeFramework.DocumentTypeConstants.igWeldmentDocument
					SaveAsJT(solidEdgeDocument)
				Case Else
					Console.WriteLine("'{0}' cannot be converted to JT.", solidEdgeDocument.Type)
			End Select
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


	Private Shared Sub SaveAsJT(ByVal solidEdgeDocument As SolidEdgeFramework.SolidEdgeDocument)
		' Note: Some of the parameters are obvious by their name but we need to work on getting better descriptions for some.
		Dim NewName = Path.ChangeExtension(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), solidEdgeDocument.Name), ".jt")
		Dim Include_PreciseGeom = 0
		Dim Prod_Structure_Option = 1
		Dim Export_PMI = 0
		Dim Export_CoordinateSystem = 0
		Dim Export_3DBodies = 0
		Dim NumberofLODs = 1
		Dim JTFileUnit = 0
		Dim Write_Which_Files = 1
		Dim Use_Simplified_TopAsm = 0
		Dim Use_Simplified_SubAsm = 0
		Dim Use_Simplified_Part = 0
		Dim EnableDefaultOutputPath = 0
		Dim IncludeSEProperties = 0
		Dim Export_VisiblePartsOnly = 0
		Dim Export_VisibleConstructionsOnly = 0
		Dim RemoveUnsafeCharacters = 0
		Dim ExportSEPartFileAsSingleJTFile = 0

		Console.WriteLine("Saving '{0}'", NewName)

		solidEdgeDocument.SaveAsJT(NewName, Include_PreciseGeom, Prod_Structure_Option, Export_PMI, Export_CoordinateSystem, Export_3DBodies, NumberofLODs, JTFileUnit, Write_Which_Files, Use_Simplified_TopAsm, Use_Simplified_SubAsm, Use_Simplified_Part, EnableDefaultOutputPath, IncludeSEProperties, Export_VisiblePartsOnly, Export_VisibleConstructionsOnly, RemoveUnsafeCharacters, ExportSEPartFileAsSingleJTFile)
	End Sub

	''' <summary>
	''' Connects to a running instance of Solid Edge.
	''' </summary>
	Public Shared Function ConnectToSolidEdge() As SolidEdgeFramework.Application
		Return ConnectToSolidEdge(False)
	End Function

	''' <summary>
	''' Connects to a running instance of Solid Edge with an option to start if not running.
	''' </summary>
	Public Shared Function ConnectToSolidEdge(ByVal startIfNotRunning As Boolean) As SolidEdgeFramework.Application
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


