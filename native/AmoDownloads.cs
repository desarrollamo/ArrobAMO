using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace ArrobAMO
{
    public sealed class AmoDownloadManager
    {
        readonly List<CoreWebView2DownloadOperation> operations = new List<CoreWebView2DownloadOperation>();
        readonly HashSet<CoreWebView2DownloadOperation> paused = new HashSet<CoreWebView2DownloadOperation>();
        readonly Action<string> retryNavigation;

        public AmoDownloadManager(Action<string> retry)
        {
            retryNavigation = retry;
        }

        public void Attach(CoreWebView2 core)
        {
            core.DownloadStarting += delegate(object sender, CoreWebView2DownloadStartingEventArgs e)
            {
                lock (operations)
                {
                    if (!operations.Contains(e.DownloadOperation)) operations.Insert(0, e.DownloadOperation);
                    if (operations.Count > 50) operations.RemoveRange(50, operations.Count - 50);
                }
            };
        }

        internal CoreWebView2DownloadOperation[] Snapshot()
        {
            lock (operations) return operations.ToArray();
        }

        public bool IsPaused(CoreWebView2DownloadOperation op)
        {
            lock (operations) return paused.Contains(op);
        }

        public void TogglePause(CoreWebView2DownloadOperation op)
        {
            if (op == null) return;
            lock (operations)
            {
                try
                {
                    if (paused.Contains(op))
                    {
                        op.Resume();
                        paused.Remove(op);
                    }
                    else
                    {
                        op.Pause();
                        paused.Add(op);
                    }
                }
                catch { }
            }
        }

        public void Retry(CoreWebView2DownloadOperation op)
        {
            if (op == null) return;
            try
            {
                if (op.CanResume)
                {
                    op.Resume();
                    lock (operations) paused.Remove(op);
                    return;
                }
            }
            catch { }

            if (retryNavigation != null && !String.IsNullOrWhiteSpace(op.Uri))
                retryNavigation(op.Uri);
        }

        public void Cancel(CoreWebView2DownloadOperation op)
        {
            if (op == null) return;
            try { op.Cancel(); } catch { }
            lock (operations) paused.Remove(op);
        }

        public void Show(IWin32Window owner)
        {
            new AmoDownloadsForm(this).Show(owner);
        }
    }

    public sealed class AmoDownloadsForm : Form
    {
        readonly AmoDownloadManager manager;
        readonly FlowLayoutPanel list = new FlowLayoutPanel();
        readonly Timer timer = new Timer();

        public AmoDownloadsForm(AmoDownloadManager source)
        {
            manager = source;
            Text = "Descargas · ArrobAMO";
            Width = 760; Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AmoTheme.Bg; ForeColor = AmoTheme.Text; Font = AmoTheme.UI(9f);

            var head = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = AmoTheme.Surface };
            head.Controls.Add(new Label { Text = "Descargas", Left = 24, Top = 15, Width = 360, Height = 30, Font = AmoTheme.UI(18f, FontStyle.Bold), ForeColor = AmoTheme.Text });
            head.Controls.Add(new Label { Text = "Archivos gestionados por ArrobAMO", Left = 25, Top = 46, Width = 420, Height = 20, ForeColor = AmoTheme.TextSoft });
            Controls.Add(head);

            list.Dock = DockStyle.Fill; list.AutoScroll = true; list.Padding = new Padding(16); list.BackColor = AmoTheme.Bg;
            list.FlowDirection = FlowDirection.TopDown; list.WrapContents = false;
            Controls.Add(list); list.BringToFront();

            timer.Interval = 700; timer.Tick += delegate { RefreshRows(); };
            Shown += delegate { RefreshRows(); timer.Start(); };
            FormClosed += delegate { timer.Stop(); timer.Dispose(); };
        }

        void RefreshRows()
        {
            int scroll = list.VerticalScroll.Value;
            list.SuspendLayout();
            list.Controls.Clear();
            var ops = manager.Snapshot();

            if (ops.Length == 0)
            {
                list.Controls.Add(new Label
                {
                    Text = "Todavía no hay descargas en esta sesión.",
                    AutoSize = false,
                    Width = 680,
                    Height = 70,
                    Padding = new Padding(16),
                    ForeColor = AmoTheme.TextSoft,
                    BackColor = AmoTheme.Surface
                });
            }

            foreach (var op in ops) list.Controls.Add(BuildRow(op));
            list.ResumeLayout();
            try { list.VerticalScroll.Value = Math.Min(scroll, Math.Max(0, list.VerticalScroll.Maximum)); } catch { }
        }

        Control BuildRow(CoreWebView2DownloadOperation op)
        {
            var row = new AmoCard { Width = 690, Height = 126, Margin = new Padding(0,0,0,10) };
            string path = op.ResultFilePath ?? "";
            string name = String.IsNullOrWhiteSpace(path) ? "Descarga" : Path.GetFileName(path);

            row.Controls.Add(new Label
            {
                Text = name, Left = 14, Top = 12, Width = 420, Height = 22,
                Font = AmoTheme.UI(10f, FontStyle.Bold), ForeColor = AmoTheme.Text
            });

            double total = 0, got = 0;
            int pct = 0;
            try
            {
                total = Convert.ToDouble(op.TotalBytesToReceive);
                got = Convert.ToDouble(op.BytesReceived);
                if (total > 0) pct = (int)Math.Max(0, Math.Min(100, got / total * 100.0));
            }
            catch { }

            string state = "En curso";
            try
            {
                if (op.State == CoreWebView2DownloadState.Completed) state = "Completada";
                else if (op.State == CoreWebView2DownloadState.Interrupted) state = "Interrumpida";
            }
            catch { }

            row.Controls.Add(new Label
            {
                Text = state + " · " + pct + "%", Left = 14, Top = 39,
                Width = 260, Height = 20, ForeColor = AmoTheme.TextSoft
            });

            var progress = new ProgressBar
            {
                Left = 14, Top = 68, Width = 420, Height = 8,
                Minimum = 0, Maximum = 100, Value = pct
            };
            row.Controls.Add(progress);

            var open = new AmoButton
            {
                Text = "Abrir", Variant = AmoButtonVariant.Secondary,
                Left = 456, Top = 12, Width = 94, Height = 34
            };
            open.Enabled = File.Exists(path);
            open.Click += delegate { try { Process.Start(path); } catch { } };
            row.Controls.Add(open);

            var folder = new AmoButton
            {
                Text = "Carpeta", Variant = AmoButtonVariant.Outline,
                Left = 558, Top = 12, Width = 104, Height = 34
            };
            folder.Enabled = !String.IsNullOrWhiteSpace(path);
            folder.Click += delegate
            {
                try { Process.Start("explorer.exe", "/select,\"" + path + "\""); } catch { }
            };
            row.Controls.Add(folder);

            var action = new AmoButton
            {
                Text = "Pausar", Variant = AmoButtonVariant.Secondary,
                Left = 456, Top = 58, Width = 94, Height = 34
            };

            bool inProgress = false;
            bool interrupted = false;
            bool completed = false;
            try
            {
                inProgress = op.State == CoreWebView2DownloadState.InProgress;
                interrupted = op.State == CoreWebView2DownloadState.Interrupted;
                completed = op.State == CoreWebView2DownloadState.Completed;
            }
            catch { }

            if (completed)
            {
                action.Text = "Completada";
                action.Enabled = false;
            }
            else if (manager.IsPaused(op))
            {
                action.Text = "Reanudar";
                action.Enabled = true;
            }
            else if (interrupted)
            {
                action.Text = op.CanResume ? "Reanudar" : "Reintentar";
                action.Enabled = true;
            }
            else
            {
                action.Text = "Pausar";
                action.Enabled = inProgress;
            }

            action.Click += delegate
            {
                if (manager.IsPaused(op) || interrupted)
                    manager.Retry(op);
                else
                    manager.TogglePause(op);
                RefreshRows();
            };
            row.Controls.Add(action);

            var cancel = new AmoButton
            {
                Text = "Cancelar", Variant = AmoButtonVariant.Ghost,
                Left = 558, Top = 58, Width = 104, Height = 34
            };
            cancel.Enabled = inProgress || manager.IsPaused(op);
            cancel.Click += delegate { manager.Cancel(op); RefreshRows(); };
            row.Controls.Add(cancel);

            return row;
        }
    }
}

