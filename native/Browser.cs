using System;
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

namespace DesarrollAMOBrowser
{
    public class BrowserForm : Form
    {
        readonly Color Ink = ColorTranslator.FromHtml("#111827");
        readonly Color Pink = ColorTranslator.FromHtml("#FF5AA5");
        readonly Color Sky = ColorTranslator.FromHtml("#7DD3FC");
        readonly Color Coral = ColorTranslator.FromHtml("#FF9F6E");
        readonly Color Mist = ColorTranslator.FromHtml("#E5E7EB");

        readonly Panel brand = new Panel();
        readonly Panel nav = new Panel();
        readonly Label logo = new Label();
        readonly Label cpuLabel = new Label();
        readonly Label ramLabel = new Label();
        readonly Label gpuLabel = new Label();
        readonly Button aiButton = new Button();
        readonly Button back = new Button();
        readonly Button forward = new Button();
        readonly Button reload = new Button();
        readonly Button home = new Button();
        readonly Button plus = new Button();
        readonly TextBox address = new TextBox();
        readonly TabControl tabs = new TabControl();
        readonly SplitContainer workspace = new SplitContainer();
        readonly Panel aiHeader = new Panel();
        readonly Label aiTitle = new Label();
        readonly Button aiClose = new Button();
        readonly WebView2 aiWeb = new WebView2();
        readonly ContextMenuStrip aiMenu = new ContextMenuStrip();
        readonly Timer monitorTimer = new Timer();
        readonly PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        bool aiReady = false;

