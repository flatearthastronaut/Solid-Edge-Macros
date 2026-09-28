using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Solid Edge Convert")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]

namespace SolidEdgeConvert
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                if (args.Length == 0) { Application.Run(new SetupForm()); return 0; }
                if (args.Length == 1 && (args[0] == "--install" || args[0] == "--uninstall"))
                {
                    using (RegistryKey user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                    {
                        if (args[0] == "--install") ShellMenu.Install(user, Application.ExecutablePath);
                        else ShellMenu.Uninstall(user);
                    }
                    ShellMenu.NotifyExplorer();
                    return 0;
                }
                if (args.Length != 2 || args[0] != "--step")
                    throw new ArgumentException("Usage: SolidEdgeConvert.exe --step \"C:\\folder\\part.par\"\n\nRun without arguments to install or remove the right-click menu.");

                // Serialize our converters: STEP translator settings belong to
                // the application, so concurrent exports could restore them in
                // the wrong order. Do not queue an unseen backlog of conversions.
                using (Mutex mutex = new Mutex(false, @"Local\SolidEdgeMacros.RightClickConvert"))
                {
                    bool acquired;
                    try { acquired = mutex.WaitOne(0); }
                    catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired) throw new InvalidOperationException("Another conversion is running. Wait for it to finish, then try again.");
                    try
                    {
                        string output = Conversion.OutputPath(args[1]);
                        bool replace = File.Exists(output);
                        if (replace && MessageBox.Show("Replace this existing STEP file?\n\n" + output,
                            "Solid Edge Convert", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2) != DialogResult.Yes) return 0;
                        using (ProgressForm form = new ProgressForm(args[1], replace))
                        {
                            Application.Run(form);
                            return form.Succeeded ? 0 : 1;
                        }
                    }
                    finally { mutex.ReleaseMutex(); }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(Describe(error), "Solid Edge Convert", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        internal static string Describe(Exception error)
        {
            AggregateException aggregate = error as AggregateException;
            if (aggregate != null)
            {
                string message = aggregate.Message.Split(new[] { " (" }, StringSplitOptions.None)[0];
                foreach (Exception inner in aggregate.Flatten().InnerExceptions) message += "\n\n" + Describe(inner);
                return message;
            }
            return error.Message + (error.InnerException == null ? "" : "\n" + Describe(error.InnerException));
        }
    }

    internal sealed class SetupForm : Form
    {
        internal SetupForm()
        {
            Text = "Solid Edge Convert";
            ClientSize = new Size(500, 205);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            Controls.Add(new Label { Left = 20, Top = 18, Width = 460, Height = 110,
                Text = "Add Convert > STEP (.stp) to the right-click menu for .par files.\n\nThe STEP file is saved beside the original part.\nOn Windows 11, choose Show more options first.\nKeep this executable in its current folder after installing." });
            Button install = new Button { Text = "Install menu", Left = 20, Top = 145, Width = 140, Height = 35 };
            Button remove = new Button { Text = "Remove menu", Left = 175, Top = 145, Width = 140, Height = 35 };
            Button close = new Button { Text = "Close", Left = 360, Top = 145, Width = 120, Height = 35 };
            install.Click += delegate { ChangeMenu(true); };
            remove.Click += delegate { ChangeMenu(false); };
            close.Click += delegate { Close(); };
            Controls.AddRange(new Control[] { install, remove, close });
            AcceptButton = install;
            CancelButton = close;
        }
        private void ChangeMenu(bool install)
        {
            try
            {
                using (RegistryKey user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                {
                    if (install) ShellMenu.Install(user, Application.ExecutablePath);
                    else ShellMenu.Uninstall(user);
                }
                ShellMenu.NotifyExplorer();
                MessageBox.Show(this, install ? "The Convert menu is installed for your Windows account." : "The Convert menu was removed.", Text);
            }
            catch (Exception error) { MessageBox.Show(this, Program.Describe(error), Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    internal sealed class ProgressForm : Form
    {
        private readonly string source;
        private readonly bool replace;
        private readonly Label status;
        private bool finished;
        internal bool Succeeded { get; private set; }

        internal ProgressForm(string source, bool replace)
        {
            this.source = source;
            this.replace = replace;
            Text = "Solid Edge Convert";
            ClientSize = new Size(460, 115);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            status = new Label { Left = 20, Top = 20, Width = 420, Height = 35, AutoEllipsis = true, Text = "Preparing conversion..." };
            Controls.Add(status);
            Controls.Add(new ProgressBar { Left = 20, Top = 70, Width = 420, Height = 18, Style = ProgressBarStyle.Marquee });
            // A COM export cannot safely be interrupted midway. Keep this window
            // alive until the worker has restored settings and released references.
            FormClosing += delegate(object sender, FormClosingEventArgs e) { e.Cancel = !finished; };
            Shown += delegate
            {
                Thread worker = new Thread(ConvertPart);
                worker.SetApartmentState(ApartmentState.STA);
                worker.Start();
            };
        }

        private void ConvertPart()
        {
            string result = null;
            Exception failure = null;
            try
            {
                using (OleMessageFilter filter = new OleMessageFilter())
                    result = Conversion.Run(source, replace, delegate { return new SolidEdgeSession(); },
                        delegate(string message) { BeginInvoke((Action)delegate { status.Text = message; }); });
            }
            catch (Exception error) { failure = error; }
            BeginInvoke((Action)delegate
            {
                finished = true;
                Succeeded = failure == null;
                MessageBox.Show(this, Succeeded ? "Created:\n\n" + result : Program.Describe(failure), Text,
                    MessageBoxButtons.OK, Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                Close();
            });
        }
    }
}
