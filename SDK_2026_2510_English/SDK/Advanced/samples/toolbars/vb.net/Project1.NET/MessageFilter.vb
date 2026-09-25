<System.Runtime.InteropServices.ComImport()> _
<System.Runtime.InteropServices.Guid("00000016-0000-0000-C000-000000000046")> _
<System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface IOleMessageFilter



    <System.Runtime.InteropServices.PreserveSig()> _
Function HandleInComingCall(ByVal dwCallType As Integer, ByVal hTaskCaller As IntPtr, ByVal dwTickCount As Integer, ByVal lpInterfaceInfo As IntPtr) As Integer



    <System.Runtime.InteropServices.PreserveSig()> _
    Function RetryRejectedCall(ByVal hTaskCallee As IntPtr, ByVal dwTickCount As Integer, ByVal dwRejectType As Integer) As Integer



    <System.Runtime.InteropServices.PreserveSig()> _
    Function MessagePending(ByVal hTaskCallee As IntPtr, ByVal dwTickCount As Integer, ByVal dwPendingType As Integer) As Integer



End Interface



    Public Class MessageFilterAPI



    <System.Runtime.InteropServices.DllImportAttribute("ole32.dll")> _
    Public Shared Function CoRegisterMessageFilter(ByVal filt As IOleMessageFilter, ByRef oldFilter As IOleMessageFilter) As Integer

    End Function



Public Shared Sub RegisterMessageFilter()



    Debug.WriteLine(String.Format("RegisterMessageFilter started at {0}", DateTime.Now.ToLongTimeString()))

    System.Threading.Thread.CurrentThread.SetApartmentState(System.Threading.ApartmentState.STA)



    Dim filt As MessageFilterImpl = New MessageFilterImpl()

        Dim oldfilt As IOleMessageFilter = Nothing

    CoRegisterMessageFilter(filt, oldfilt)

End Sub



Public Shared Sub RevokeMessageFilter()



    Dim oldfilt As IOleMessageFilter = Nothing

    CoRegisterMessageFilter(Nothing, oldfilt)

End Sub

    End Class



Public Class MessageFilterImpl

    Implements IOleMessageFilter



    Public Function HandleInComingCall(ByVal dwCallType As Integer, ByVal hTaskCaller As System.IntPtr, ByVal dwTickCount As Integer, ByVal lpInterfaceInfo As System.IntPtr) As Integer Implements IOleMessageFilter.HandleInComingCall

        Debug.WriteLine("SERVERCALL_ISHANDLED")

        Return 0 ' SERVERCALL_ISHANDLED

    End Function



    Public Function MessagePending(ByVal hTaskCallee As System.IntPtr, ByVal dwTickCount As Integer, ByVal dwPendingType As Integer) As Integer Implements IOleMessageFilter.MessagePending

        Debug.WriteLine("PENDINGMSG_WAITDEFPROCESS")

        Return 2 ' PENDINGMSG_WAITDEFPROCESS 

    End Function



    Public Function RetryRejectedCall(ByVal hTaskCallee As System.IntPtr, ByVal dwTickCount As Integer, ByVal dwRejectType As Integer) As Integer Implements IOleMessageFilter.RetryRejectedCall

        If dwRejectType = 2 Then ' SERVERCALL_RETRYLATER

            Debug.WriteLine("Retry call later")

            Return 99 ' retry immediately if return >=0 & <100

        End If

        Return -1 ' cancel call

    End Function

End Class


