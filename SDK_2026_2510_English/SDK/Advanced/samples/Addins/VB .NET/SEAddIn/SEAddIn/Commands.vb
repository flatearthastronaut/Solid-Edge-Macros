Friend Class Commands

    Implements IDisposable

    Public m_SEapp As SolidEdgeFramework.Application
    Public m_addinEvents As SolidEdgeFramework.ISEAddInEvents_Event
    Public m_myAddIn As SolidEdgeFramework.AddIn

#Region "EVENTS"
    Public Sub AddOrRemoveEventHandlers(ByVal Add As Boolean)
        ' 4/30/8 RDH - I removed "WithEvents" and am directly adding handlers for the events (Microsoft suggestion to
        ' help avoid leaks that lead to crashes). It is absolutely required to call RemoveHandler for any event that 
        ' was added. Failure to remove each one added will result in a memory leak and that leak will get cleaned up
        ' at shutdown causing problems too.
        If Add Then
            ' Add the handlers for events we want to respond to.
            If Not m_addinEvents Is Nothing Then

                ' Must be a corresponding RemoveHandler for each call to AddHandler.
                AddHandler m_addinEvents.OnCommand, AddressOf m_addinEvents_OnCommand
                AddHandler m_addinEvents.OnCommandHelp, AddressOf m_addinEvents_OnCommandHelp
                AddHandler m_addinEvents.OnCommandUpdateUI, AddressOf m_addinEvents_OnCommandUpdateUI
            End If
        Else
            ' Remove the handlers as we are done responding to events
            If Not m_addinEvents Is Nothing Then

                ' The corresponding RemoveHandler calls.
                RemoveHandler m_addinEvents.OnCommand, AddressOf m_addinEvents_OnCommand
                RemoveHandler m_addinEvents.OnCommandHelp, AddressOf m_addinEvents_OnCommandHelp
                RemoveHandler m_addinEvents.OnCommandUpdateUI, AddressOf m_addinEvents_OnCommandUpdateUI

                ' Must call ReleaseComObject now or GC will do so at shutdown as DLLs are unloading, which is
                ' a major problem since there is no control over the order DLLs are unloaded and that can lead
                ' to exceptions that may or may not be caught by the .NET runtime.
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(m_addinEvents)
                m_addinEvents = Nothing
            End If
        End If
    End Sub
    Public Sub m_addinEvents_OnCommand(ByVal nCmdID As Integer)

        On Error Resume Next ' activedocument can throw exception if no doc is open
        Select Case nCmdID
            Case 1
                MsgBox("Sample VB Addin command 1 invoked")

                ' Start a command if and only if a document is opened.
                Dim Doc As SolidEdgeFramework.SolidEdgeDocument

                Doc = m_SEapp.ActiveDocument

                If Not Doc Is Nothing Then
                    Dim Cmd As Command

                    Cmd = New Command

                    If Not Cmd Is Nothing Then
                        Cmd.CreateCommand(m_SEapp)
                        Cmd.Start()
                    End If

                    ' No final release call here as other .net clients might have the doc too!
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(Doc)
                End If

            Case 2

                MsgBox("Sample VB Addin command 2 invoked")
        End Select

    End Sub

    Public Sub m_addinEvents_OnCommandHelp(ByVal hFrameWnd As Integer, ByVal uHelpCommand As Integer, ByVal nCmdID As Integer)
        MsgBox("OnCommandHelp event")
    End Sub

    Public Sub m_addinEvents_OnCommandUpdateUI(ByVal nCmdID As Integer, ByRef lCmdFlags As Integer, ByRef MenuItemText As String, ByRef nIDBitmap As Integer)
        ' Called a lot ...
        'MsgBox "OnCommandUpdateUI event"
    End Sub
#End Region

    Private disposedValue As Boolean = False        ' To detect redundant calls

    ' IDisposable
    ' 4/30/8 RDH - I implement Dispose at Microsoft's suggestion as this is the .NET way to make sure
    ' "resources" are released in a timely fashion. In this case, the issue we have is that Finalize
    ' is running when GC runs when the app shuts down and by then it is too late to start releasing
    ' edge objects since all the DLLs are unloading. Unloading is not ordered and anytime a .NET object
    ' tries to release the com object it holds, the DLL serving the com object may already be unloaded
    ' and there is an access violation the .NET runtime may or may not catch.
    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        On Error Resume Next

        If Not Me.disposedValue Then
            If disposing Then
                If Not m_SEapp Is Nothing Then
                    ' No final release call here as other .net clients might have the app too!
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(m_SEapp)
                    m_SEapp = Nothing
                End If

                AddOrRemoveEventHandlers(False)

                If Not m_myAddIn Is Nothing Then
                    ' No final release call here as other .net clients might have the addin interface too!
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(m_myAddIn)
                    m_myAddIn = Nothing
                End If
                ' TODO: free managed resources when explicitly called
            End If

            ' TODO: free shared unmanaged resources
        End If
        Me.disposedValue = True
    End Sub

#Region " IDisposable Support "
    ' This code added by Visual Basic to correctly implement the disposable pattern.
    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class