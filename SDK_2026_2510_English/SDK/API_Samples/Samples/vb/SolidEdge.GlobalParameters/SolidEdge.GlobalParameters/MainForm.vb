Imports System.ComponentModel
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Reflection
Imports System.Text.RegularExpressions

Partial Public Class MainForm
	Inherits Form

	Private _application As SolidEdgeFramework.Application = Nothing
	Private _textToolStripTextBox As ToolStripSpringTextBox = Nothing

	Public Sub New()
		InitializeComponent()
	End Sub

	Private Sub MainForm_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
		booleanToolStripComboBox.Visible = False


		_textToolStripTextBox = New ToolStripSpringTextBox()
		_textToolStripTextBox.Visible = False

		Dim index As Integer = toolStrip1.Items.IndexOf(textToolStripTextBox)
		toolStrip1.Items.Remove(textToolStripTextBox)
		textToolStripTextBox = Nothing
		toolStrip1.Items.Insert(index, _textToolStripTextBox)

		booleanToolStripComboBox.Items.AddRange(New Object() { True, False })

		Dim methodInvoker As New MethodInvoker(AddressOf RefreshGlobalParametersListView)
		BeginInvoke(methodInvoker)
	End Sub

	Private Sub MainForm_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles MyBase.FormClosing
		If _application IsNot Nothing Then
			OleMessageFilter.Revoke()
		End If
	End Sub

	Private Sub exitToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) Handles exitToolStripMenuItem.Click
		Close()
	End Sub

	Private Sub refreshButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles refreshButton.Click
		Cursor.Current = Cursors.WaitCursor

		Try
			RefreshGlobalParametersListView()
		Catch ex As System.Exception
			MessageBox.Show(Me, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
		End Try

		Cursor.Current = Cursors.Default

	End Sub

	Private Sub editButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles editButton.Click
		Dim listViewItem As ListViewItem = lvGlobalParameters.SelectedItem

		Try
			If listViewItem IsNot Nothing Then
				Dim globalConstant As SolidEdgeFramework.ApplicationGlobalConstants = CType(listViewItem.Tag, SolidEdgeFramework.ApplicationGlobalConstants)
				Dim valueType As Type = TryCast(listViewItem.SubItems(2).Tag, Type)
				Dim globalValue As Object = Nothing
				Dim globalValueType As Type = Nothing

				If GetType(Boolean).Equals(valueType) Then
					globalValue = booleanToolStripComboBox.SelectedItem
				ElseIf GetType(String).Equals(valueType) Then
					globalValue = _textToolStripTextBox.Text
				ElseIf GetType(Integer).Equals(valueType) Then
					globalValue = Integer.Parse(_textToolStripTextBox.Text)
				End If

				If globalValue IsNot Nothing Then
					_application.SetGlobalParameter(globalConstant, globalValue)

					globalValueType = globalValue.GetType()

					listViewItem.SubItems(1).Text = String.Format("{0}", globalValue)
					listViewItem.SubItems(1).Tag = globalValue
					listViewItem.SubItems(2).Text = String.Format("{0}", globalValueType)
					listViewItem.SubItems(2).Tag = globalValueType
				End If
			End If
		Catch ex As System.Exception
			MessageBox.Show(Me, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
		End Try

		lvGlobalParameters.SelectedItem = listViewItem
	End Sub

	Private Function GetAllApplicationGlobalConstants() As SolidEdgeFramework.ApplicationGlobalConstants()
		Dim list As New List(Of SolidEdgeFramework.ApplicationGlobalConstants)()
		Dim fieldInfos() As FieldInfo = GetType(SolidEdgeFramework.ApplicationGlobalConstants).GetFields()

		For Each fieldInfo As FieldInfo In fieldInfos
			If fieldInfo.IsSpecialName Then
				Continue For
			End If
			list.Add(CType(fieldInfo.GetRawConstantValue(), SolidEdgeFramework.ApplicationGlobalConstants))
		Next fieldInfo

		Return list.OrderBy(Function(x) x.ToString()).ToArray()
	End Function

	Private Sub RefreshGlobalParametersListView()
		If lvGlobalParameters.Items.Count = 0 Then
			Dim listViewItems As New List(Of ListViewItem)()
			Dim appGlobalConstants() As SolidEdgeFramework.ApplicationGlobalConstants = GetAllApplicationGlobalConstants()

			For Each appGlobalConstant As SolidEdgeFramework.ApplicationGlobalConstants In appGlobalConstants
				Dim itemValues() As String = { appGlobalConstant.ToString(), String.Empty, String.Empty, String.Format("SolidEdgeFramework.ApplicationGlobalConstants.{0}", appGlobalConstant.ToString()) }

				itemValues(0) = itemValues(0).Replace("seApplicationGlobal", String.Empty).CamelCaseToWordString()

				Dim listViewItem As New ListViewItem(itemValues)
				listViewItem.ImageIndex = 0
				listViewItem.Tag = appGlobalConstant
				listViewItems.Add(listViewItem)
			Next appGlobalConstant

			lvGlobalParameters.Items.AddRange(listViewItems.ToArray())
			lvGlobalParameters.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent)
			lvGlobalParameters.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent)
			lvGlobalParameters.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent)
		End If

		Try
			If _application Is Nothing Then
				' Register with OLE to handle concurrency issues on the current thread.
				OleMessageFilter.Register()

				' Connect to or start Solid Edge.
				_application = ConnectToSolidEdge(True)

				' Ensure Solid Edge GUI is visible.
				_application.Visible = True
			End If

			For Each listViewItem As ListViewItem In lvGlobalParameters.Items
				Dim appGlobalConstant As SolidEdgeFramework.ApplicationGlobalConstants = CType(listViewItem.Tag, SolidEdgeFramework.ApplicationGlobalConstants)

				Dim globalValue As Object = Nothing
				Dim globalValueType As Object = Nothing

				Try
					_application.GetGlobalParameter(appGlobalConstant, globalValue)
				Catch ex As System.Exception
					globalValue = ex
					globalValueType = ex.GetType()
				End Try

				If globalValue IsNot Nothing Then
					If globalValueType Is Nothing Then
						globalValueType = globalValue.GetType()
					End If
				End If

				listViewItem.SubItems(1).Text = String.Format("{0}", globalValue)
				listViewItem.SubItems(1).Tag = globalValue
				listViewItem.SubItems(2).Text = String.Format("{0}", globalValueType)
				listViewItem.SubItems(2).Tag = globalValueType
			Next listViewItem

			lvGlobalParameters.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent)
		Catch ex As System.Exception
