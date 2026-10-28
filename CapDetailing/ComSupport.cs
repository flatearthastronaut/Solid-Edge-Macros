using System;
using System.Runtime.InteropServices;
[ComImport,Guid("00000016-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMessageFilter{
 [PreserveSig]int HandleInComingCall(int type,IntPtr caller,int ticks,IntPtr info);
 [PreserveSig]int RetryRejectedCall(IntPtr callee,int ticks,int type);
 [PreserveSig]int MessagePending(IntPtr callee,int ticks,int type);
}
sealed class BusyFilter:IMessageFilter,IDisposable{
 IMessageFilter previous;
 public BusyFilter(){Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this,out previous));}
 public int HandleInComingCall(int type,IntPtr caller,int ticks,IntPtr info){return 0;}
 public int RetryRejectedCall(IntPtr callee,int ticks,int type){return type==2&&ticks<30000?250:-1;}
 public int MessagePending(IntPtr callee,int ticks,int type){return 2;}
 public void Dispose(){IMessageFilter removed;Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(previous,out removed));GC.KeepAlive(this);}
 [DllImport("ole32.dll")]static extern int CoRegisterMessageFilter(IMessageFilter current,out IMessageFilter previous);
}
