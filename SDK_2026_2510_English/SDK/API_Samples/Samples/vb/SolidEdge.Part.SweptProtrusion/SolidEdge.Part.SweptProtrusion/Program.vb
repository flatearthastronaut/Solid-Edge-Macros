' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
' ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
' PARTICULAR PURPOSE.

' Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Console application that demonstrates how to create a swept protrusion.
''' </summary>
Friend Class Program
	<STAThread> _
	Shared Sub Main(ByVal args() As String)
		Dim application As SolidEdgeFramework.Application = Nothing
		Dim documents As SolidEdgeFramework.Documents = Nothing
		Dim partDocument As SolidEdgePart.PartDocument = Nothing
		Dim models As SolidEdgePart.Models = Nothing
		Dim model As SolidEdgePart.Model = Nothing
		Dim sketches As SolidEdgePart.Sketchs = Nothing
		Dim sketch As SolidEdgePart.Sketch = Nothing
		Dim refPlanes As SolidEdgePart.RefPlanes = Nothing
		Dim refPlane As SolidEdgePart.RefPlane = Nothing
		Dim profileSets As SolidEdgePart.ProfileSets = Nothing
		Dim profileSet As SolidEdgePart.ProfileSet = Nothing
		Dim profiles As SolidEdgePart.Profiles = Nothing
		Dim sketchProfile As SolidEdgePart.Profile = Nothing
		Dim profile As SolidEdgePart.Profile = Nothing
		Dim circles2d As SolidEdgeFrameworkSupport.Circles2d = Nothing

		Dim listPaths As New List(Of SolidEdgePart.Profile)()
		Dim listPathTypes As New List(Of SolidEdgePart.FeaturePropertyConstants)()
		Dim listSections As New List(Of SolidEdgePart.Profile)()
		Dim listSectionTypes As New List(Of SolidEdgePart.FeaturePropertyConstants)()
		Dim listOrigins As New List(Of Integer)()

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

			' Get a reference to the Documents collection.
			documents = application.Documents

			Console.WriteLine("Creating a new part document.")

			' Create a new PartDocument.
			partDocument = CType(documents.Add("SolidEdge.PartDocument"), SolidEdgePart.PartDocument)

			' Always a good idea to give SE a chance to breathe.
			application.DoIdle()

			Console.WriteLine("Drawing part.")

			' Get a reference to the models collection.
			models = CType(partDocument.Models, SolidEdgePart.Models)

			' Get a reference to the Sketches collections.
			sketches = CType(partDocument.Sketches, SolidEdgePart.Sketchs)

			' Get a reference to the profile sets collection.
			profileSets = CType(partDocument.ProfileSets, SolidEdgePart.ProfileSets)

			' Get a reference to the ref planes collection.
			refPlanes = CType(partDocument.RefPlanes, SolidEdgePart.RefPlanes)

			' Front (xz).
			refPlane = CType(refPlanes.Item(3), SolidEdgePart.RefPlane)

			' Add a new sketch.
			sketch = CType(sketches.Add(), SolidEdgePart.Sketch)

			' Add profile for sketch on specified refplane.
			sketchProfile = sketch.Profiles.Add(refPlane)

			' Get a reference to the Circles2d collection.
			circles2d = sketchProfile.Circles2d

			' Draw the Base Profile.
			circles2d.AddByCenterRadius(0, 0, 0.02)

			' Close the profile.
			sketchProfile.End(SolidEdgePart.ProfileValidationType.igProfileClosed)

			' Arrays for AddSweptProtrusion().
			listPaths.Add(sketchProfile)
			listPathTypes.Add(SolidEdgePart.FeaturePropertyConstants.igProfileBasedCrossSection)

			' NOTE: profile is the Curve.
			refPlane = refPlanes.AddNormalToCurve(sketchProfile, SolidEdgePart.ReferenceElementConstants.igCurveEnd, refPlanes.Item(3), SolidEdgePart.ReferenceElementConstants.igPivotEnd, True, System.Reflection.Missing.Value)

			' Add a new profile set.
			profileSet = CType(profileSets.Add(), SolidEdgePart.ProfileSet)

			' Get a reference to the profiles collection.
			profiles = CType(profileSet.Profiles, SolidEdgePart.Profiles)

			' add a new profile.
			profile = CType(profiles.Add(refPlane), SolidEdgePart.Profile)

			' Get a reference to the Circles2d collection.
			circles2d = profile.Circles2d

			' Draw the Base Profile.
			circles2d.AddByCenterRadius(0, 0, 0.01)

			' Close the profile.
			profile.End(SolidEdgePart.ProfileValidationType.igProfileClosed)

			' Arrays for AddSweptProtrusion().
			listSections.Add(profile)
			listSectionTypes.Add(SolidEdgePart.FeaturePropertyConstants.igProfileBasedCrossSection)
			listOrigins.Add(0) 'Use 0 for closed profiles.

			Console.WriteLine("Creating swept protrusion.")

			' Create the extended protrusion.
			model = models.AddSweptProtrusion(listPaths.Count, listPaths.ToArray(), listPathTypes.ToArray(), listSections.Count, listSections.ToArray(), listSectionTypes.ToArray(), listOrigins.ToArray(), 0, SolidEdgePart.FeaturePropertyConstants.igLeft, SolidEdgePart.FeaturePropertyConstants.igNone, 0.0, Nothing, SolidEdgePart.FeaturePropertyConstants.igNone, 0.0, Nothing)

			' Hide profiles.
			sketchProfile.Visible = False
			profile.Visible = False

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


