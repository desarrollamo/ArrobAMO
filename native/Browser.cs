using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
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
        readonly Color Ink = AmoTheme.SurfaceDark;
        readonly Color Ink2 = AmoTheme.SurfaceDark2;
        readonly Color Ink3 = AmoTheme.SurfaceDark3;
        readonly Color Soft = AmoTheme.BorderStrong;
        readonly Color TextColor = AmoTheme.TextInverse;
        readonly Color Muted = AmoTheme.TextMuted;
        readonly Color Danger = AmoTheme.Danger;

        readonly Panel brand = new Panel();
        readonly Panel nav = new Panel();
        readonly Panel side = new Panel();
        readonly Panel statusLine = new Panel();
        readonly AmoSpinner navSpinner = new AmoSpinner();
        readonly Label logo = new Label();
        readonly Label cpuLabel = new Label();
        readonly Label ramLabel = new Label();
        readonly Label gpuLabel = new Label();
        readonly Label loopState = new Label();
        readonly Button aiButton = new Button();
        readonly Button plus = new Button();
        readonly Button back = new Button();
        readonly Button forward = new Button();
        readonly Button reload = new Button();
        readonly Button home = new Button();
        readonly Button historyButton = new Button();
        readonly Button bookmarkButton = new Button();
        readonly Button menuButton = new Button();
        readonly Label securityLabel = new Label();
        readonly Button scriptsButton = new Button();
        readonly Button loopButton = new Button();
        readonly Button uiKitButton = new Button();
        readonly Button sideToggle = new Button();
        readonly TextBox address = new TextBox();
        readonly TabControl tabs = new TabControl();
        readonly SplitContainer workspace = new SplitContainer();
        readonly Panel aiHeader = new Panel();
        readonly Label aiTitle = new Label();
        readonly Button aiClose = new Button();
        readonly Button aiOpenTab = new Button();
        readonly WebView2 aiWeb = new WebView2();
        readonly ContextMenuStrip aiMenu = new ContextMenuStrip();
        readonly ContextMenuStrip historyMenu = new ContextMenuStrip();
        readonly ContextMenuStrip scriptsMenu = new ContextMenuStrip();
        readonly ContextMenuStrip loopMenu = new ContextMenuStrip();
        readonly ContextMenuStrip mainMenu = new ContextMenuStrip();
        readonly ContextMenuStrip bookmarksMenu = new ContextMenuStrip();
        readonly Timer monitorTimer = new Timer();
        readonly PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        readonly List<string> history = new List<string>();
        readonly List<string> bookmarks = new List<string>();
        readonly List<string> recordingSteps = new List<string>();
        readonly string startupUrl;
        readonly string startupScript;
        readonly bool startupMinimized;
        readonly bool startupRecord;

        CoreWebView2Environment sharedEnvironment;
        bool aiReady;
        bool recording;
        bool replaying;
        bool stopRequested;
        string lastScriptPath = "";
        bool closingTab;
        bool sidebarExpanded;
        bool pageLoading;
        string DataRoot
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArrobAMO");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }
        string HistoryFile { get { return Path.Combine(DataRoot, "history.txt"); } }
        string BookmarksFile { get { return Path.Combine(DataRoot, "bookmarks.txt"); } }
        string ScriptsDir
        {
            get
            {
                var dir = Path.Combine(DataRoot, "Scripts");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public BrowserForm(string initialUrl, string initialScript, bool initialMinimized, bool initialRecord)
        {
            startupUrl = String.IsNullOrWhiteSpace(initialUrl) ? "https://desarrollamo.com.ar/" : initialUrl;
            startupScript = initialScript;
            startupMinimized = initialMinimized;
            startupRecord = initialRecord;
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
            BuildMenus();
            LoadHistory();

            back.Click += delegate { var w = Active(); if (w != null && w.CanGoBack) w.GoBack(); };
            forward.Click += delegate { var w = Active(); if (w != null && w.CanGoForward) w.GoForward(); };
            reload.Click += delegate { var w = Active(); if (w != null) { if (pageLoading && w.CoreWebView2 != null) w.CoreWebView2.Stop(); else w.Reload(); } };
            home.Click += delegate { Navigate("https://desarrollamo.com.ar/"); };
            plus.Click += async delegate { await AddTab("https://desarrollamo.com.ar/"); };
            aiButton.Click += delegate { aiMenu.Show(aiButton, new Point(0, aiButton.Height)); };
            historyButton.Click += delegate { ShowHistory(); };
            bookmarkButton.Click += delegate { ToggleBookmark(); };
            menuButton.Click += delegate { mainMenu.Show(menuButton, new Point(0, menuButton.Height)); };
            uiKitButton.Click += delegate { ShowUIKit(); };
            sideToggle.Click += delegate { ToggleSidebar(); };
            scriptsButton.Click += delegate { ShowScriptsMenu(); };
            loopButton.Click += delegate { ShowLoopMenu(); };

            Shown += async delegate
            {
                cpuCounter.NextValue();
                await EnsureEnvironment();
                SetCueBanner(address, "Buscar o escribir una dirección");
                UpdateResponsiveChrome();
                await AddTab(startupUrl);
                monitorTimer.Start();
                if (startupRecord) StartRecording();
                if (!String.IsNullOrWhiteSpace(startupScript) && File.Exists(startupScript)) await RunScript(startupScript, startupMinimized);
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
            Resize += delegate { UpdateResponsiveChrome(); };
        }

        async Task EnsureEnvironment()
        {
            if (sharedEnvironment != null) return;
            string profile = Path.Combine(DataRoot, "WebView2");
            Directory.CreateDirectory(profile);
            sharedEnvironment = await CoreWebView2Environment.CreateAsync(null, profile, null);
        }

        void UpdateResponsiveChrome()
        {
            bool narrow = ClientSize.Width < 1050;
            cpuLabel.Visible = !narrow;
            ramLabel.Visible = !narrow;
            gpuLabel.Visible = !narrow;
            loopState.Visible = ClientSize.Width >= 1180;
            home.Visible = ClientSize.Width >= 900;
            securityLabel.Visible = ClientSize.Width >= 760;
            int addressLeft = securityLabel.Visible ? 234 : 184;
            address.Left = addressLeft;
            address.Width = Math.Max(160, ClientSize.Width - addressLeft - 206);
            statusLine.Left = address.Left;
            statusLine.Width = pageLoading ? address.Width : 0;
        }

        void SetCueBanner(TextBox box, string text)
        {
            try { SendMessage(box.Handle, 0x1501, (IntPtr)1, text); } catch { }
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

            securityLabel.Left = 184;
            securityLabel.Top = 15;
            securityLabel.Width = 44;
            securityLabel.Height = 20;
            securityLabel.Text = "WEB";
            securityLabel.TextAlign = ContentAlignment.MiddleCenter;
            securityLabel.Font = AmoTheme.UI(7.5f, FontStyle.Bold);
            securityLabel.ForeColor = AmoTheme.TextMuted;
            nav.Controls.Add(securityLabel);

            address.Left = 234;
            address.Top = 10;
            address.Height = 30;
            address.Width = ClientSize.Width - 440;
            address.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            address.Font = AmoTheme.UI(10f);
            address.BorderStyle = BorderStyle.FixedSingle;
            address.BackColor = Ink3;
            address.ForeColor = TextColor;
            address.KeyDown += Address_KeyDown;
            AmoTheme.StyleInput(address, true);
            nav.Controls.Add(address);

            navSpinner.Left = ClientSize.Width - 198;
            navSpinner.Top = 13;
            navSpinner.Width = 22;
            navSpinner.Height = 22;
            navSpinner.ForeColor = Color.White;
            navSpinner.BackColor = Ink2;
            navSpinner.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            navSpinner.Visible = false;
            nav.Controls.Add(navSpinner);

            plus.Text = "+";
            bookmarkButton.Text = ((char)0x2606).ToString();
            bookmarkButton.Left = ClientSize.Width - 166;
            bookmarkButton.Top = 9;
            bookmarkButton.Width = 38;
            bookmarkButton.Height = 32;
            bookmarkButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bookmarkButton.FlatStyle = FlatStyle.Flat;
            bookmarkButton.FlatAppearance.BorderSize = 0;
            bookmarkButton.BackColor = Ink2;
            bookmarkButton.ForeColor = TextColor;
            bookmarkButton.Font = new Font("Segoe UI Symbol", 12f);
            nav.Controls.Add(bookmarkButton);

            menuButton.Text = ((char)0x22EF).ToString();
            menuButton.Left = ClientSize.Width - 126;
            menuButton.Top = 9;
            menuButton.Width = 34;
            menuButton.Height = 32;
            menuButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            menuButton.FlatStyle = FlatStyle.Flat;
            menuButton.FlatAppearance.BorderSize = 0;
            menuButton.BackColor = Ink2;
            menuButton.ForeColor = TextColor;
            nav.Controls.Add(menuButton);

            plus.Left = ClientSize.Width - 86;
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
            AddSideButton("Scripts", ((char)0x25A3).ToString(), 96, delegate { ShowScriptsMenu(); }, scriptsButton);
            AddSideButton("Activar Loop", ((char)0x2699).ToString(), 140, delegate { ShowLoopMenu(); }, loopButton);
            AddSideButton("IA", ((char)0x2726).ToString(), 184, delegate { aiMenu.Show(side, new Point(side.Width, 184)); });
            AddSideButton("UI Kit", "UI", 228, delegate { ShowUIKit(); }, uiKitButton);
            AddSideButton("Expandir sidebar", ((char)0x226B).ToString(), 272, delegate { ToggleSidebar(); }, sideToggle);
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

        void BuildMenus()
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

            scriptsMenu.BackColor = Ink2;
            scriptsMenu.ForeColor = TextColor;
            AddScriptMenuItem("Redactar script", delegate { EditScript(null); });
            AddScriptMenuItem("Importar script", delegate { ImportScript(); });
            AddScriptMenuItem("Exportar último script", delegate { ExportLastScript(); });
            AddScriptMenuItem("Mis scripts", delegate { ShowScriptManager(); });

            loopMenu.BackColor = Ink2;
            loopMenu.ForeColor = TextColor;
            AddLoopMenuItem("Activar Loop / grabar", delegate { StartRecording(); });
            AddLoopMenuItem("Detener y guardar Loop", delegate { StopRecordingAndSave(); });
            AddLoopMenuItem("Ejecutar último Loop", async delegate { await RunLastScript(false); });
            AddLoopMenuItem("Ejecutar último minimizado", async delegate { await RunLastScript(true); });
            AddLoopMenuItem("Repetir último Loop...", async delegate { await RunLoopPrompt(); });
            AddLoopMenuItem("Detener ejecución", delegate { stopRequested = true; });

            AmoTheme.StyleMenu(mainMenu);
            AddMainMenuItem("Nueva pestaña", async delegate { await AddTab("https://desarrollamo.com.ar/"); });
            AddMainMenuItem("Historial", delegate { ShowHistory(); });
            AddMainMenuItem("Marcadores", delegate { ShowBookmarks(); });
            AddMainMenuItem("Scripts", delegate { ShowScriptsMenu(); });
            AddMainMenuItem("Automatizaciones", delegate { ShowLoopMenu(); });
            AddMainMenuItem("UI Kit", delegate { ShowUIKit(); });
            AmoTheme.StyleMenu(bookmarksMenu);
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
            b.Tag = glyph + "|" + tooltip;
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


        void ToggleSidebar()
        {
            sidebarExpanded = !sidebarExpanded;
            side.Width = sidebarExpanded ? 190 : 54;
            foreach (Control raw in side.Controls)
            {
                var b = raw as Button;
                if (b == null || b.Tag == null) continue;
                string tag = Convert.ToString(b.Tag);
                int sep = tag.IndexOf('|');
                if (sep < 0) continue;
                string glyph = tag.Substring(0, sep);
                string label = tag.Substring(sep + 1);
                b.Left = 7;
                b.Width = sidebarExpanded ? 176 : 40;
                b.TextAlign = sidebarExpanded ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter;
                b.Padding = sidebarExpanded ? new Padding(10,0,0,0) : new Padding(0);
                b.Text = sidebarExpanded ? glyph + "   " + label : glyph;
            }
            sideToggle.Text = sidebarExpanded ? ((char)0x226A).ToString() + "   Colapsar" : ((char)0x226B).ToString();
        }

        void ShowUIKit()
        {
            var f = new Form();
            f.Text = "UI Kit · ArrobAMO";
            f.Width = 980;
            f.Height = 720;
            f.StartPosition = FormStartPosition.CenterParent;
            f.BackColor = AmoTheme.Bg;
            f.ForeColor = AmoTheme.Text;
            f.Font = AmoTheme.UI(9f);

            var head = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = AmoTheme.Surface };
            var title = new Label { Text = "ArrobAMO · Sistema visual", Left = 24, Top = 16, Width = 520, Height = 30, Font = AmoTheme.UI(18f, FontStyle.Bold), ForeColor = AmoTheme.Text };
            var sub = new Label { Text = "Componentes reales usados por el navegador", Left = 25, Top = 48, Width = 520, Height = 20, ForeColor = AmoTheme.TextSoft };
            head.Controls.Add(title); head.Controls.Add(sub);
            f.Controls.Add(head);

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(22), BackColor = AmoTheme.Bg, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            Func<string, Panel> section = delegate(string name)
            {
                var p = new Panel { Width = 430, Height = 210, BackColor = AmoTheme.Surface, Margin = new Padding(8), Padding = new Padding(16) };
                var l = new Label { Text = name, Left = 16, Top = 14, Width = 360, Height = 26, Font = AmoTheme.UI(11f, FontStyle.Bold), ForeColor = AmoTheme.Text };
                p.Controls.Add(l);
                return p;
            };

            var buttons = section("Botones");
            var b1 = new AmoButton { Text = "Primario", Variant = AmoButtonVariant.Primary, Left = 16, Top = 56, Width = 110 };
            var b2 = new AmoButton { Text = "Secundario", Variant = AmoButtonVariant.Secondary, Left = 136, Top = 56, Width = 110 };
            var b3 = new AmoButton { Text = "Outline", Variant = AmoButtonVariant.Outline, Left = 256, Top = 56, Width = 100 };
            var b4 = new AmoButton { Text = "Peligro", Variant = AmoButtonVariant.Danger, Left = 16, Top = 106, Width = 110 };
            var b5 = new AmoButton { Text = "Deshabilitado", Variant = AmoButtonVariant.Secondary, Left = 136, Top = 106, Width = 130, Enabled = false };
            buttons.Controls.Add(b1); buttons.Controls.Add(b2); buttons.Controls.Add(b3); buttons.Controls.Add(b4); buttons.Controls.Add(b5);

            var status = section("Estados y feedback");
            var ok = new AmoBadge { Text = "Correcto", Left = 16, Top = 58, Width = 100, ForeColor = AmoTheme.Success, BadgeColor = AmoTheme.SuccessSoft };
            var er = new AmoBadge { Text = "Error", Left = 126, Top = 58, Width = 90, ForeColor = AmoTheme.Danger, BadgeColor = AmoTheme.DangerSoft };
            var wa = new AmoBadge { Text = "Advertencia", Left = 226, Top = 58, Width = 120, ForeColor = AmoTheme.Warning, BadgeColor = AmoTheme.WarningSoft };
            var inf = new AmoBadge { Text = "Información", Left = 16, Top = 96, Width = 110, ForeColor = AmoTheme.Info, BadgeColor = AmoTheme.InfoSoft };
            status.Controls.Add(ok); status.Controls.Add(er); status.Controls.Add(wa); status.Controls.Add(inf);

            var loaders = section("Cargas");
            var spin16 = new AmoSpinner { Left = 18, Top = 58, Width = 20, Height = 20, ForeColor = AmoTheme.Text, BackColor = AmoTheme.Surface };
            var spin32 = new AmoSpinner { Left = 58, Top = 50, Width = 36, Height = 36, ForeColor = AmoTheme.Text, BackColor = AmoTheme.Surface };
            var prog = new AmoProgress { Left = 16, Top = 112, Width = 350, Height = 4, Value = 72, ForeColor = AmoTheme.Text };
            var loadText = new Label { Text = "Cargando ArrobAMO…", Left = 112, Top = 58, Width = 180, ForeColor = AmoTheme.TextSoft };
            loaders.Controls.Add(spin16); loaders.Controls.Add(spin32); loaders.Controls.Add(prog); loaders.Controls.Add(loadText);

            var forms = section("Formularios");
            var input = new TextBox { Left = 16, Top = 58, Width = 250, Height = 36, Text = "Buscar o escribir una dirección" };
            AmoTheme.StyleInput(input, false);
            var check = new CheckBox { Left = 16, Top = 112, Width = 160, Text = "Checkbox", ForeColor = AmoTheme.Text, BackColor = AmoTheme.Surface };
            var radio = new RadioButton { Left = 190, Top = 112, Width = 150, Text = "Radio activo", Checked = true, ForeColor = AmoTheme.Text, BackColor = AmoTheme.Surface };
            forms.Controls.Add(input); forms.Controls.Add(check); forms.Controls.Add(radio);

            flow.Controls.Add(buttons); flow.Controls.Add(status); flow.Controls.Add(loaders); flow.Controls.Add(forms);
            f.Controls.Add(flow);
            flow.BringToFront();
            f.Show(this);
        }

        void ShowToast(string message, Color accent)
        {
            var toast = new AmoToastForm(message, accent, 3200);
            toast.Show();
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
                var deferral = e.GetDeferral();
                try
                {
                    var popupWeb = await AddPopupTab();
                    e.NewWindow = popupWeb.CoreWebView2;
                }
                finally { deferral.Complete(); }
            };

            web.CoreWebView2.NavigationCompleted += delegate
            {
                SetLoading(false);
                if (tabs.SelectedTab == page) SyncActive();
                AddHistory(web.Source == null ? "" : web.Source.ToString(), web.CoreWebView2.DocumentTitle);
            };

            web.Source = new Uri(Normalize(url));
        }

        async Task<WebView2> AddPopupTab()
        {
            var page = new TabPage("Nueva ventana");
            page.BackColor = Ink;
            page.ForeColor = TextColor;
            var web = new WebView2 { Dock = DockStyle.Fill };
            page.Controls.Add(web);
            tabs.TabPages.Add(page);
            tabs.SelectedTab = page;
            await EnsureEnvironment();
            await web.EnsureCoreWebView2Async(sharedEnvironment);
            web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            web.CoreWebView2.Settings.AreDevToolsEnabled = true;
            web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            web.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            EnableInspectMenu(web);
            HookPopupWebView(web, page);
            return web;
        }

        void HookPopupWebView(WebView2 web, TabPage page)
        {
            web.CoreWebView2.SourceChanged += delegate { if (tabs.SelectedTab == page) address.Text = web.Source == null ? "" : web.Source.ToString(); };
            web.CoreWebView2.DocumentTitleChanged += delegate { page.Text = ShortTitle(web.CoreWebView2.DocumentTitle); tabs.Invalidate(); };
            web.CoreWebView2.NavigationCompleted += delegate { if (tabs.SelectedTab == page) SyncActive(); AddHistory(web.Source == null ? "" : web.Source.ToString(), web.CoreWebView2.DocumentTitle); };
            web.CoreWebView2.WindowCloseRequested += delegate { BeginInvoke(new Action(delegate { var p = tabs.TabPages.Cast<TabPage>().FirstOrDefault(x => x.Controls.Contains(web)); if (p != null) CloseTab(tabs.TabPages.IndexOf(p)); })); };
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
            pageLoading = active;
            statusLine.Width = active ? address.Width : 0;
            navSpinner.Visible = active;
            reload.Text = active ? ((char)0x00D7).ToString() : ((char)0x21BB).ToString();
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



        void AddMainMenuItem(string text, EventHandler action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += action;
            mainMenu.Items.Add(item);
        }

        void LoadBookmarks()
        {
            try
            {
                if (File.Exists(BookmarksFile))
                    bookmarks.AddRange(File.ReadAllLines(BookmarksFile).Where(x => !String.IsNullOrWhiteSpace(x)).Take(200));
            }
            catch { }
        }

        void SaveBookmarks()
        {
            try { File.WriteAllLines(BookmarksFile, bookmarks.Take(200).ToArray()); } catch { }
        }

        void ToggleBookmark()
        {
            var w = Active();
            if (w == null || w.Source == null) return;
            string url = w.Source.ToString();
            string existing = bookmarks.FirstOrDefault(x => x.EndsWith("|" + url, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                bookmarks.Remove(existing);
                bookmarkButton.Text = ((char)0x2606).ToString();
                SaveBookmarks();
                ShowToast("Marcador eliminado.", AmoTheme.Info);
            }
            else
            {
                string title = w.CoreWebView2 == null ? url : w.CoreWebView2.DocumentTitle;
                bookmarks.Insert(0, (String.IsNullOrWhiteSpace(title) ? url : title.Replace("|"," ")) + "|" + url);
                if (bookmarks.Count > 200) bookmarks.RemoveRange(200, bookmarks.Count - 200);
                bookmarkButton.Text = ((char)0x2605).ToString();
                SaveBookmarks();
                ShowToast("Sitio agregado a marcadores.", AmoTheme.Success);
            }
        }

        void ShowBookmarks()
        {
            bookmarksMenu.Items.Clear();
            AmoTheme.StyleMenu(bookmarksMenu);
            if (bookmarks.Count == 0)
            {
                bookmarksMenu.Items.Add(new ToolStripMenuItem("Sin marcadores") { Enabled = false });
            }
            else
            {
                foreach (string row in bookmarks.Take(20))
                {
                    var p = row.Split(new[] { '|' }, 2);
                    string title = p.Length > 1 ? p[0] : p[0];
                    string url = p.Length > 1 ? p[1] : p[0];
                    var item = new ToolStripMenuItem(ShortTitle(title));
                    item.ToolTipText = url;
                    item.Click += delegate { Navigate(url); };
                    bookmarksMenu.Items.Add(item);
                }
            }
            bookmarksMenu.Show(bookmarkButton, new Point(0, bookmarkButton.Height));
        }

        void UpdateSecurityState()
        {
            var w = Active();
            if (w == null || w.Source == null)
            {
                securityLabel.Text = "WEB";
                securityLabel.ForeColor = AmoTheme.TextMuted;
                return;
            }

            var uri = w.Source;
            if (String.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                securityLabel.Text = "HTTPS";
                securityLabel.ForeColor = AmoTheme.Success;
            }
            else if (String.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase))
            {
                securityLabel.Text = "HTTP";
                securityLabel.ForeColor = AmoTheme.Warning;
            }
            else
            {
                securityLabel.Text = "LOCAL";
                securityLabel.ForeColor = AmoTheme.Info;
            }

            string url = uri.ToString();
            bookmarkButton.Text = bookmarks.Any(x => x.EndsWith("|" + url, StringComparison.OrdinalIgnoreCase))
                ? ((char)0x2605).ToString()
                : ((char)0x2606).ToString();
        }
        void AddScriptMenuItem(string text, EventHandler action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += action;
            scriptsMenu.Items.Add(item);
        }

        void AddLoopMenuItem(string text, EventHandler action)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += action;
            loopMenu.Items.Add(item);
        }

        void ShowScriptsMenu()
        {
            scriptsMenu.Show(side, new Point(side.Width, 96));
        }

        void ShowLoopMenu()
        {
            bool has = !String.IsNullOrWhiteSpace(lastScriptPath) && File.Exists(lastScriptPath);
            foreach (ToolStripItem raw in loopMenu.Items)
            {
                var item = raw as ToolStripMenuItem;
                if (item == null) continue;
                string t = item.Text ?? "";
                if (t.StartsWith("Activar Loop")) item.Enabled = !recording && !replaying;
                else if (t.StartsWith("Detener y guardar")) item.Enabled = recording;
                else if (t.StartsWith("Detener ejecución")) item.Enabled = replaying;
                else item.Enabled = has && !recording;
            }
            loopMenu.Show(side, new Point(side.Width, 140));
        }

        async Task InstallRecorder(WebView2 web)
        {
            string js = @"(function(){
if(window.__arrobamoRecorder)return;
window.__arrobamoRecorder=true;
function clean(s){return String(s||'').replace(/[\t\r\n]/g,' ');}
function css(el){
 if(!el)return '';
 if(el.id)return '#'+CSS.escape(el.id);
 var p=[],n=el;
 while(n&&n.nodeType===1&&p.length<5){
  var s=n.tagName.toLowerCase();
  if(n.classList&&n.classList.length)s+='.'+Array.from(n.classList).slice(0,2).map(CSS.escape).join('.');
  var par=n.parentElement;
  if(par){var same=Array.from(par.children).filter(x=>x.tagName===n.tagName);if(same.length>1)s+=':nth-of-type('+(same.indexOf(n)+1)+')';}
  p.unshift(s); n=par;
 }
 return p.join(' > ');
}
document.addEventListener('click',function(ev){
 var el=ev.target.closest('a,button,input,[role=""button""],[onclick]');
 if(el)chrome.webview.postMessage('CLICK\t'+clean(css(el)));
},true);
document.addEventListener('change',function(ev){
 var el=ev.target;
 if(!el||!('value' in el))return;
 if(String(el.type||'').toLowerCase()==='password')return;
 chrome.webview.postMessage('INPUT\t'+clean(css(el))+'\t'+clean(el.value));
},true);
})();";
            await web.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(js);
            try { await web.ExecuteScriptAsync(js); } catch { }
        }

        void StartRecording()
        {
            recordingSteps.Clear();
            recording = true;
            loopState.Text = "Loop grabando";
            loopState.ForeColor = Danger;
            var w = Active();
            if (w != null && w.Source != null) RecordStep("NAV", w.Source.ToString(), "");
        }

        void StopRecordingAndSave()
        {
            if (!recording) return;
            recording = false;
            loopState.Text = "Loop detenido";
            loopState.ForeColor = Muted;
            if (recordingSteps.Count == 0) { MessageBox.Show("No se registraron acciones.", "ArrobAMO"); return; }
            string name = PromptText("Guardar Loop", "Nombre", "Loop " + DateTime.Now.ToString("yyyy-MM-dd HH-mm"));
            if (String.IsNullOrWhiteSpace(name)) return;
            string path = Path.Combine(ScriptsDir, SafeFileName(name) + ".arrobamo");
            File.WriteAllLines(path, recordingSteps.ToArray(), Encoding.UTF8);
            lastScriptPath = path;
            MessageBox.Show("Loop guardado: " + Path.GetFileName(path), "ArrobAMO");
        }

        void RecordStep(string type, string a, string b)
        {
            if (!recording || replaying) return;
            string row = type + "|" + B64(a ?? "") + "|" + B64(b ?? "");
            if (recordingSteps.Count == 0 || recordingSteps[recordingSteps.Count - 1] != row)
                recordingSteps.Add(row);
        }

        async Task RunLastScript(bool minimized)
        {
            if (String.IsNullOrWhiteSpace(lastScriptPath) || !File.Exists(lastScriptPath))
            {
                var files = Directory.GetFiles(ScriptsDir, "*.arrobamo").OrderByDescending(File.GetLastWriteTime).ToArray();
                if (files.Length == 0) { MessageBox.Show("Todavía no hay scripts.", "ArrobAMO"); return; }
                lastScriptPath = files[0];
            }
            stopRequested = false;
            await RunScript(lastScriptPath, minimized);
        }

        async Task RunScript(string path, bool minimized)
        {
            var web = Active();
            if (web == null || web.CoreWebView2 == null || !File.Exists(path)) return;
            replaying = true;
            var oldState = WindowState;
            if (minimized) WindowState = FormWindowState.Minimized;
            try
            {
                foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    if (stopRequested) break;
                    if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string cmd = "", a = "", b = "";
                    if (line.Contains("|"))
                    {
                        var p = line.Split('|');
                        cmd = p[0].Trim().ToUpperInvariant();
                        a = p.Length > 1 ? UB64(p[1]) : "";
                        b = p.Length > 2 ? UB64(p[2]) : "";
                    }
                    else
                    {
                        int sp = line.IndexOf(' ');
                        cmd = (sp < 0 ? line : line.Substring(0, sp)).Trim().ToUpperInvariant();
                        string rest = sp < 0 ? "" : line.Substring(sp + 1).Trim();
                        if (cmd == "INPUT")
                        {
                            int sep = rest.IndexOf(" => ", StringComparison.Ordinal);
                            if (sep >= 0)
                            {
                                a = rest.Substring(0, sep).Trim();
                                b = rest.Substring(sep + 4);
                            }
                            else a = rest;
                        }
                        else a = rest;
                    }

                    if (cmd == "NAV")
                    {
                        await NavigateAndWait(web, Normalize(a));
                    }
                    else if (cmd == "CLICK")
                    {
                        await web.ExecuteScriptAsync("(function(){var e=document.querySelector(" + Js(a) + ");if(e){e.click();return true;}return false;})();");
                        await Task.Delay(700);
                    }
                    else if (cmd == "INPUT")
                    {
                        await web.ExecuteScriptAsync("(function(){var e=document.querySelector(" + Js(a) + ");if(!e)return false;e.focus();e.value=" + Js(b) + ";e.dispatchEvent(new Event('input',{bubbles:true}));e.dispatchEvent(new Event('change',{bubbles:true}));return true;})();");
                        await Task.Delay(350);
                    }
                    else if (cmd == "WAIT")
                    {
                        int ms;
                        if (Int32.TryParse(a, out ms))
                            await Task.Delay(Math.Max(0, Math.Min(ms, 60000)));
                    }
                }
            }
            finally
            {
                replaying = false;
                if (minimized && oldState != FormWindowState.Minimized) WindowState = oldState;
            }
        }
        async Task RunLoopPrompt()
        {
            if (String.IsNullOrWhiteSpace(lastScriptPath) || !File.Exists(lastScriptPath))
            {
                var files=Directory.GetFiles(ScriptsDir,"*.arrobamo").OrderByDescending(File.GetLastWriteTime).ToArray();
                if(files.Length==0){MessageBox.Show("Todavía no hay Loops guardados.","ArrobAMO");return;}
                lastScriptPath=files[0];
            }
            string raw=PromptText("Repetir Loop","Cantidad de repeticiones","3");
            int count; if(!Int32.TryParse(raw,out count)||count<1)return; count=Math.Min(count,1000);
            stopRequested=false;
            for(int i=0;i<count && !stopRequested;i++) await RunScript(lastScriptPath,false);
        }

        Task NavigateAndWait(WebView2 web, string url)
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler<CoreWebView2NavigationCompletedEventArgs> done = null;
            done = delegate(object s, CoreWebView2NavigationCompletedEventArgs e)
            {
                web.CoreWebView2.NavigationCompleted -= done;
                tcs.TrySetResult(e.IsSuccess);
            };
            web.CoreWebView2.NavigationCompleted += done;
            web.Source = new Uri(url);
            return Task.WhenAny(tcs.Task, Task.Delay(20000));
        }

        void EditScript(string path)
        {
            using (var f = new Form())
            {
                f.Text = "Script - ArrobAMO"; f.Width = 760; f.Height = 580; f.StartPosition = FormStartPosition.CenterParent;
                var editor = new TextBox { Multiline=true, ScrollBars=ScrollBars.Both, WordWrap=false, Dock=DockStyle.Fill, Font=new Font("Consolas",10f) };
                var bar = new Panel { Dock=DockStyle.Bottom, Height=46 };
                var save = new Button { Text="Guardar", Left=555, Top=7, Width=85, Height=32 };
                var run = new Button { Text="Ejecutar", Left=645, Top=7, Width=85, Height=32 };
                editor.Text = path != null && File.Exists(path) ? File.ReadAllText(path,Encoding.UTF8) : "# ArrobAMO script\r\n# NAV https://ejemplo.com\r\n# CLICK button#enviar\r\n# INPUT input[name=email] => texto\r\n# WAIT 1000\r\n";
                save.Click += delegate
                {
                    if (String.IsNullOrWhiteSpace(path))
                    {
                        string n=PromptText("Guardar script","Nombre","Mi script"); if(String.IsNullOrWhiteSpace(n)) return;
                        path=Path.Combine(ScriptsDir,SafeFileName(n)+".arrobamo");
                    }
                    File.WriteAllText(path,editor.Text,Encoding.UTF8); lastScriptPath=path;
                };
                run.Click += async delegate { save.PerformClick(); if(!String.IsNullOrWhiteSpace(path)) await RunScript(path,false); };
                bar.Controls.Add(save); bar.Controls.Add(run); f.Controls.Add(editor); f.Controls.Add(bar); f.ShowDialog(this);
            }
        }

        void ImportScript()
        {
            using(var d=new OpenFileDialog())
            {
                d.Filter="ArrobAMO script (*.arrobamo)|*.arrobamo|Todos (*.*)|*.*";
                if(d.ShowDialog(this)!=DialogResult.OK)return;
                string dest=Path.Combine(ScriptsDir,Path.GetFileName(d.FileName)); File.Copy(d.FileName,dest,true); lastScriptPath=dest;
            }
        }

        void ExportLastScript()
        {
            if(String.IsNullOrWhiteSpace(lastScriptPath)||!File.Exists(lastScriptPath)){MessageBox.Show("No hay un script seleccionado.","ArrobAMO");return;}
            using(var d=new SaveFileDialog()){d.Filter="ArrobAMO script (*.arrobamo)|*.arrobamo";d.FileName=Path.GetFileName(lastScriptPath);if(d.ShowDialog(this)==DialogResult.OK)File.Copy(lastScriptPath,d.FileName,true);}
        }

        void ShowScriptManager()
        {
            using(var f=new Form())
            {
                f.Text="Mis scripts - ArrobAMO";f.Width=620;f.Height=420;f.StartPosition=FormStartPosition.CenterParent;
                var list=new ListBox{Left=12,Top=12,Width=580,Height=300,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom};
                foreach(var file in Directory.GetFiles(ScriptsDir,"*.arrobamo").OrderByDescending(File.GetLastWriteTime))list.Items.Add(file);
                var edit=new Button{Text="Editar",Left=12,Top=325,Width=90};var run=new Button{Text="Ejecutar",Left=108,Top=325,Width=90};var mini=new Button{Text="Minimizado",Left=204,Top=325,Width=100};
                edit.Click+=delegate{if(list.SelectedItem!=null)EditScript(list.SelectedItem.ToString());};
                run.Click+=async delegate{if(list.SelectedItem!=null){lastScriptPath=list.SelectedItem.ToString();await RunScript(lastScriptPath,false);}};
                mini.Click+=async delegate{if(list.SelectedItem!=null){lastScriptPath=list.SelectedItem.ToString();await RunScript(lastScriptPath,true);}};
                f.Controls.Add(list);f.Controls.Add(edit);f.Controls.Add(run);f.Controls.Add(mini);f.ShowDialog(this);
            }
        }

        string PromptText(string title,string labelText,string initial)
        {
            using(var f=new Form())
            {
                f.Text=title;f.Width=460;f.Height=150;f.StartPosition=FormStartPosition.CenterParent;
                var l=new Label{Left=12,Top=12,Width=420,Text=labelText};var b=new TextBox{Left=12,Top=36,Width=420,Text=initial};var ok=new Button{Text="Aceptar",Left=342,Top=70,Width=90,DialogResult=DialogResult.OK};
                f.Controls.Add(l);f.Controls.Add(b);f.Controls.Add(ok);f.AcceptButton=ok;return f.ShowDialog(this)==DialogResult.OK?b.Text.Trim():"";
            }
        }

        string SafeFileName(string s){foreach(char c in Path.GetInvalidFileNameChars())s=s.Replace(c,'_');return String.IsNullOrWhiteSpace(s)?"script":s.Trim();}
        string B64(string s){return Convert.ToBase64String(Encoding.UTF8.GetBytes(s??""));}
        string UB64(string s){try{return Encoding.UTF8.GetString(Convert.FromBase64String(s??""));}catch{return "";}}
        string Js(string s){if(s==null)s="";return "'" + s.Replace("\\","\\\\").Replace("'","\\'").Replace("\r","\\r").Replace("\n","\\n") + "'";}
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
            UpdateSecurityState();
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
                await EnsureEnvironment();
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

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        static void HandleUiException(Exception ex)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArrobAMO");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "errores.log"), DateTime.Now.ToString("s") + " " + ex + Environment.NewLine + Environment.NewLine);
            }
            catch { }
            MessageBox.Show("ArrobAMO encontró un problema y evitó cerrarse. El detalle técnico quedó guardado en errores.log.", "ArrobAMO", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e) { HandleUiException(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e) { try { HandleUiException(e.ExceptionObject as Exception ?? new Exception("Error no controlado")); } catch { } };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string url=null, script=null; bool mini=false, record=false;
            if(args!=null) foreach(string a in args)
            {
                if(a.StartsWith("--script=",StringComparison.OrdinalIgnoreCase)) script=a.Substring(9).Trim('"');
                else if(a.Equals("--minimized",StringComparison.OrdinalIgnoreCase)) mini=true;
                else if(a.Equals("--record",StringComparison.OrdinalIgnoreCase)) record=true;
                else if(!a.StartsWith("--") && url==null) url=a;
            }
            Application.Run(new BrowserForm(url, script, mini, record));
        }
    }
}































