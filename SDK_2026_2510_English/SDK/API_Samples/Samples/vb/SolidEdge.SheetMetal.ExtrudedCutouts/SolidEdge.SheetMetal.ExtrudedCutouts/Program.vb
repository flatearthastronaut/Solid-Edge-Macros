' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
' ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
' PARTICULAR PURPOSE.

' Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks

Friend Class Program
	<STAThread> _
	Shared Sub Main(ByVal args() As String)
		Dim application As SolidEdgeFramework.Application = Nothing
		Dim documents As SolidEdgeFramework.Documents = Nothing
		Dim sheetMetalDocument As SolidEdgePart.SheetMetalDocument = Nothing

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
			documents = application.Documents

			Console.WriteLine("Creating a new sheetmetal document.")

			' Create a new sheetmetal document.
			sheetMetalDocument = CType(documents.Add("SolidEdge.SheetMetalDocument"), SolidEdgePart.SheetMetalDocument)

			' Always a good idea to give SE a chance to breathe.
			application.DoIdle()

			' Create geometry.
			CreateExtrudedProtrusion(sheetMetalDocument)

			' Create cutout.
			CreateExtrudedCutout(sheetMetalDocument)

			Console.WriteLine("Switching to ISO view.")

			' Switch to ISO view.
			application.StartCommand(CType(SolidEdgeConstants.PartCommandConstants.PartViewISOView, SolidEdgeFramework.SolidEdgeCommandConstants))
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

	Private Shared Sub CreateExtrudedProtrusion(ByVal sheetMetalDocument As SolidEdgePart.SheetMetalDocument)
		Dim refPlanes As SolidEdgePart.RefPlanes = Nothing
		Dim refPlane As SolidEdgePart.RefPlane = Nothing
		Dim sketchs As SolidEdgePart.Sketchs = Nothing
		Dim sketch As SolidEdgePart.Sketch = Nothing
		Dim profiles As SolidEdgePart.Profiles = Nothing
		Dim profile As SolidEdgePart.Profile = Nothing
		Dim circles2d As SolidEdgeFrameworkSupport.Circles2d = Nothing
		Dim circle2d As SolidEdgeFrameworkSupport.Circle2d = Nothing
		Dim models As SolidEdgePart.Models = Nothing
		Dim model As SolidEdgePart.Model = Nothing

		' Get refplane.
		refPlanes = sheetMetalDocument.RefPlanes
		refPlane = refPlanes.Item(2)

		' Create sketch.
		sketchs = sheetMetalDocument.Sketches
		sketch = sketchs.Add()

		' Create profile.
		profiles = sketch.Profiles
		profile = profiles.Add(refPlane)

		' Create 2D circle.
		circles2d = profile.Circles2d
		circle2d = circles2d.AddByCenterRadius(0, 0, 0.05)

		profile.Visible = False

		' Create extruded protrusion.
		models = sheetMetalDocument.Models
		model = models.AddBaseTab(profile, SolidEdgePart.FeaturePropertyConstants.igRight)
	End Sub

	Private Shared Sub CreateExtrudedCutout(ByVal sheetMetalDocument As SolidEdgePart.SheetMetalDocument)
		Dim refPlanes As SolidEdgePart.RefPlanes = Nothing
		Dim refPlane As SolidEdgePart.RefPlane = Nothing
		Dim sketchs As SolidEdgePart.Sketchs = Nothing
		Dim sketch As SolidEdgePart.Sketch = Nothing
		Dim profiles As SolidEdgePart.Profiles = Nothing
		Dim profile As SolidEdgePart.Profile = Nothing
		Dim circles2d As SolidEdgeFrameworkSupport.Circles2d = Nothing
		Dim circle2d As SolidEdgeFrameworkSupport.Circle2d = Nothing
		Dim models As SolidEdgePart.Models = Nothing
		Dim model As SolidEdgePart.Model = Nothing
		Dim extrudedCutouts As SolidEdgePart.ExtrudedCutouts = Nothing
		Dim extrudedCutout As SolidEdgePart.ExtrudedCutout = Nothing
		Dim profileList As New List(Of SolidEdgePart.Profile)()
		Dim finiteDepth1 As Double = 0.5

		' Get refplane.
		refPlanes = sheetMetalDocument.RefPlanes
		refPlane = refPlanes.Item(2)

		' Create 2nd sketch.
		sketchs = sheetMetalDocument.Sketches
		sketch = sketchs.Add()

		' Create profile.
		profiles = sketch.Profiles
		profile = profiles.Add(refPlane)

		' Create 2D circle.
		circles2d = profile.Circles2d
		circle2d = circles2d.AddByCenterRadius(0, 0, 0.025)

		profile.Visible = False
		profileList.Add(profile)

		models = sheetMetalDocument.Models
		model = models.Item(1)

		extrudedCutouts = model.ExtrudedCutouts
		extrudedCutout = extrudedCutouts.Add(profileList.Count, profileList.ToArray(), SolidEdgePart.FeaturePropertyConstants.igLeft, SolidEdgePart.FeaturePropertyConstants.igFinite, SolidEdgePart.FeaturePropertyConstants.igSymmetric, finiteDepth1, Nothing, SolidEdgePart.KeyPointExtentConstants.igTangentNormal, Nothing, SolidEdgePart.OffsetSideConstants.seOffsetNone, 0, SolidEdgePart.TreatmentTypeConstants.seTreatmentNone, SolidEdgePart.DraftSideConstants.seDraftNone, 0, SolidEdgePart.TreatmentCrownTypeConstants.seTreatmentCrownNone, SolidEdgePart.TreatmentCrownSideConstants.seTreatmentCrownSideNone, SolidEdgePart.TreatmentCrownCurvatureSideConstants.seTreatmentCrownCurvatureNone, 0, 0, SolidEdgePart.FeaturePropertyConstants.igNone, SolidEdgePart.FeaturePropertyConstants.igNone, 0, Nothing, SolidEdgePart.KeyPointExtentConstants.igTangentNormal, Nothing, SolidEdgePart.OffsetSideConstants.seOffsetNone, 0, SolidEdgePart.TreatmentTypeConstants.seTreatmentNone, SolidEdgePart.DraftSideConstants.seDraftNone, 0, SolidEdgePart.TreatmentCrownTypeConstants.seTreatmentCrownNone, SolidEdgePart.TreatmentCrownSideConstants.seTreatmentCrownSideNone, SolidEdgePart.TreatmentCrownCurvatureSideConstants.seTreatmentCrownCurvatureNone, 0, 0)
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