        public BrowserForm()
        {
            Text = "DesarrollAMO Browser";
            Width = 1360;
            Height = 860;
            MinimumSize = new Size(900, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ink;
            KeyPreview = true;

            brand.Dock = DockStyle.Top;
            brand.Height = 38;
            brand.BackColor = Color.FromArgb(248, 247, 243);
            Controls.Add(brand);

            logo.Text = "Desarroll  AMO .";
            logo.Left = 14;
            logo.Top = 8;
            logo.AutoSize = true;
            logo.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            logo.ForeColor = Ink;
            brand.Controls.Add(logo);

            ConfigureMetric(cpuLabel, "CPU --", 210);
            ConfigureMetric(ramLabel, "RAM --", 300);
            ConfigureMetric(gpuLabel, "GPU --", 400);

            aiButton.Text = "Conectar IA ▾";
            aiButton.Width = 128;
            aiButton.Height = 28;
            aiButton.Top = 5;
            aiButton.Left = ClientSize.Width - 145;
            aiButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            aiButton.FlatStyle = FlatStyle.Flat;
            aiButton.FlatAppearance.BorderColor = Pink;
            aiButton.ForeColor = Ink;
            aiButton.BackColor = Color.White;
            aiButton.Click += delegate { aiMenu.Show(aiButton, new Point(0, aiButton.Height)); };
            brand.Controls.Add(aiButton);

            AddAIOption("ChatGPT", "https://chatgpt.com/");
            AddAIOption("Gemini", "https://gemini.google.com/");
            AddAIOption("Claude", "https://claude.ai/");
            AddAIOption("Microsoft Copilot", "https://copilot.microsoft.com/");
            AddAIOption("Perplexity", "https://www.perplexity.ai/");

            nav.Dock = DockStyle.Top;
            nav.Height = 48;
            nav.BackColor = Ink;
            Controls.Add(nav);

            AddNavButton(back, "←", 8);
            AddNavButton(forward, "→", 50);
            AddNavButton(reload, "↻", 92);
            AddNavButton(home, "⌂", 134);

            address.Left = 180;
            address.Top = 9;
            address.Height = 29;
            address.Width = ClientSize.Width - 238;
            address.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            address.Font = new Font("Segoe UI", 10f);
            address.BorderStyle = BorderStyle.FixedSingle;
            address.KeyDown += Address_KeyDown;
            nav.Controls.Add(address);

            AddNavButton(plus, "+", ClientSize.Width - 48);
            plus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            workspace.Dock = DockStyle.Fill;
            workspace.FixedPanel = FixedPanel.Panel2;
            workspace.Panel2MinSize = 0;
            workspace.SplitterWidth = 5;
            workspace.IsSplitterFixed = false;
            Controls.Add(workspace);
            workspace.BringToFront();

            tabs.Dock = DockStyle.Fill;
            tabs.Font = new Font("Segoe UI", 9f);
            tabs.SelectedIndexChanged += delegate { SyncActive(); };
            workspace.Panel1.Controls.Add(tabs);

            aiHeader.Dock = DockStyle.Top;
            aiHeader.Height = 40;
            aiHeader.BackColor = Ink;
            aiTitle.Text = "IA";
            aiTitle.ForeColor = Color.White;
            aiTitle.Left = 12;
            aiTitle.Top = 11;
            aiTitle.AutoSize = true;
            aiTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            aiHeader.Controls.Add(aiTitle);
            aiClose.Text = "×";
            aiClose.Width = 38;
            aiClose.Height = 30;
            aiClose.Top = 5;
            aiClose.Left = 320;
            aiClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            aiClose.FlatStyle = FlatStyle.Flat;
            aiClose.FlatAppearance.BorderSize = 0;
            aiClose.ForeColor = Color.White;
            aiClose.BackColor = Ink;
            aiClose.Font = new Font("Segoe UI", 13f);
            aiClose.Click += delegate { HideAI(); };
            aiHeader.Controls.Add(aiClose);
            workspace.Panel2.Controls.Add(aiHeader);

            aiWeb.Dock = DockStyle.Fill;
            workspace.Panel2.Controls.Add(aiWeb);
            aiWeb.BringToFront();
            aiHeader.BringToFront();
            HideAI();

            back.Click += delegate { var w = Active(); if (w != null && w.CanGoBack) w.GoBack(); };
            forward.Click += delegate { var w = Active(); if (w != null && w.CanGoForward) w.GoForward(); };
            reload.Click += delegate { var w = Active(); if (w != null) w.Reload(); };
            home.Click += delegate { Navigate("https://desarrollamo.com.ar/"); };
            plus.Click += async delegate { await AddTab("https://desarrollamo.com.ar/"); };
            Shown += async delegate
            {
                cpuCounter.NextValue();
                await AddTab("https://desarrollamo.com.ar/");
                monitorTimer.Start();
            };
            FormClosed += delegate { monitorTimer.Stop(); cpuCounter.Dispose(); };
            KeyDown += BrowserForm_KeyDown;

            monitorTimer.Interval = 2500;
            monitorTimer.Tick += async delegate { await UpdateMetrics(); };
        }

        void ConfigureMetric(Label label, string text, int left)
        {
            label.Text = text;
            label.Left = left;
            label.Top = 10;
            label.Width = 95;
            label.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            label.ForeColor = Ink;
            brand.Controls.Add(label);
        }

        void AddNavButton(Button b, string text, int left)
        {
            b.Text = text;
            b.Left = left;
            b.Top = 7;
            b.Width = 38;
            b.Height = 32;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Pink;
            b.BackColor = Ink;
            b.ForeColor = text == "+" ? Pink : Sky;
            b.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            nav.Controls.Add(b);
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
            workspace.SplitterDistance = Math.Max(520, Width - 420);
            aiTitle.Text = "IA · " + name;
            if (!aiReady)
            {
                string data = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DesarrollAMO", "Browser", "AI");
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

        async Task AddTab(string url)
        {
            var page = new TabPage("Nueva pestaña");
            var web = new WebView2 { Dock = DockStyle.Fill };
            page.Controls.Add(web);
            tabs.TabPages.Add(page);
            tabs.SelectedTab = page;

            string data = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesarrollAMO", "Browser", "WebView2");
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
            };
            web.CoreWebView2.SourceChanged += delegate
            {
                if (tabs.SelectedTab == page) address.Text = web.Source == null ? "" : web.Source.ToString();
            };
            web.CoreWebView2.DocumentTitleChanged += delegate
            {
                string title = web.CoreWebView2.DocumentTitle;
                page.Text = ShortTitle(title);
                if (tabs.SelectedTab == page) Text = title + " — DesarrollAMO Browser";
            };
            web.CoreWebView2.NewWindowRequested += async delegate(object s, CoreWebView2NewWindowRequestedEventArgs e)
            {
                e.Handled = true;
                await AddTab(e.Uri);
            };
            web.CoreWebView2.NavigationCompleted += delegate { if (tabs.SelectedTab == page) SyncActive(); };
            web.Source = new Uri(Normalize(url));
        }

        void EnableInspectMenu(WebView2 web)
        {
            web.CoreWebView2.ContextMenuRequested += delegate(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
            {
                var inspect = web.CoreWebView2.Environment.CreateContextMenuItem("Inspeccionar", null, CoreWebView2ContextMenuItemKind.Command);
                inspect.CustomItemSelected += delegate { BeginInvoke(new Action(delegate { web.CoreWebView2.OpenDevToolsWindow(); })); };
                e.MenuItems.Insert(0, inspect);
            };
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
            var w = Active();
            if (w == null || w.CoreWebView2 == null) return;
            address.Text = w.Source == null ? "" : w.Source.ToString();
            back.Enabled = w.CanGoBack;
            forward.Enabled = w.CanGoForward;
            Text = w.CoreWebView2.DocumentTitle + " — DesarrollAMO Browser";
        }

        string ShortTitle(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return "Nueva pestaña";
            return s.Length <= 24 ? s : s.Substring(0, 23) + "…";
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