#If DEBUG Then
			System.Diagnostics.Debugger.Break()
#End If
			Throw
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

	Private Sub lvGlobalParameters_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles lvGlobalParameters.SelectedIndexChanged
		booleanToolStripComboBox.Visible = False
		_textToolStripTextBox.Visible = False
		editButton.Enabled = False

		Dim item As ListViewItem = lvGlobalParameters.SelectedItem

		If item IsNot Nothing Then
			Dim parameterValue As Object = item.SubItems(1).Tag
			Dim valueType As Type = TryCast(item.SubItems(2).Tag, Type)

			If GetType(Boolean).Equals(valueType) Then
				booleanToolStripComboBox.Visible = True
				booleanToolStripComboBox.SelectedItem = item.SubItems(1).Tag
				editButton.Enabled = True
			ElseIf GetType(String).Equals(valueType) Then
				_textToolStripTextBox.InputType = valueType
				_textToolStripTextBox.Visible = True
				_textToolStripTextBox.Text = parameterValue.ToString()
				editButton.Enabled = True
			ElseIf GetType(Integer).Equals(valueType) Then
				_textToolStripTextBox.InputType = valueType
				_textToolStripTextBox.Visible = True
				_textToolStripTextBox.Text = parameterValue.ToString()
				editButton.Enabled = True
			End If


			Dim exception As System.Exception = TryCast(lvGlobalParameters.SelectedItem.SubItems(1).Tag, System.Exception)
		End If
	End Sub
End Class
