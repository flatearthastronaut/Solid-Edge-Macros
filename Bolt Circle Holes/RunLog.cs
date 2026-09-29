using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BoltCircleHoles
{
    public static class RunLog
    {
        static readonly object gate = new object();
        static readonly string session = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Process.GetCurrentProcess().Id;
        public static readonly string PathName = Path.Combine(Path.GetDirectoryName(typeof(RunLog).Assembly.Location), "BoltCircleHoles-run.log");
        public static string WriteFailure;
        public static string Value(object value)
        {
            var array=value as Array;
            if(array!=null)
            {
                var items=new System.Collections.Generic.List<string>();
                foreach(object item in array) items.Add(Value(item));
                return "["+String.Join(", ",items)+"]";
            }
            return Convert.ToString(value,CultureInfo.InvariantCulture);
        }
        public static void Write(string stage,string detail)
        {
            lock(gate)
            {
                try
                {
                    string line=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz",CultureInfo.InvariantCulture)+" session="+session+" thread="+Thread.CurrentThread.ManagedThreadId+" "+stage+" | "+detail+Environment.NewLine;
                    using(var stream=new FileStream(PathName,FileMode.Append,FileAccess.Write,FileShare.ReadWrite))
                    using(var writer=new StreamWriter(stream,new UTF8Encoding(false))) writer.Write(line);
                    WriteFailure=null;
                }
                catch(Exception ex) { WriteFailure=ex.Message; }
            }
        }
        public static void Error(string stage,Exception ex)
        {
            Write(stage,"ERROR HRESULT=0x"+ex.HResult.ToString("X8")+" "+ex);
        }
        public static T Call<T>(string stage,Func<T> action)
        {
            Write(stage,"BEGIN");
            var elapsed=Stopwatch.StartNew();
            try {T result=action();Write(stage,"OK elapsed_ms="+elapsed.ElapsedMilliseconds);return result;}
            catch(Exception ex){Error(stage+" elapsed_ms="+elapsed.ElapsedMilliseconds,ex);throw;}
        }
        public static void Call(string stage,Action action) {Call<int>(stage,delegate {action();return 0;});}
        public static string Tail()
        {
            try
            {
                using(var stream=new FileStream(PathName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                {
                    bool truncated=stream.Length>262144;
                    if(truncated) stream.Seek(-262144,SeekOrigin.End);
                    using(var reader=new StreamReader(stream,Encoding.UTF8))
                    { if(truncated)reader.ReadLine();return reader.ReadToEnd(); }
                }
            }
            catch(Exception ex){return "Unable to read run log: "+ex.Message;}
        }
    }
    public sealed class LogWindow : Form
    {
        readonly TextBox text=new TextBox {Multiline=true,ReadOnly=true,WordWrap=false,ScrollBars=ScrollBars.Both,Dock=DockStyle.Fill,Font=new Font("Consolas",9)};
        readonly CheckBox live=new CheckBox {Text="Live updates",Checked=true,Dock=DockStyle.Top,Height=28};
        readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer {Interval=1000};
        public LogWindow()
        {
            Text="Bolt Circle Holes — Run log"; Size=new Size(1000,540); StartPosition=FormStartPosition.CenterParent;
            var path=new TextBox {ReadOnly=true,Text=RunLog.PathName,Dock=DockStyle.Bottom};
            var copy=new Button {Text="Copy log",Dock=DockStyle.Bottom,Height=30};
            copy.Click+=delegate {try {Clipboard.SetText(text.Text);} catch(Exception ex){MessageBox.Show(this,ex.Message,"Copy log");}};
            Controls.Add(text);Controls.Add(live);Controls.Add(copy);Controls.Add(path);
            timer.Tick+=delegate {if(live.Checked) RefreshLog();};
            Shown+=delegate {RefreshLog();timer.Start();};
            FormClosed+=delegate {timer.Stop();timer.Dispose();};
        }
        void RefreshLog()
        {
            string content=RunLog.Tail();
            if(text.Text==content)return;
            text.Text=content;text.SelectionStart=text.TextLength;text.ScrollToCaret();
        }
    }
}
