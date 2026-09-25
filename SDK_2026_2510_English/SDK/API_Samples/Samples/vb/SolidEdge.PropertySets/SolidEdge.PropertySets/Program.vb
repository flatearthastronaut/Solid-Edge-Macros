' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
' ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
' PARTICULAR PURPOSE.

' Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

Imports System.Runtime.InteropServices
Imports System.Text

Friend Class Program
	<STAThread> _
	Shared Sub Main(ByVal args() As String)
		Dim application As SolidEdgeFramework.Application = Nothing
		Dim documents As SolidEdgeFramework.Documents = Nothing
		Dim document As SolidEdgeFramework.SolidEdgeDocument = Nothing
		Dim propertySets As SolidEdgeFramework.PropertySets = Nothing

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

			' Note: these two will throw exceptions if no document is open.
			'application.ActiveDocument
			'application.ActiveDocumentType;

			If documents.Count > 0 Then
				' Get a reference to the documents collection.
				document = CType(application.ActiveDocument, SolidEdgeFramework.SolidEdgeDocument)
			Else
				Throw New System.Exception("No document open.")
			End If

			propertySets = CType(document.Properties, SolidEdgeFramework.PropertySets)

			ProcessPropertySets(propertySets)

			AddCustomProperties(propertySets)
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

	Private Shared Sub ProcessPropertySets(ByVal propertySets As SolidEdgeFramework.PropertySets)
		Dim properties As SolidEdgeFramework.Properties = Nothing

		For i As Integer = 1 To propertySets.Count
			properties = propertySets.Item(i)

			Console.WriteLine("PropertSet '{0}'.", properties.Name)

			ProcessProperties(properties)
		Next i
	End Sub

	Private Shared Sub ProcessProperties(ByVal properties As SolidEdgeFramework.Properties)
		'SolidEdgeFramework.Property property = null;
		Dim [property] As Object = Nothing ' Using dynamic so that property.Value works.

		For i As Integer = 1 To properties.Count
			Dim nativePropertyType As System.Runtime.InteropServices.VarEnum = System.Runtime.InteropServices.VarEnum.VT_EMPTY
			Dim runtimePropertyType As Type = Nothing

			Dim value As Object = Nothing

			Try
				[property] = properties.Item(i)
				nativePropertyType = CType([property].Type, System.Runtime.InteropServices.VarEnum)

				' May throw an exception...
				value = [property].Value

				If value IsNot Nothing Then
					runtimePropertyType = value.GetType()
				End If
			Catch ex As System.Exception
				value = ex.Message
			End Try

			Console.WriteLine(vbTab & "{0} = '{1}' ({2} | {3}).", [property].Name, value, nativePropertyType, runtimePropertyType)
		Next i
	End Sub

	Private Shared Sub AddCustomProperties(ByVal propertySets As SolidEdgeFramework.PropertySets)
		Dim properties As SolidEdgeFramework.Properties = Nothing

		properties = propertySets.Item("Custom")

		Console.WriteLine("Adding custom properties.")

		properties.Add("My String", "Hello world!")
		properties.Add("My Integer", 338)
		properties.Add("My Boolean", True)
		properties.Add("My DateTime", Date.Now)
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


