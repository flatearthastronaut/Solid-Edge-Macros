using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Solid Edge Convert")]
[assembly: System.Reflection.AssemblyVersion("1.11.0.0")]

namespace SolidEdgeConvert
{
    internal static class Program
    {
        // One executable serves three roles: setup with no arguments, interactive
        // CLI conversion, and the COM server launched by Explorer. Both conversion
        // entry points converge on RunBatch for identical prompts and cleanup.
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                if (args.Length == 0) { Application.Run(new SetupForm()); return 0; }
                if (args[0] == "--shell-server")
                {
                    using (ShellServer server = new ShellServer()) Application.Run(server);
                    return 0;
                }
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
                if (args.Length < 2 || (args[0] != "--step" && args[0] != "--pdf" && args[0] != "--pdf-date" && args[0] != "--part" && args[0] != "--parasolid-part" && args[0] != "--parasolid-assembly" && args[0] != "--step-assembly" && args[0] != "--parasolid" && args[0] != "--stl" && args[0] != "--pdf-same-type" && args[0] != "--pdf-date-same-type" && args[0] != "--pdf-no-grind"))
                    throw new ArgumentException("Usage:\nSolidEdgeConvert.exe --step \"part.par\" \"assembly.asm\" ...\nSolidEdgeConvert.exe --pdf \"drawing1.dft\" \"drawing2.dft\" ...\nSolidEdgeConvert.exe --pdf-date \"drawing1.dft\" \"drawing2.dft\" ...\nSolidEdgeConvert.exe --part \"file1.stp\" \"file2.step\" ...\nSolidEdgeConvert.exe --parasolid-part \"file1.x_t\" \"file2.x_b\" ...\nSolidEdgeConvert.exe --parasolid-assembly \"file1.x_t\" \"file2.x_b\" ...\nSolidEdgeConvert.exe --step-assembly \"file1.stp\" \"file2.step\" ...\nSolidEdgeConvert.exe --parasolid \"part.par\" \"assembly.asm\" ...\nSolidEdgeConvert.exe --stl \"part1.par\" \"part2.par\" ...\nSolidEdgeConvert.exe --pdf-same-type \"drawing.dft\" ...\nSolidEdgeConvert.exe --pdf-date-same-type \"drawing.dft\" ...\nSolidEdgeConvert.exe --pdf-no-grind \"drawing.dft\" ...\n--pdf, --pdf-date and --pdf-no-grind export the active sheet only.\n\nRun without arguments to install or remove the right-click menus.");
                ConversionFormat format = args[0] == "--step" ? ConversionFormat.Step : args[0] == "--pdf" ? ConversionFormat.Pdf : args[0] == "--part" ? ConversionFormat.Part : args[0] == "--parasolid-part" ? ConversionFormat.ParasolidPart : args[0] == "--parasolid-assembly" ? ConversionFormat.ParasolidAssembly : args[0] == "--step-assembly" ? ConversionFormat.StepAssembly : args[0] == "--parasolid" ? ConversionFormat.ParasolidExport : args[0] == "--stl" ? ConversionFormat.Stl : args[0] == "--pdf-same-type" ? ConversionFormat.PdfSameType : args[0] == "--pdf-date-same-type" ? ConversionFormat.PdfWithDateSameType : args[0] == "--pdf-no-grind" ? ConversionFormat.PdfWithoutGrindStock : ConversionFormat.PdfWithDate;

