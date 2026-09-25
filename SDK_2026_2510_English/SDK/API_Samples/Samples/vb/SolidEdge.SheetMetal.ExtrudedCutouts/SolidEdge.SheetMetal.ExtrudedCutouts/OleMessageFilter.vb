' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
' ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
' PARTICULAR PURPOSE.

' Copyright (c) Siemens Product Lifecycle Management Software Inc. All rights reserved.

Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Threading


Friend Enum SERVERCALL
	SERVERCALL_ISHANDLED = 0
	SERVERCALL_REJECTED = 1
	SERVERCALL_RETRYLATER = 2
End Enum

Friend Enum PENDINGMSG
	PENDINGMSG_CANCELCALL = 0
	PENDINGMSG_WAITNOPROCESS = 1
	PENDINGMSG_WAITDEFPROCESS = 2
End Enum

<ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
Public Interface IOleMessageFilter
	<PreserveSig> _
	Function HandleInComingCall(ByVal dwCallType As Integer, ByVal hTaskCaller As IntPtr, ByVal dwTickCount As Integer, ByVal lpInterfaceInfo As IntPtr) As Integer

	<PreserveSig> _
	Function RetryRejectedCall(ByVal hTaskCallee As IntPtr, ByVal dwTickCount As Integer, ByVal dwRejectType As Integer) As Integer

	<PreserveSig> _
	Function MessagePending(ByVal hTaskCallee As IntPtr, ByVal dwTickCount As Integer, ByVal dwPendingType As Integer) As Integer
End Interface

Public Class OleMessageFilter
	Implements IOleMessageFilter

	''' <summary>
	''' Registers this instance of IMessageFilter interface with OLE to handle concurrency issues on the current thread. 
	''' Only one message filter can be registered for each thread. 
	''' Threads in multithreaded apartments cannot have message filters.
	''' </summary>
	Public Shared Sub Register()
		Dim newFilter As IOleMessageFilter = New OleMessageFilter()
		Dim oldFilter As IOleMessageFilter = Nothing

		If Thread.CurrentThread.GetApartmentState() = ApartmentState.STA Then
			CoRegisterMessageFilter(newFilter, oldFilter)
		Else
			Throw New COMException("Unable to register message filter because the current thread apartment state is not STA.")
		End If
	End Sub

	Public Shared Sub Revoke()
		Dim oldFilter As IOleMessageFilter = Nothing
		CoRegisterMessageFilter(Nothing, oldFilter)
	End Sub

	Private Function IOleMessageFilter_HandleInComingCall(ByVal dwCallType As Integer, ByVal hTaskCaller As System.IntPtr, ByVal dwTickCount As Integer, ByVal lpInterfaceInfo As System.IntPtr) As Integer Implements IOleMessageFilter.HandleInComingCall
		Return CInt(SERVERCALL.SERVERCALL_ISHANDLED)
	End Function

	Private Function IOleMessageFilter_RetryRejectedCall(ByVal hTaskCallee As System.IntPtr, ByVal dwTickCount As Integer, ByVal dwRejectType As Integer) As Integer Implements IOleMessageFilter.RetryRejectedCall
		If dwRejectType = CInt(SERVERCALL.SERVERCALL_RETRYLATER) Then
			Return 99
		End If

		Return -1
	End Function

	Private Function IOleMessageFilter_MessagePending(ByVal hTaskCallee As System.IntPtr, ByVal dwTickCount As Integer, ByVal dwPendingType As Integer) As Integer Implements IOleMessageFilter.MessagePending
		Return CInt(PENDINGMSG.PENDINGMSG_WAITDEFPROCESS)
	End Function

	<DllImport("Ole32.dll")> _
	Private Shared Function CoRegisterMessageFilter(ByVal newFilter As IOleMessageFilter, ByRef oldFilter As IOleMessageFilter) As Integer
	End Function
End Class


