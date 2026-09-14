using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ArrobAMO
{
    public sealed class PyramidMark : Control
    {
        public PyramidMark()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var b = new SolidBrush(ForeColor))
            {
                float h = Height - 4, y = Height - 2, w = Math.Max(10, Width / 3f - 3);
                e.Graphics.FillPolygon(b, new PointF[] { new PointF(2,y), new PointF(2+w/2,y-h*0.72f), new PointF(2+w,y) });
                e.Graphics.FillPolygon(b, new PointF[] { new PointF(Width/2f-w/2,y), new PointF(Width/2f,2), new PointF(Width/2f+w/2,y) });
                e.Graphics.FillPolygon(b, new PointF[] { new PointF(Width-w-2,y), new PointF(Width-2-w/2,y-h*0.62f), new PointF(Width-2,y) });
            }
        }
    }

    public sealed class BrowserForm : Form
    {
        readonly Color Ink = Color.FromArgb(18,18,20);
        readonly Color Ink2 = Color.FromArgb(28,28,31);
        readonly Color Ink3 = Color.FromArgb(38,38,42);
        readonly Color Soft = Color.FromArgb(52,52,57);
        readonly Color TextColor = Color.FromArgb(242,242,244);
        readonly Color Muted = Color.FromArgb(166,166,172);
        readonly Color Danger = Color.FromArgb(220,70,70);

        readonly Panel brand = new Panel();
        readonly Panel nav = new Panel();
        readonly Panel side = new Panel();
        readonly Panel statusLine = new Panel();
        readonly Label logo = new Label();
        readonly Label cpuLabel = new Label();
        readonly Label ramLabel = new Label();
        readonly Label gpuLabel = new Label();
        readonly Button aiButton = new Button();
        readonly Button plus = new Button();
        readonly Button back = new Button();
        readonly Button forward = new Button();
        readonly Button reload = new Button();
        readonly Button home = new Button();
        readonly Button historyButton = new Button();
        readonly TextBox address = new TextBox();
        readonly TabControl tabs = new TabControl();
        readonly SplitContainer workspace = new SplitContainer();
        readonly Panel aiHeader = new Panel();
        readonly Label aiTitle = new Label();
        readonly Button aiClose = new Button();
        readonly WebView2 aiWeb = new WebView2();
        readonly ContextMenuStrip aiMenu = new ContextMenuStrip();
        readonly ContextMenuStrip historyMenu = new ContextMenuStrip();
        readonly Timer monitorTimer = new Timer();
        readonly PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        readonly List<string> history = new List<string>();

        bool aiReady;
        bool closingTab;
        string HistoryFile
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArrobAMO");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "history.txt");
            }
        }

        public BrowserForm()
        {
            Text = "ArrobAMO";
            Width = 1380;
            Height = 880;
            MinimumSize = new Size(960, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ink;
            ForeColor = TextColor;
            KeyPreview = true;

            BuildBrand();
            BuildNav();
            BuildSidebar();
            BuildWorkspace();
            BuildAiMenu();
            LoadHistory();

            back.Click += delegate { var w = Active(); if (w != null && w.CanGoBack) w.GoBack(); };
            forward.Click += delegate { var w = Active(); if (w != null && w.CanGoForward) w.GoForward(); };
            reload.Click += delegate { var w = Active(); if (w != null) w.Reload(); };
            home.Click += delegate { Navigate("https://desarrollamo.com.ar/"); };
            plus.Click += async delegate { await AddTab("https://desarrollamo.com.ar/"); };
            aiButton.Click += delegate { aiMenu.Show(aiButton, new Point(0, aiButton.Height)); };
            historyButton.Click += delegate { ShowHistory(); };

            Shown += async delegate
            {
                cpuCounter.NextValue();
                await AddTab("https://desarrollamo.com.ar/");
                monitorTimer.Start();
            };

            FormClosed += delegate
            {
                monitorTimer.Stop();
                cpuCounter.Dispose();
                SaveHistory();
            };

            KeyDown += BrowserForm_KeyDown;
            monitorTimer.Interval = 2500;
            monitorTimer.Tick += async delegate { await UpdateMetrics(); };
        }

        void BuildBrand()
        {
            brand.Dock = DockStyle.Top;
            brand.Height = 38;
            brand.BackColor = Ink;
            Controls.Add(brand);

            var mark = new PyramidMark
            {
                Left = 12, Top = 8, Width = 44, Height = 22,
                ForeColor = Color.White, BackColor = Ink
            };
            brand.Controls.Add(mark);

            logo.Text = "ArrobAMO";
            logo.Left = 64;
            logo.Top = 9;
            logo.AutoSize = true;
            logo.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            logo.ForeColor = TextColor;
            brand.Controls.Add(logo);

            ConfigureMetric(cpuLabel, "CPU --", 210);
            ConfigureMetric(ramLabel, "RAM --", 295);
            ConfigureMetric(gpuLabel, "GPU --", 382);

            aiButton.Text = "Conectar IA";
            aiButton.Width = 118;
            aiButton.Height = 28;
            aiButton.Top = 5;
            aiButton.Left = ClientSize.Width - 134;
            aiButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            aiButton.FlatStyle = FlatStyle.Flat;
            aiButton.FlatAppearance.BorderColor = Soft;
            aiButton.FlatAppearance.MouseOverBackColor = Ink3;
            aiButton.ForeColor = TextColor;
            aiButton.BackColor = Ink2;
            brand.Controls.Add(aiButton);
        }

        void BuildNav()
        {
            nav.Dock = DockStyle.Top;
            nav.Height = 50;
            nav.BackColor = Ink2;
            Controls.Add(nav);

            AddNavButton(back, ((char)0x2190).ToString(), 10);
            AddNavButton(forward, ((char)0x2192).ToString(), 52);
            AddNavButton(reload, ((char)0x21BB).ToString(), 94);
            AddNavButton(home, ((char)0x2302).ToString(), 136);

            address.Left = 184;
            address.Top = 10;
            address.Height = 30;
            address.Width = ClientSize.Width - 282;
            address.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            address.Font = new Font("Segoe UI", 10f);
            address.BorderStyle = BorderStyle.FixedSingle;
            address.BackColor = Ink3;
            address.ForeColor = TextColor;
            address.KeyDown += Address_KeyDown;
            nav.Controls.Add(address);

            plus.Text = "+";
            plus.Left = ClientSize.Width - 88;
            plus.Top = 9;
            plus.Width = 38;
            plus.Height = 32;
            plus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            plus.FlatStyle = FlatStyle.Flat;
            plus.FlatAppearance.BorderSize = 0;
            plus.BackColor = Ink2;
            plus.ForeColor = TextColor;
            plus.Font = new Font("Segoe UI", 13f);
            nav.Controls.Add(plus);

            statusLine.Left = 184;
            statusLine.Top = 47;
            statusLine.Width = 0;
            statusLine.Height = 2;
            statusLine.BackColor = Color.White;
            nav.Controls.Add(statusLine);
        }

        void BuildSidebar()
        {
            side.Dock = DockStyle.Left;
            side.Width = 54;
            side.BackColor = Ink;
            Controls.Add(side);
            side.BringToFront();

            AddSideButton("Inicio", ((char)0x2302).ToString(), 8, delegate { Navigate("https://desarrollamo.com.ar/"); });
            AddSideButton("Historial", ((char)0x25F4).ToString(), 52, delegate { ShowHistory(); }, historyButton);
            AddSideButton("Scripts", ((char)0x25A3).ToString(), 96, delegate { MessageBox.Show("Scripts llega en la siguiente etapa de ArrobAMO.", "ArrobAMO"); });
            AddSideButton("Automatizaciones", ((char)0x2699).ToString(), 140, delegate { MessageBox.Show("Activar Loop y automatizaciones se implementarán sobre una ejecución real y verificable.", "ArrobAMO"); });
            AddSideButton("IA", ((char)0x2726).ToString(), 184, delegate { aiMenu.Show(side, new Point(side.Width, 184)); });
        }

        void BuildWorkspace()
        {
            workspace.Dock = DockStyle.Fill;
            workspace.BackColor = Ink;
            workspace.FixedPanel = FixedPanel.Panel2;
            workspace.Panel2MinSize = 0;
            workspace.SplitterWidth = 4;
            Controls.Add(workspace);
            workspace.BringToFront();

            tabs.Dock = DockStyle.Fill;
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(220, 34);
            tabs.Padding = new Point(16, 5);
            tabs.Font = new Font("Segoe UI", 9f);
            tabs.BackColor = Ink;
            tabs.DrawItem += Tabs_DrawItem;
            tabs.MouseDown += Tabs_MouseDown;
            tabs.MouseUp += delegate { closingTab = false; };
            tabs.SelectedIndexChanged += delegate { SyncActive(); tabs.Invalidate(); };
            workspace.Panel1.Controls.Add(tabs);

            aiHeader.Dock = DockStyle.Top;
            aiHeader.Height = 40;
            aiHeader.BackColor = Ink2;

            aiTitle.Text = "IA";
            aiTitle.ForeColor = TextColor;
            aiTitle.Left = 12;
            aiTitle.Top = 11;
            aiTitle.AutoSize = true;
            aiTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            aiHeader.Controls.Add(aiTitle);

            aiClose.Text = ((char)0x00D7).ToString();
            aiClose.Width = 38;
            aiClose.Height = 30;
            aiClose.Top = 5;
            aiClose.Left = 320;
            aiClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            aiClose.FlatStyle = FlatStyle.Flat;
            aiClose.FlatAppearance.BorderSize = 0;
            aiClose.ForeColor = TextColor;
            aiClose.BackColor = Ink2;
            aiClose.Font = new Font("Segoe UI", 13f);
            aiClose.Click += delegate { HideAI(); };
            aiHeader.Controls.Add(aiClose);

            workspace.Panel2.Controls.Add(aiHeader);
            aiWeb.Dock = DockStyle.Fill;
            workspace.Panel2.Controls.Add(aiWeb);
            aiWeb.BringToFront();
            aiHeader.BringToFront();
            HideAI();
        }

        void BuildAiMenu()
        {
            aiMenu.BackColor = Ink2;
            aiMenu.ForeColor = TextColor;
            AddAIOption("ChatGPT", "https://chatgpt.com/");
            AddAIOption("Gemini", "https://gemini.google.com/");
            AddAIOption("Claude", "https://claude.ai/");
            AddAIOption("Microsoft Copilot", "https://copilot.microsoft.com/");
            AddAIOption("Perplexity", "https://www.perplexity.ai/");
            aiMenu.Items.Add(new ToolStripSeparator());
            var other = new ToolStripMenuItem("Otra IA por URL...");
            other.Click += async delegate
            {
                string u = PromptAIUrl();
                if (!String.IsNullOrWhiteSpace(u)) await ShowAI("Personalizada", Normalize(u));
            };
            aiMenu.Items.Add(other);
        }

        void ConfigureMetric(Label label, string text, int left)
        {
            label.Text = text;
            label.Left = left;
            label.Top = 11;
            label.Width = 78;
            label.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            label.ForeColor = Muted;
            brand.Controls.Add(label);
        }

        void AddNavButton(Button b, string text, int left)
        {
            b.Text = text;
            b.Left = left;
            b.Top = 8;
            b.Width = 36;
            b.Height = 32;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Ink3;
            b.BackColor = Ink2;
            b.ForeColor = TextColor;
            b.Font = new Font("Segoe UI Symbol", 11f, FontStyle.Regular);
            nav.Controls.Add(b);
        }

        void AddSideButton(string tooltip, string glyph, int top, EventHandler action, Button existing = null)
        {
            var b = existing ?? new Button();
            b.Text = glyph;
            b.Left = 7;
            b.Top = top;
            b.Width = 40;
            b.Height = 38;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Ink3;
            b.BackColor = Ink;
            b.ForeColor = Muted;
            b.Font = new Font("Segoe UI Symbol", 12f);
            b.Click += action;
            new ToolTip().SetToolTip(b, tooltip);
            side.Controls.Add(b);
        }

        async Task AddTab(string url)
        {
            var page = new TabPage("Nueva pestaña");
            page.BackColor = Ink;
            page.ForeColor = TextColor;

            var web = new WebView2 { Dock = DockStyle.Fill };
            page.Controls.Add(web);
            tabs.TabPages.Add(page);
            tabs.SelectedTab = page;

            string data = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArrobAMO", "WebView2");
            Directory.CreateDirectory(data);

            var env = await CoreWebView2Environment.CreateAsync(null, data, null);
            await web.EnsureCoreWebView2Async(env);

            web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            web.CoreWebView2.Settings.AreDevToolsEnabled = true;
            web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            web.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            EnableInspectMenu(web);

            web.CoreWebView2.NavigationStarting += delegate(object s, CoreWebView2NavigationStartingEventArgs e)
            {
                if (tabs.SelectedTab == page) address.Text = e.Uri;
                SetLoading(true);
            };

            web.CoreWebView2.SourceChanged += delegate
            {
                if (tabs.SelectedTab == page) address.Text = web.Source == null ? "" : web.Source.ToString();
            };

            web.CoreWebView2.DocumentTitleChanged += delegate
            {
                string title = web.CoreWebView2.DocumentTitle;
                page.Text = ShortTitle(title);
                if (tabs.SelectedTab == page) Text = title + " - ArrobAMO";
                tabs.Invalidate();
            };

            web.CoreWebView2.NewWindowRequested += async delegate(object s, CoreWebView2NewWindowRequestedEventArgs e)
            {
                e.Handled = true;
                await AddTab(e.Uri);
            };

            web.CoreWebView2.NavigationCompleted += delegate
            {
                SetLoading(false);
                if (tabs.SelectedTab == page) SyncActive();
                AddHistory(web.Source == null ? "" : web.Source.ToString(), web.CoreWebView2.DocumentTitle);
            };

            web.Source = new Uri(Normalize(url));
        }

        void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabs.TabPages.Count) return;
            var page = tabs.TabPages[e.Index];
            var r = tabs.GetTabRect(e.Index);
            bool selected = tabs.SelectedIndex == e.Index;

            using (var bg = new SolidBrush(selected ? Ink3 : Ink2))
                e.Graphics.FillRectangle(bg, r);

            var textRect = new Rectangle(r.X + 12, r.Y + 7, r.Width - 42, r.Height - 12);
            TextRenderer.DrawText(e.Graphics, page.Text, tabs.Font, textRect, selected ? TextColor : Muted,
                TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

            var closeRect = CloseRect(r);
            TextRenderer.DrawText(e.Graphics, ((char)0x00D7).ToString(), new Font("Segoe UI", 11f),
                closeRect, selected ? TextColor : Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            if (selected)
            {
                using (var p = new Pen(Color.White, 2))
                    e.Graphics.DrawLine(p, r.Left + 8, r.Bottom - 2, r.Right - 8, r.Bottom - 2);
            }
        }

        Rectangle CloseRect(Rectangle tabRect)
        {
            return new Rectangle(tabRect.Right - 28, tabRect.Top + 6, 22, 22);
        }

        void Tabs_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabs.TabPages.Count; i++)
            {
                var r = tabs.GetTabRect(i);
                if (!r.Contains(e.Location)) continue;

                if (e.Button == MouseButtons.Middle || CloseRect(r).Contains(e.Location))
                {
                    closingTab = true;
                    CloseTab(i);
                    return;
                }
            }
        }

        void CloseTab(int index)
        {
            if (index < 0 || index >= tabs.TabPages.Count) return;
            var page = tabs.TabPages[index];
            var web = page.Controls.OfType<WebView2>().FirstOrDefault();
            if (web != null) web.Dispose();
            tabs.TabPages.Remove(page);
            page.Dispose();

            if (tabs.TabPages.Count == 0)
                BeginInvoke(new Action(async delegate { await AddTab("https://desarrollamo.com.ar/"); }));
            else
                SyncActive();
        }

        void SetLoading(bool active)
        {
            statusLine.Width = active ? Math.Max(100, address.Width / 2) : 0;
        }

        void EnableInspectMenu(WebView2 web)
        {
            web.CoreWebView2.ContextMenuRequested += delegate(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
            {
                var inspect = web.CoreWebView2.Environment.CreateContextMenuItem(
                    "Inspeccionar", null, CoreWebView2ContextMenuItemKind.Command);
                inspect.CustomItemSelected += delegate
                {
                    BeginInvoke(new Action(delegate { web.CoreWebView2.OpenDevToolsWindow(); }));
                };
                e.MenuItems.Insert(0, inspect);
            };
        }

        void AddAIOption(string name, string url)
        {
            var item = new ToolStripMenuItem(name);
            item.Click += async delegate { await ShowAI(name, url); };
            aiMenu.Items.Add(item);
        }

        async Task ShowAI(string name, string url)
        {
            workspace.Panel2Collapsed = false;
            workspace.SplitterDistance = Math.Max(560, Width - 420);
            aiTitle.Text = "IA - " + name;

            if (!aiReady)
            {
                string data = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ArrobAMO", "AI");
                Directory.CreateDirectory(data);
                var env = await CoreWebView2Environment.CreateAsync(null, data, null);
                await aiWeb.EnsureCoreWebView2Async(env);
                aiWeb.CoreWebView2.Settings.AreDevToolsEnabled = true;
                aiWeb.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                aiWeb.CoreWebView2.Settings.IsStatusBarEnabled = false;
                aiReady = true;
            }

            aiWeb.Source = new Uri(url);
        }

        void HideAI()
        {
            workspace.Panel2Collapsed = true;
        }

        string PromptAIUrl()
        {
            using (var f = new Form())
            {
                f.Text = "Conectar otra IA";
                f.Width = 520;
                f.Height = 160;
                f.StartPosition = FormStartPosition.CenterParent;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.BackColor = Ink2;
                f.ForeColor = TextColor;

                var label = new Label { Text = "URL del servicio de IA", Left = 16, Top = 16, Width = 460, ForeColor = TextColor };
                var box = new TextBox { Left = 16, Top = 42, Width = 470, Text = "https://" };
                var ok = new Button { Text = "Conectar", Left = 386, Top = 76, Width = 100, DialogResult = DialogResult.OK };

                f.Controls.Add(label);
                f.Controls.Add(box);
                f.Controls.Add(ok);
                f.AcceptButton = ok;

                return f.ShowDialog(this) == DialogResult.OK ? box.Text.Trim() : "";
            }
        }

        void ShowHistory()
        {
            historyMenu.Items.Clear();
            historyMenu.BackColor = Ink2;
            historyMenu.ForeColor = TextColor;

            if (history.Count == 0)
            {
                historyMenu.Items.Add(new ToolStripMenuItem("Sin historial") { Enabled = false });
            }
            else
            {
                foreach (var row in history.Take(15))
                {
                    var parts = row.Split(new[] { '|' }, 2);
                    string title = parts.Length > 1 ? parts[0] : parts[0];
                    string url = parts.Length > 1 ? parts[1] : parts[0];
                    var item = new ToolStripMenuItem(ShortTitle(title));
                    item.ToolTipText = url;
                    item.Click += delegate { Navigate(url); };
                    historyMenu.Items.Add(item);
                }

                historyMenu.Items.Add(new ToolStripSeparator());
                var clear = new ToolStripMenuItem("Borrar historial");
                clear.Click += delegate { history.Clear(); SaveHistory(); };
                historyMenu.Items.Add(clear);
            }

            historyMenu.Show(side, new Point(side.Width, 52));
        }

        void AddHistory(string url, string title)
        {
            if (String.IsNullOrWhiteSpace(url) || (!url.StartsWith("http://") && !url.StartsWith("https://"))) return;
            string safeTitle = String.IsNullOrWhiteSpace(title) ? url : title.Replace("|", " ");
            string row = safeTitle + "|" + url;
            history.RemoveAll(x => x.EndsWith("|" + url, StringComparison.OrdinalIgnoreCase));
            history.Insert(0, row);
            if (history.Count > 100) history.RemoveRange(100, history.Count - 100);
            SaveHistory();
        }

        void LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryFile))
                    history.AddRange(File.ReadAllLines(HistoryFile).Where(x => !String.IsNullOrWhiteSpace(x)).Take(100));
            }
            catch { }
        }

        void SaveHistory()
        {
            try { File.WriteAllLines(HistoryFile, history.Take(100).ToArray()); } catch { }
        }

        void BrowserForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                var w = Active();
                if (w != null && w.CoreWebView2 != null) w.CoreWebView2.OpenDevToolsWindow();
            }

            if (e.Control && e.KeyCode == Keys.L)
            {
                address.Focus();
                address.SelectAll();
                e.SuppressKeyPress = true;
            }

            if (e.Control && e.KeyCode == Keys.T)
            {
                BeginInvoke(new Action(async delegate { await AddTab("https://desarrollamo.com.ar/"); }));
                e.SuppressKeyPress = true;
            }

            if (e.Control && e.KeyCode == Keys.W)
            {
                if (tabs.SelectedIndex >= 0) CloseTab(tabs.SelectedIndex);
                e.SuppressKeyPress = true;
            }
        }

        void Address_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Navigate(address.Text);
        }

        void Navigate(string input)
        {
            var web = Active();
            if (web == null) return;
            web.Source = new Uri(Normalize(input));
        }

        string Normalize(string input)
        {
            input = (input ?? "").Trim();
            if (input.Length == 0) return "https://desarrollamo.com.ar/";

            Uri uri;
            if (Uri.TryCreate(input, UriKind.Absolute, out uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return uri.ToString();

            if (input.Contains(".") && !input.Contains(" "))
                return "https://" + input;

            return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
        }

        WebView2 Active()
        {
            if (tabs.SelectedTab == null) return null;
            return tabs.SelectedTab.Controls.OfType<WebView2>().FirstOrDefault();
        }

        void SyncActive()
        {
            if (closingTab) return;
            var w = Active();
            if (w == null || w.CoreWebView2 == null) return;
            address.Text = w.Source == null ? "" : w.Source.ToString();
            back.Enabled = w.CanGoBack;
            forward.Enabled = w.CanGoForward;
            Text = w.CoreWebView2.DocumentTitle + " - ArrobAMO";
        }

        string ShortTitle(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return "Nueva pestaña";
            return s.Length <= 28 ? s : s.Substring(0, 27) + "...";
        }

        async Task UpdateMetrics()
        {
            try
            {
                float cpu = cpuCounter.NextValue();
                MEMORYSTATUSEX m = new MEMORYSTATUSEX();
                GlobalMemoryStatusEx(m);
                double ram = m.ullTotalPhys == 0 ? 0 : (double)(m.ullTotalPhys - m.ullAvailPhys) / m.ullTotalPhys * 100.0;
                double gpu = await Task.Run(() => ReadGpuUsage());

                cpuLabel.Text = "CPU " + Math.Round(cpu) + "%";
                ramLabel.Text = "RAM " + Math.Round(ram) + "%";
                gpuLabel.Text = gpu < 0 ? "GPU --" : "GPU " + Math.Round(gpu) + "%";
            }
            catch { }
        }

        double ReadGpuUsage()
        {
            try
            {
                double total = 0;
                using (var searcher = new ManagementObjectSearcher(
                    "root\\CIMV2",
                    "SELECT Name,UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = Convert.ToString(obj["Name"]) ?? "";
                        if (name.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            name.IndexOf("engtype_Compute", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            double v;
                            if (Double.TryParse(Convert.ToString(obj["UtilizationPercentage"]), out v))
                                total += v;
                        }
                    }
                }
                return Math.Min(100, total);
            }
            catch { return -1; }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BrowserForm());
        }
    }
}



