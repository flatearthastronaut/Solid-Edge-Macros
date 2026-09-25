Imports System.Drawing
Imports System.Windows.Forms

Friend Class EdgeBarForm

    Inherits System.Windows.Forms.Form

    Private pDocument As SolidEdgeFramework.SolidEdgeDocument
    Private pEdgeBarPageHandle As Integer

    Private Sub EdgeBarForm_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated

    End Sub


    Private Sub EdgeBarForm_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed

        Initialize(Nothing, Nothing)

    End Sub

    Private Sub EdgeBarForm_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

    End Sub

    Private Sub EdgeBarForm_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize

        ' Edge will send the edge bar client window a resize message when the user resizes the edgebar docking frame.
        ' Add any code here needed to move your controls around if so desired.

    End Sub
    Public Sub Initialize(ByRef Addin As SolidEdgeFramework.AddIn, ByRef theDocument As Object) ' 20/10/2005 DT
        ' Have to release the com wrapper object so it can be GC'd.
        If Not pDocument Is Nothing Then
            System.Runtime.InteropServices.Marshal.ReleaseComObject(pDocument)
        End If

        ' Input may be "nothing", which is how the final reference to the Object is removed.
        pDocument = theDocument

    End Sub

    Public Property EdgeBarPageHandle() As Integer
        Get
            On Error Resume Next
            EdgeBarPageHandle = pEdgeBarPageHandle
        End Get
        Set(ByVal Value As Integer)
            On Error Resume Next
            pEdgeBarPageHandle = Value
        End Set
    End Property

    ' Document property is used to detect if a particular document com wrapper is referenced by any
    ' edge bar page.
    Public Property Document() As Object
        Get
            On Error Resume Next
            Document = pDocument
        End Get
        Set(ByVal value As Object)
            pDocument = value
        End Set
    End Property

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        MsgBox("Edgebar Button1 clicked")
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub
End Class