                return RunBatch(new List<string>(args).GetRange(1, args.Length - 1).ToArray(), format);
            }
            catch (Exception error)
            {
                MessageBox.Show(Describe(error), "Solid Edge Convert", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        internal static int RunBatch(string[] sources, ConversionFormat format)
        {
            sources = BatchConversion.UniquePaths(sources);
            // Freeze before the overwrite prompt; a user may leave it open past
            // midnight, but its filenames must still match the eventual exports.
            DateTime batchDate = DateTime.Today;
            // The server queue serializes its own requests; this named mutex also
            // excludes separate CLI processes in the same Windows session. Never
            // run parallel exports against Solid Edge's shared translator state.
            using (Mutex mutex = new Mutex(false, @"Local\SolidEdgeMacros.RightClickConvert"))
            {
                bool acquired;
                try { acquired = mutex.WaitOne(0); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new InvalidOperationException("Another conversion is running. Wait for it to finish, then try again.");
                try
                {
                    int conflicts = 0;
                    foreach (string source in sources)
                    {
                        // Invalid inputs become individual failures in the final
                        // results; they must not prevent valid files converting.
                        try { if (File.Exists(Conversion.OutputPath(source, format, batchDate))) conflicts++; }
                        catch (ArgumentException) { }
                        catch (IOException) { }
                    }
                    // Default to Skip even if no conflicts exist yet: another
                    // program may create an output before its turn in the batch.
                    // Only an explicit Yes grants replacement for this selection.
                    ExistingOutput existing = ExistingOutput.Skip;
                    if (conflicts > 0)
                    {
                        DialogResult answer = MessageBox.Show(conflicts + " output file(s) already exist.\n\nYes: replace existing files\nNo: skip existing files\nCancel: cancel this conversion",
                            "Solid Edge Convert", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                        if (answer == DialogResult.Cancel) return 0;
                        if (answer == DialogResult.Yes) existing = ExistingOutput.Replace;
                    }
                    using (ProgressForm form = new ProgressForm(sources, existing, format, batchDate))
                    {
                        form.ShowDialog();
                        return form.Succeeded ? 0 : 1;
                    }
                }
                finally { mutex.ReleaseMutex(); }
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
            ClientSize = new Size(550, 340);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            Controls.Add(new Label { Left = 20, Top = 18, Width = 510, Height = 250,
                Text = "Select one or more files, then right-click Convert:\nParts (.par) > STEP, Parasolid or STL\nAssemblies (.asm) > STEP or Parasolid\nDrafts (.dft) > PDF or PDF with Date\n  Active sheet only / All sheets of same type\n  PDF without Grind Stock (active sheet only)\nSTEP (.stp / .step) > Part (.par) or Assembly (.asm)\nParasolid (.x_t / .x_b) > Part (.par) or Assembly (.asm)\n\nOutputs are saved beside the original files.\nOn Windows 11, choose Show more options first.\nKeep this executable in its current folder after installing." });
            Button install = new Button { Text = "Install menus", Left = 20, Top = 285, Width = 140, Height = 35 };
            Button remove = new Button { Text = "Remove menus", Left = 175, Top = 285, Width = 140, Height = 35 };
            Button close = new Button { Text = "Close", Left = 360, Top = 285, Width = 120, Height = 35 };
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
                MessageBox.Show(this, install ? "The Convert menus are installed for your Windows account." : "The Convert menus were removed.", Text);
            }
            catch (Exception error) { MessageBox.Show(this, Program.Describe(error), Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    internal sealed class ProgressForm : Form
    {
        private readonly string[] sources;
        private readonly ExistingOutput existing;
        private readonly ConversionFormat format;
        private readonly DateTime batchDate;
        private readonly Label status;
        private bool finished;
        internal bool Succeeded { get; private set; }

        internal ProgressForm(string[] sources, ExistingOutput existing, ConversionFormat format, DateTime batchDate)
        {
            this.sources = sources;
            this.existing = existing;
            this.format = format;
            this.batchDate = batchDate;
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
                // Keep the UI responsive while COM translates. A dedicated STA
                // is required; a default thread-pool task would normally be MTA.
                Thread worker = new Thread(ConvertFiles);
                worker.SetApartmentState(ApartmentState.STA);
                worker.Start();
            };
        }

        private void ConvertFiles()
        {
            // Only this worker owns CAD references and its OLE message filter.
            // BeginInvoke posts text/results back to the UI thread; no document
            // wrappers cross that boundary. The final callback runs after batch
            // cleanup and filter disposal, when closing the form is safe again.
            List<BatchItem> results = null;
            Exception failure = null;
            try
            {
                using (OleMessageFilter filter = new OleMessageFilter())
                    results = BatchConversion.Run(sources, format, existing, delegate { return new SolidEdgeSession(); },
                        delegate(int number, int total, string message) { BeginInvoke((Action)delegate { status.Text = number + " / " + total + ": " + message; }); }, batchDate);
            }
            catch (Exception error) { failure = error; }
            BeginInvoke((Action)delegate
            {
                finished = true;
                int converted = 0, skipped = 0, failed = 0;
                List<string> lines = new List<string>();
                if (Conversion.IsAssembly(format))
                    lines.Add("Keep each assembly with its matching Components folder when moving or sharing it.");
                if (results != null) foreach (BatchItem item in results)
                {
                    if (item.Error != null) { failed++; lines.Add("FAILED: " + item.Source + "\r\n  " + item.Error); }
                    else if (item.Skipped) { skipped++; lines.Add("SKIPPED: " + item.Output); }
                    else { converted++; lines.Add("CREATED: " + item.Output); }
                }
                if (failure != null) { failed++; lines.Add(Program.Describe(failure)); }
                Succeeded = failed == 0;
                // Dispose the progress controls before replacing the contents;
                // removing controls alone would leave their native handles alive.
                while (Controls.Count > 0) Controls[0].Dispose();
                ClientSize = new Size(700, 400);
                Controls.Add(new Label { Left = 20, Top = 15, Width = 660, Height = 30,
                    Text = converted + " converted, " + skipped + " skipped, " + failed + " failed." });
                Controls.Add(new TextBox { Left = 20, Top = 50, Width = 660, Height = 285,
                    Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false,
                    Text = String.Join("\r\n\r\n", lines) });
                Button close = new Button { Left = 560, Top = 350, Width = 120, Height = 35, Text = "Close" };
                close.Click += delegate { Close(); };
                Controls.Add(close);
                AcceptButton = close; CancelButton = close;
            });
        }
    }
}
