' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
' ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
' PARTICULAR PURPOSE.

' Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

Imports System.Text
Imports System.Runtime.InteropServices

Friend Class Program
	<STAThread> _
	Shared Sub Main(ByVal args() As String)
		Dim application As SolidEdgeFramework.Application = Nothing
		Dim documents As SolidEdgeFramework.Documents = Nothing
		Dim draftDocument As SolidEdgeDraft.DraftDocument = Nothing
		Dim sheet As SolidEdgeDraft.Sheet = Nothing
		Dim balloons As SolidEdgeFrameworkSupport.Balloons = Nothing
		Dim balloon As SolidEdgeFrameworkSupport.Balloon = Nothing
		Dim dimStype As SolidEdgeFrameworkSupport.DimStyle = Nothing

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

			' Create a new draft document.
			draftDocument = CType(documents.Add("SolidEdge.DraftDocument"), SolidEdgeDraft.DraftDocument)

			' Get a reference to the active sheet.
			sheet = draftDocument.ActiveSheet

			' Get a reference to the balloons collection.
			balloons = CType(sheet.Balloons, SolidEdgeFrameworkSupport.Balloons)

			balloon = balloons.Add(0.05, 0.05, 0)
			balloon.TextScale = 1.0
			balloon.BalloonText = "B"
			balloon.Leader = True
			balloon.BreakLine = True
			balloon.BalloonSize = 3
			balloon.SetTerminator(balloon, 0, 0, 0, False)
			balloon.BalloonType = SolidEdgeFrameworkSupport.DimBalloonTypeConstants.igDimBalloonCircle

			dimStype = balloon.Style
			dimStype.TerminatorType = SolidEdgeFrameworkSupport.DimTermTypeConstants.igDimStyleTermFilled

			balloon = balloons.Add(0.1, 0.1, 0)
			balloon.Callout = 1
			balloon.TextScale = 1.0
			balloon.BalloonText = "This is a callout"
			balloon.Leader = True
			balloon.BreakLine = True
			balloon.BalloonSize = 3
			balloon.SetTerminator(balloon, 0, 0, 0, False)
			balloon.BalloonType = SolidEdgeFrameworkSupport.DimBalloonTypeConstants.igDimBalloonCircle

			dimStype = balloon.Style
			dimStype.TerminatorType = SolidEdgeFrameworkSupport.DimTermTypeConstants.igDimStyleTermFilled
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


