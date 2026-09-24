using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using Microsoft.Win32;
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
        readonly AmoDownloadManager downloadManager;
        readonly Label logo = new Label();
        readonly OrbitMark voiceMark = new OrbitMark();
        readonly Label cpuLabel = new Label();
        readonly Label ramLabel = new Label();
        readonly Label gpuLabel = new Label();
        readonly Label cameraLabel = new Label();
        readonly Label microphoneLabel = new Label();
        readonly Label loopState = new Label();
        readonly Button aiButton = new Button();
        readonly Button plus = new Button();
        readonly Button back = new Button();
        readonly Button forward = new Button();
        readonly Button reload = new Button();
        readonly Button home = new Button();
        readonly Button historyButton = new Button();
        readonly Button bookmarkButton = new Button();
        readonly Button profileButton = new Button();
        readonly Button menuButton = new Button();
        readonly Label securityLabel = new Label();
        readonly Button scriptsButton = new Button();
        readonly Button loopButton = new Button();
        readonly Button uiKitButton = new Button();
        readonly Button downloadsButton = new Button();
        readonly Button settingsButton = new Button();
        readonly Button securityButton = new Button();
        readonly Button teamButton = new Button();
        readonly Button sideToggle = new Button();
        readonly TextBox address = new TextBox();
        readonly TabControl tabs = new TabControl();
        readonly SplitContainer workspace = new SplitContainer();
        readonly Panel aiHeader = new Panel();
        readonly Label aiTitle = new Label();
        readonly Label aiState = new Label();
        readonly AmoLoadingDots aiDots = new AmoLoadingDots();
        readonly Button aiClose = new Button();
        readonly Button aiOpenTab = new Button();
        readonly WebView2 aiWeb = new WebView2();
        readonly ContextMenuStrip aiMenu = new ContextMenuStrip();
        readonly ContextMenuStrip historyMenu = new ContextMenuStrip();
        readonly ContextMenuStrip scriptsMenu = new ContextMenuStrip();
        readonly ContextMenuStrip loopMenu = new ContextMenuStrip();
        readonly ContextMenuStrip mainMenu = new ContextMenuStrip();
        readonly ContextMenuStrip bookmarksMenu = new ContextMenuStrip();
        readonly ContextMenuStrip tabMenu = new ContextMenuStrip();
        readonly Timer monitorTimer = new Timer();
        readonly ToolTip tooltips = new ToolTip();
        readonly PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        readonly List<string> history = new List<string>();
        readonly List<string> bookmarks = new List<string>();
        readonly List<string> recordingSteps = new List<string>();
        readonly Dictionary<TabPage, Image> tabFavicons = new Dictionary<TabPage, Image>();
        readonly HashSet<TabPage> loadingTabs = new HashSet<TabPage>();
        readonly HashSet<TabPage> pinnedTabs = new HashSet<TabPage>();
        readonly Stack<string> closedTabs = new Stack<string>();
        readonly string startupUrl;
        readonly string startupScript;
        readonly bool startupMinimized;
        readonly bool startupRecord;
        readonly bool startupImport;
        readonly bool startupAI;
        readonly bool startupElite;
        readonly bool startupUrlExplicit;
        readonly bool privateMode;
        readonly string privateRoot;
        readonly AmoSettings settings;
        readonly AmoEnterpriseStore enterpriseStore;
        AmoSecurityManager securityManager;

        CoreWebView2Environment sharedEnvironment;
        bool aiReady;
        bool recording;
        bool replaying;
        bool stopRequested;
        string lastScriptPath = "";
        bool closingTab;
        bool sidebarExpanded;
        bool pageLoading;
        int hoverTabIndex = -1;
        string DataRoot
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArrobAMO");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }
        string SettingsFile { get { return Path.Combine(DataRoot, "settings.ini"); } }
        string SessionFile { get { return Path.Combine(DataRoot, "session.txt"); } }
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

        public BrowserForm(string initialUrl, string initialScript, bool initialMinimized, bool initialRecord, bool initialImport, bool initialAI, bool initialElite, bool isPrivate)
        {
            downloadManager = new AmoDownloadManager(delegate(string uri)
            {
                if (String.IsNullOrWhiteSpace(uri)) return;
                BeginInvoke(new Action(delegate { Navigate(uri); }));
            });
            privateMode = isPrivate;
            privateRoot = privateMode
                ? Path.Combine(Path.GetTempPath(), "ArrobAMO", "Private", Guid.NewGuid().ToString("N"))
                : "";
            settings = AmoSettings.Load(SettingsFile);
            enterpriseStore = new AmoEnterpriseStore(DataRoot);
            if (privateMode) Directory.CreateDirectory(privateRoot);
            securityManager = new AmoSecurityManager(privateMode ? privateRoot : DataRoot, this, ShowToast);
            startupUrlExplicit = !String.IsNullOrWhiteSpace(initialUrl);
            startupUrl = startupUrlExplicit ? CanonicalInternal(initialUrl) : CanonicalInternal(settings.HomeUrl);
            startupScript = initialScript;
            startupMinimized = initialMinimized;
            startupRecord = initialRecord;
            startupImport = initialImport;
            startupAI = initialAI;
            startupElite = initialElite;
            Text = privateMode ? "ArrobAMO ? Privado" : "ArrobAMO";
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
            if (!privateMode) LoadHistory();
            LoadBookmarks();
            UpdateAddressSuggestions();

            back.Click += delegate { var w = Active(); if (w != null && w.CanGoBack) w.GoBack(); };
            forward.Click += delegate { var w = Active(); if (w != null && w.CanGoForward) w.GoForward(); };
            reload.Click += delegate { var w = Active(); if (w != null) { if (pageLoading && w.CoreWebView2 != null) w.CoreWebView2.Stop(); else w.Reload(); } };
            home.Click += delegate { Navigate(CanonicalInternal(settings.HomeUrl)); };
            plus.Click += async delegate { await AddTab("arrobamo://inicio"); };
            aiButton.Click += delegate { aiMenu.Show(aiButton, new Point(0, aiButton.Height)); };
            historyButton.Click += delegate { ShowHistory(); };
            bookmarkButton.Click += delegate { ToggleBookmark(); };
            profileButton.Click += delegate { ShowProfile(); };
            menuButton.Click += delegate { mainMenu.Show(menuButton, new Point(0, menuButton.Height)); };
            uiKitButton.Click += delegate { ShowUIKit(); };
            downloadsButton.Click += delegate { downloadManager.Show(this); };
            settingsButton.Click += delegate { ShowSettings(); };
            securityButton.Click += delegate { ShowSecurity(); };
            teamButton.Click += delegate { ShowEliteCenter(); };
            sideToggle.Click += delegate { ToggleSidebar(); };
            scriptsButton.Click += delegate { ShowScriptsMenu(); };
            loopButton.Click += delegate { ShowLoopMenu(); };

            Shown += async delegate
            {
                cpuCounter.NextValue();
                await EnsureEnvironment();
                SetCueBanner(address, "Buscar o escribir una dirección");
                UpdateResponsiveChrome();
                bool restored = false;
                if (!privateMode && !startupUrlExplicit && settings.RestoreSession)
                    restored = await RestoreSessionTabs();
                if (!restored)
                    await AddTab(startupUrl);
                monitorTimer.Start();
                if (startupRecord) StartRecording();
                if (startupImport) BeginInvoke(new Action(ShowImportData));
                if (startupAI) BeginInvoke(new Action(async delegate { await ShowAI("ChatGPT", "https://chatgpt.com/"); }));
                if (startupElite) BeginInvoke(new Action(ShowEliteCenter));
                if (!String.IsNullOrWhiteSpace(startupScript) && File.Exists(startupScript)) await RunScript(startupScript, startupMinimized);
            };

            FormClosed += delegate
            {
                monitorTimer.Stop();
                cpuCounter.Dispose();
                if (!privateMode)
                {
                    SaveHistory();
                    SaveSession();
                }
                else
                {
                    SchedulePrivateCleanup();
                }
            };

            KeyDown += BrowserForm_KeyDown;
            tooltips.InitialDelay = 450;
            tooltips.ReshowDelay = 120;
            tooltips.AutoPopDelay = 5000;
            monitorTimer.Interval = 2500;
            monitorTimer.Tick += async delegate { await UpdateMetrics(); };
            Resize += delegate { UpdateResponsiveChrome(); };
            if (settings.ExpandSidebarAtStart) ToggleSidebar();
        }

        void SchedulePrivateCleanup()
        {
            if (!privateMode || String.IsNullOrWhiteSpace(privateRoot)) return;
            try
            {
                string escaped = privateRoot.Replace("'", "''");
                string command =
                    "Start-Sleep -Seconds 3; " +
                    "Remove-Item -LiteralPath '" + escaped + "' -Recurse -Force -ErrorAction SilentlyContinue";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -WindowStyle Hidden -Command \"" + command.Replace("\"", "\\\"") + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(psi);
            }
            catch { }
        }

        async Task EnsureEnvironment()
        {
            if (sharedEnvironment != null) return;
            string profile = Path.Combine(privateMode ? privateRoot : DataRoot, "WebView2");
            Directory.CreateDirectory(profile);
            sharedEnvironment = await CoreWebView2Environment.CreateAsync(null, profile, null);
        }

        void UpdateResponsiveChrome()
        {
            bool narrow = ClientSize.Width < 1050;
            cpuLabel.Visible = settings.ShowMetrics && ClientSize.Width >= 800;
            ramLabel.Visible = settings.ShowMetrics && ClientSize.Width >= 800;
            gpuLabel.Visible = settings.ShowMetrics && ClientSize.Width >= 800;
            cameraLabel.Visible = ClientSize.Width >= 800;
            microphoneLabel.Visible = ClientSize.Width >= 800;
            loopState.Visible = ClientSize.Width >= 1180;
            home.Visible = ClientSize.Width >= 900;
            securityLabel.Visible = ClientSize.Width >= 760;
            int addressLeft = securityLabel.Visible ? 234 : 184;
            address.Left = addressLeft;
            address.Width = Math.Max(160, ClientSize.Width - addressLeft - 206);
            statusLine.Left = address.Left;
            statusLine.Width = pageLoading ? address.Width : 0;
            int available = Math.Max(420, workspace.Panel1.Width - 20);
            int tabWidth = Math.Max(125, Math.Min(220, available / Math.Max(1, tabs.TabCount)));
            tabs.ItemSize = new Size(tabWidth, 34);
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

            voiceMark.Left = 12; voiceMark.Top = 8; voiceMark.Width = 44; voiceMark.Height = 22;
            voiceMark.Inverted = true; voiceMark.BackColor = Ink;
            voiceMark.Cursor = Cursors.Hand;
            brand.Controls.Add(voiceMark);
            tooltips.SetToolTip(voiceMark, "Clic: activar o pausar voz; clic derecho: opciones de AvatarAMO");
            voiceMark.MouseUp += VoiceMark_MouseUp;

            logo.Text = "ArrobAMO";
            logo.Left = 60;
            logo.Top = 9;
            logo.AutoSize = true;
            logo.Font = AmoTheme.UI(11f, FontStyle.Bold);
            logo.ForeColor = TextColor;
            brand.Controls.Add(logo);

            ConfigureMetric(cpuLabel, "CPU --", 210);
            ConfigureMetric(ramLabel, "RAM --", 295);
            ConfigureMetric(gpuLabel, "GPU --", 382);
            ConfigureMetric(cameraLabel, "CAM ?", 468);
            ConfigureMetric(microphoneLabel, "MIC ?", 548);
            tooltips.SetToolTip(cameraLabel, "Uso de cámara informado por el registro de privacidad de Windows; puede existir un retraso.");
            tooltips.SetToolTip(microphoneLabel, "Uso de micrófono informado por el registro de privacidad de Windows; puede existir un retraso.");

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

        async void VoiceMark_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var menu = new ContextMenuStrip();
                AmoTheme.StyleMenu(menu);
                menu.Items.Add("Activar / pausar escucha", null, async delegate { await RunAvatarVoice("document.getElementById('mic')?.click()"); });
                menu.Items.Add("Detener voz", null, async delegate { await RunAvatarVoice("document.getElementById('stop')?.click()"); });
                menu.Items.Add("Abrir AvatarAMO", null, delegate { Navigate("arrobamo://avataramo"); });
                menu.Closed += delegate { menu.Dispose(); };
                menu.Show(voiceMark, new Point(0, voiceMark.Height));
            }
            else if (e.Button == MouseButtons.Left)
                await RunAvatarVoice("document.getElementById('mic')?.click()");
        }

        async Task RunAvatarVoice(string script)
        {
            var web = tabs.TabPages.Cast<TabPage>().SelectMany(p => p.Controls.OfType<WebView2>())
                .FirstOrDefault(w => w.Source != null && w.Source.Host == "127.0.0.1" &&
                                     w.Source.Port == 18771 && w.Source.Scheme == "http");
            if (web == null || web.CoreWebView2 == null)
            {
                var previous = tabs.SelectedTab;
                await AddTab("arrobamo://avataramo");
                if (previous != null && tabs.TabPages.Contains(previous)) tabs.SelectedTab = previous;
                return;
            }
            try { await web.ExecuteScriptAsync(script); }
            catch { ShowToast("No se pudo cambiar el estado de voz de AvatarAMO.", AmoTheme.Danger); }
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
            address.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            address.AutoCompleteSource = AutoCompleteSource.CustomSource;
            nav.Controls.Add(address);

            navSpinner.Left = ClientSize.Width - 238;
            navSpinner.Top = 13;
            navSpinner.Width = 22;
            navSpinner.Height = 22;
            navSpinner.ForeColor = Color.White;
            navSpinner.BackColor = Ink2;
            navSpinner.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            navSpinner.Visible = false;
            nav.Controls.Add(navSpinner);

            plus.Text = "+";
            profileButton.Text = ((char)0x25CB).ToString();
            profileButton.Left = ClientSize.Width - 206;
            profileButton.Top = 9;
            profileButton.Width = 36;
            profileButton.Height = 32;
            profileButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            profileButton.FlatStyle = FlatStyle.Flat;
            profileButton.FlatAppearance.BorderSize = 0;
            profileButton.FlatAppearance.MouseOverBackColor = Ink3;
            profileButton.BackColor = Ink2;
            profileButton.ForeColor = TextColor;
            profileButton.Font = AmoTheme.UI(12f, FontStyle.Bold);
            nav.Controls.Add(profileButton);

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
            tooltips.SetToolTip(profileButton, "Perfil local");
            tooltips.SetToolTip(bookmarkButton, "Agregar o quitar marcador");
            tooltips.SetToolTip(menuButton, "Men\u00FA principal");
            tooltips.SetToolTip(plus, "Nueva pesta\u00F1a");

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

            AddSideButton("Inicio", ((char)0x2302).ToString(), 8, delegate { Navigate(settings.HomeUrl); });
            AddSideButton("Sitios", ((char)0x25CE).ToString(), 52, delegate { Navigate("amo://inicio"); });
            AddSideButton("Herramientas", ((char)0x25C7).ToString(), 96, delegate { ShowTools(); });
            AddSideButton("Scripts", ((char)0x25A3).ToString(), 140, delegate { ShowScriptsMenu(); }, scriptsButton);
            AddSideButton("Automatizaciones", ((char)0x21BB).ToString(), 184, delegate { ShowLoopMenu(); }, loopButton);
            AddSideButton("IA", ((char)0x2726).ToString(), 228, delegate { aiMenu.Show(side, new Point(side.Width, 228)); });
            AddSideButton("AvatarAMO", "A", 580, delegate { Navigate("arrobamo://avataramo"); });
            AddSideButton("Descargas", ((char)0x21E9).ToString(), 272, delegate { downloadManager.Show(this); }, downloadsButton);
            AddSideButton("Marcadores", ((char)0x2606).ToString(), 316, delegate { ShowBookmarks(); });
            AddSideButton("Equipo", ((char)0x25C8).ToString(), 360, delegate { ShowEliteCenter(); }, teamButton);
            AddSideButton("Seguridad", ((char)0x26E8).ToString(), 404, delegate { ShowSecurity(); }, securityButton);
            AddSideButton("Configuraci\u00F3n", ((char)0x2699).ToString(), 448, delegate { ShowSettings(); }, settingsButton);
            AddSideButton("UI Kit", "UI", 492, delegate { ShowUIKit(); }, uiKitButton);
            AddSideButton("Expandir sidebar", ((char)0x226B).ToString(), 536, delegate { ToggleSidebar(); }, sideToggle);
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
            tabs.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                closingTab = false;
                if (e.Button == MouseButtons.Right)
                {
                    for (int i=0;i<tabs.TabPages.Count;i++)
                    {
                        if (tabs.GetTabRect(i).Contains(e.Location))
                        {
                            tabs.SelectedIndex = i;
                            ShowTabMenu(e.Location);
                            break;
                        }
                    }
                }
            };
            tabs.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                int next = -1;
                for (int i=0;i<tabs.TabPages.Count;i++) if (tabs.GetTabRect(i).Contains(e.Location)) { next=i; break; }
                if (next != hoverTabIndex) { hoverTabIndex = next; tabs.Invalidate(); }
            };
            tabs.MouseLeave += delegate { hoverTabIndex = -1; tabs.Invalidate(); };
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

            aiState.Text = "Lista";
            aiState.Left = 116;
            aiState.Top = 12;
            aiState.Width = 88;
            aiState.Height = 20;
            aiState.ForeColor = AmoTheme.Success;
            aiState.Font = AmoTheme.UI(8.5f, FontStyle.Bold);
            aiHeader.Controls.Add(aiState);

            aiDots.Left = 172;
            aiDots.Top = 11;
            aiDots.Width = 42;
            aiDots.Height = 18;
            aiDots.ForeColor = TextColor;
            aiDots.BackColor = Ink2;
            aiDots.Visible = false;
            aiHeader.Controls.Add(aiDots);

            aiOpenTab.Text = "Abrir en pesta\u00F1a";
            aiOpenTab.Width = 112;
            aiOpenTab.Height = 28;
            aiOpenTab.Top = 6;
            aiOpenTab.Left = 198;
            aiOpenTab.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            aiOpenTab.FlatStyle = FlatStyle.Flat;
            aiOpenTab.FlatAppearance.BorderColor = Soft;
            aiOpenTab.BackColor = Ink2;
            aiOpenTab.ForeColor = TextColor;
            aiOpenTab.Click += async delegate { if (aiWeb.Source != null) await AddTab(aiWeb.Source.ToString()); };
            aiHeader.Controls.Add(aiOpenTab);

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
            AmoTheme.StyleMenu(aiMenu);
            AddAIOption("AvatarAMO local", "http://127.0.0.1:18771/");
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

            AmoTheme.StyleMenu(scriptsMenu);
            AddScriptMenuItem("Redactar script", delegate { EditScript(null); });
            AddScriptMenuItem("Importar script", delegate { ImportScript(); });
            AddScriptMenuItem("Exportar último script", delegate { ExportLastScript(); });
            AddScriptMenuItem("Mis scripts", delegate { ShowScriptManager(); });

            AmoTheme.StyleMenu(loopMenu);
            AddLoopMenuItem("Activar Loop / grabar", delegate { StartRecording(); });
            AddLoopMenuItem("Detener y guardar Loop", delegate { StopRecordingAndSave(); });
            AddLoopMenuItem("Ejecutar último Loop", async delegate { await RunLastScript(false); });
            AddLoopMenuItem("Ejecutar último minimizado", async delegate { await RunLastScript(true); });
            AddLoopMenuItem("Repetir último Loop...", async delegate { await RunLoopPrompt(); });
            AddLoopMenuItem("Detener ejecución", delegate { stopRequested = true; });

            AmoTheme.StyleMenu(mainMenu);
            AddMainMenuItem("Nueva pestaña", async delegate { await AddTab("https://desarrollamo.com.ar/"); });
            AddMainMenuItem("Abrir AvatarAMO", delegate { Navigate("arrobamo://avataramo"); });
            AddMainMenuItem("Historial", delegate { ShowHistory(); });
            AddMainMenuItem("Marcadores", delegate { ShowBookmarks(); });
            AddMainMenuItem("Descargas", delegate { downloadManager.Show(this); });
            AddMainMenuItem("Importar datos", delegate { ShowImportData(); });
            AddMainMenuItem("Scripts", delegate { ShowScriptsMenu(); });
            AddMainMenuItem("Automatizaciones", delegate { ShowLoopMenu(); });
            AddMainMenuItem("Perfil local", delegate { ShowProfile(); });
            AddMainMenuItem("Equipo", delegate { ShowEliteCenter(); });
            AddMainMenuItem("Seguridad", delegate { ShowSecurity(); });
            AddMainMenuItem("Configuraci\u00F3n", delegate { ShowSettings(); });
            AddMainMenuItem("UI Kit", delegate { ShowUIKit(); });
            AmoTheme.StyleMenu(bookmarksMenu);
            AmoTheme.StyleMenu(tabMenu);
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
            string tip = b == back ? "Atr\u00E1s" : b == forward ? "Adelante" : b == reload ? "Recargar / detener" : b == home ? "Inicio" : "";
            if (tip.Length > 0) tooltips.SetToolTip(b, tip);
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
            tooltips.SetToolTip(b, tooltip);
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




        void ShowImportData()
        {
            using (var f = new AmoImportForm())
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                int before = bookmarks.Count;
                foreach (string row in f.Imported)
                {
                    if (!bookmarks.Any(x => String.Equals(x, row, StringComparison.OrdinalIgnoreCase)))
                        bookmarks.Add(row);
                }
                if (bookmarks.Count > 2000) bookmarks.RemoveRange(2000, bookmarks.Count - 2000);
                SaveBookmarks();
                UpdateAddressSuggestions();
                int added = bookmarks.Count - before;
                ShowToast(added > 0 ? added + " favoritos importados." : "No hab\u00EDa favoritos nuevos.", added > 0 ? AmoTheme.Success : AmoTheme.Info);
            }
        }

        void ShowProfile()
        {
            var f = new Form
            {
                Text = "Perfil local \u00B7 ArrobAMO",
                Width = 560, Height = 410,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = AmoTheme.Bg, ForeColor = AmoTheme.Text,
                Font = AmoTheme.UI(9f)
            };

            var mark = new OrbitMark { Left = 28, Top = 26, Width = 72, Height = 72, Inverted = false, BackColor = AmoTheme.Bg };
            f.Controls.Add(mark);
            f.Controls.Add(new Label { Text = "Perfil local", Left = 120, Top = 28, Width = 300, Height = 34, Font = AmoTheme.UI(18f, FontStyle.Bold), ForeColor = AmoTheme.Text });
            f.Controls.Add(new Label { Text = "Datos y sesi\u00F3n almacenados \u00FAnicamente en esta instalaci\u00F3n.", Left = 121, Top = 64, Width = 380, Height = 38, ForeColor = AmoTheme.TextSoft });

            var card = new AmoCard { Left = 28, Top = 124, Width = 490, Height = 160 };
            card.Controls.Add(new Label { Text = "Producto", Left = 18, Top = 18, Width = 120, Height = 20, ForeColor = AmoTheme.TextSoft });
            card.Controls.Add(new Label { Text = "ArrobAMO", Left = 160, Top = 18, Width = 280, Height = 20, Font = AmoTheme.UI(9f, FontStyle.Bold), ForeColor = AmoTheme.Text });
            card.Controls.Add(new Label { Text = "Versi\u00F3n", Left = 18, Top = 54, Width = 120, Height = 20, ForeColor = AmoTheme.TextSoft });
            card.Controls.Add(new Label { Text = Application.ProductVersion, Left = 160, Top = 54, Width = 280, Height = 20, ForeColor = AmoTheme.Text });
            card.Controls.Add(new Label { Text = "Perfil Web", Left = 18, Top = 90, Width = 120, Height = 20, ForeColor = AmoTheme.TextSoft });
            card.Controls.Add(new Label { Text = Path.Combine(DataRoot, "WebView2"), Left = 160, Top = 90, Width = 300, Height = 40, ForeColor = AmoTheme.Text });
            f.Controls.Add(card);

            var open = new AmoButton { Text = "Abrir carpeta del perfil", Variant = AmoButtonVariant.Secondary, Left = 28, Top = 306, Width = 190 };
            open.Click += delegate { try { Process.Start("explorer.exe", DataRoot); } catch { } };
            var close = new AmoButton { Text = "Cerrar", Variant = AmoButtonVariant.Primary, Left = 418, Top = 306, Width = 100 };
            close.Click += delegate { f.Close(); };
            f.Controls.Add(open); f.Controls.Add(close);
            f.Show(this);
        }

        void ShowEliteCenter()
        {
            var f = new AmoEliteCenterForm(enterpriseStore, ShowToast);
            f.Show(this);
        }

        void ShowSecurity()
        {
            var f = new Form
            {
                Text = "Seguridad ? ArrobAMO",
                Width = 820,
                Height = 560,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = AmoTheme.Bg,
                ForeColor = AmoTheme.Text,
                Font = AmoTheme.UI(9f)
            };

            f.Controls.Add(new Label
            {
                Text = "Seguridad",
                Left = 24, Top = 18, Width = 380, Height = 34,
                Font = AmoTheme.UI(18f, FontStyle.Bold),
                ForeColor = AmoTheme.Text
            });
            f.Controls.Add(new Label
            {
                Text = "Permisos recordados por sitio. La comprobaci?n de reputaci?n del motor WebView2 est? activada.",
                Left = 25, Top = 55, Width = 720, Height = 36,
                ForeColor = AmoTheme.TextSoft
            });

            var info = new AmoCard { Left = 24, Top = 100, Width = 754, Height = 82 };
            info.Controls.Add(new Label
            {
                Text = "Protecci?n b?sica activa",
                Left = 16, Top = 14, Width = 230, Height = 22,
                Font = AmoTheme.UI(10f, FontStyle.Bold), ForeColor = AmoTheme.Success
            });
            info.Controls.Add(new Label
            {
                Text = "Popups no iniciados por el usuario se bloquean. C?mara, micr?fono, ubicaci?n y otros permisos requieren decisi?n.",
                Left = 16, Top = 42, Width = 690, Height = 28,
                ForeColor = AmoTheme.TextSoft
            });
            f.Controls.Add(info);

            var list = new ListView
            {
                Left = 24, Top = 198, Width = 754, Height = 268,
                View = View.Details, FullRowSelect = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = AmoTheme.Surface, ForeColor = AmoTheme.Text
            };
            list.Columns.Add("Sitio", 300);
            list.Columns.Add("Permiso", 220);
            list.Columns.Add("Decisi?n", 200);

            Action refresh = delegate
            {
                list.Items.Clear();
                foreach (var kv in securityManager.Snapshot())
                {
                    int i = kv.Key.LastIndexOf('|');
                    string host = i > 0 ? kv.Key.Substring(0, i) : kv.Key;
                    string kind = i > 0 ? kv.Key.Substring(i + 1) : "";
                    var item = new ListViewItem(host);
                    item.SubItems.Add(kind);
                    item.SubItems.Add(kv.Value == CoreWebView2PermissionState.Allow ? "Permitido" : "Bloqueado");
                    item.Tag = kv.Key;
                    list.Items.Add(item);
                }
            };
            refresh();
            f.Controls.Add(list);

            var remove = new AmoButton { Text = "Olvidar regla", Variant = AmoButtonVariant.Secondary, Left = 24, Top = 484, Width = 120, Height = 38 };
            var clear = new AmoButton { Text = "Borrar todas", Variant = AmoButtonVariant.Danger, Left = 154, Top = 484, Width = 120, Height = 38 };
            var close = new AmoButton { Text = "Cerrar", Variant = AmoButtonVariant.Primary, Left = 678, Top = 484, Width = 100, Height = 38 };

            remove.Click += delegate
            {
                if (list.SelectedItems.Count == 0) return;
                securityManager.Remove(Convert.ToString(list.SelectedItems[0].Tag));
                refresh();
            };
            clear.Click += delegate
            {
                using (var modal = new AmoModalForm("Borrar permisos", "ArrobAMO volver? a preguntar cuando un sitio solicite permisos.", "Borrar", true))
                {
                    if (modal.ShowDialog(f) != DialogResult.OK) return;
                }
                securityManager.Clear();
                refresh();
            };
            close.Click += delegate { f.Close(); };

            f.Controls.Add(remove);
            f.Controls.Add(clear);
            f.Controls.Add(close);
            f.Show(this);
        }

        void ShowSettings()
        {
            using (var f = new AmoSettingsForm(settings))
            {
                if (f.ShowDialog(this) != DialogResult.OK || !f.Saved) return;
                settings.HomeUrl = f.Value.HomeUrl;
                settings.ShowMetrics = f.Value.ShowMetrics;
                settings.ExpandSidebarAtStart = f.Value.ExpandSidebarAtStart;
                settings.RestoreSession = f.Value.RestoreSession;
                settings.Save(SettingsFile);
                UpdateResponsiveChrome();
                if (settings.ExpandSidebarAtStart != sidebarExpanded) ToggleSidebar();
                ShowToast("Configuraci\u00F3n guardada.", AmoTheme.Success);
            }
        }

        void ShowTools()
        {
            var f = new Form
            {
                Text = "Herramientas \u00B7 ArrobAMO",
                Width = 660, Height = 430,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = AmoTheme.Bg, ForeColor = AmoTheme.Text,
                Font = AmoTheme.UI(9f)
            };
            f.Controls.Add(new Label { Text = "Herramientas", Left = 24, Top = 18, Width = 400, Height = 34, Font = AmoTheme.UI(18f, FontStyle.Bold), ForeColor = AmoTheme.Text });
            f.Controls.Add(new Label { Text = "Acciones reales del navegador y del entorno local.", Left = 25, Top = 55, Width = 500, Height = 22, ForeColor = AmoTheme.TextSoft });

            var dev = new AmoButton { Text = "Abrir DevTools", Variant = AmoButtonVariant.Primary, Left = 24, Top = 105, Width = 180 };
            var copy = new AmoButton { Text = "Copiar URL", Variant = AmoButtonVariant.Secondary, Left = 220, Top = 105, Width = 180 };
            var data = new AmoButton { Text = "Carpeta de datos", Variant = AmoButtonVariant.Outline, Left = 416, Top = 105, Width = 180 };
            var kit = new AmoButton { Text = "Abrir UI Kit", Variant = AmoButtonVariant.Secondary, Left = 24, Top = 160, Width = 180 };
            var downloads = new AmoButton { Text = "Descargas", Variant = AmoButtonVariant.Secondary, Left = 220, Top = 160, Width = 180 };
            var scripts = new AmoButton { Text = "Mis scripts", Variant = AmoButtonVariant.Secondary, Left = 416, Top = 160, Width = 180 };

            dev.Click += delegate { var w = Active(); if (w != null && w.CoreWebView2 != null) w.CoreWebView2.OpenDevToolsWindow(); };
            copy.Click += delegate
            {
                var w = Active();
                if (w != null && w.Source != null)
                {
                    Clipboard.SetText(w.Source.ToString());
                    ShowToast("URL copiada.", AmoTheme.Info);
                }
            };
            data.Click += delegate { try { Process.Start("explorer.exe", DataRoot); } catch { } };
            kit.Click += delegate { ShowUIKit(); };
            downloads.Click += delegate { downloadManager.Show(this); };
            scripts.Click += delegate { ShowScriptManager(); };

            f.Controls.Add(dev); f.Controls.Add(copy); f.Controls.Add(data);
            f.Controls.Add(kit); f.Controls.Add(downloads); f.Controls.Add(scripts);
            f.Controls.Add(new Label { Text = "ArrobAMO no simula capacidades: estas acciones operan sobre la pesta\u00F1a y los datos reales.", Left = 24, Top = 240, Width = 570, Height = 48, ForeColor = AmoTheme.TextSoft });
            f.Show(this);
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

            var misc = section("Controles y etiquetas");
            var sw = new AmoSwitch { Left = 16, Top = 58, Checked = true };
            var swLabel = new Label { Text = "Switch activado", Left = 72, Top = 61, Width = 160, ForeColor = AmoTheme.Text };
            var chip1 = new AmoChip { Text = "Automatizaci\u00F3n \u00D7", Left = 16, Top = 105, Width = 130 };
            var chip2 = new AmoChip { Text = "IA \u00D7", Left = 156, Top = 105, Width = 76 };
            var chip3 = new AmoChip { Text = "Productividad \u00D7", Left = 242, Top = 105, Width = 128 };
            misc.Controls.Add(sw); misc.Controls.Add(swLabel); misc.Controls.Add(chip1); misc.Controls.Add(chip2); misc.Controls.Add(chip3);

            var skeletons = section("Skeletons y espera");
            var sk1 = new AmoSkeleton { Left = 16, Top = 58, Width = 250, Height = 16 };
            var sk2 = new AmoSkeleton { Left = 16, Top = 86, Width = 340, Height = 16 };
            var sk3 = new AmoSkeleton { Left = 16, Top = 114, Width = 190, Height = 16 };
            var dots = new AmoLoadingDots { Left = 300, Top = 55, ForeColor = AmoTheme.Text, BackColor = AmoTheme.Surface };
            skeletons.Controls.Add(sk1); skeletons.Controls.Add(sk2); skeletons.Controls.Add(sk3); skeletons.Controls.Add(dots);

            var feedback = section("Modales y notificaciones");
            var modalBtn = new AmoButton { Text = "Probar modal", Variant = AmoButtonVariant.Primary, Left = 16, Top = 58, Width = 130 };
            var toastBtn = new AmoButton { Text = "Probar toast", Variant = AmoButtonVariant.Secondary, Left = 158, Top = 58, Width = 130 };
            modalBtn.Click += delegate
            {
                using (var modal = new AmoModalForm("Confirmar acci\u00F3n", "Este es el modal consistente de ArrobAMO. No ejecutar\u00E1 ninguna acci\u00F3n destructiva.", "Confirmar", false))
                    modal.ShowDialog(f);
            };
            toastBtn.Click += delegate { ShowToast("Cambios guardados correctamente.", AmoTheme.Success); };
            feedback.Controls.Add(modalBtn); feedback.Controls.Add(toastBtn);

            var brandSection = section("Identidad ArrobAMO");
            var orbitMark = new OrbitMark { Left = 20, Top = 52, Width = 100, Height = 100, Inverted = false, BackColor = AmoTheme.Surface };
            var brandTitle = new Label { Text = "ArrobAMO", Left = 142, Top = 64, Width = 220, Height = 34, Font = AmoTheme.UI(20f, FontStyle.Bold), ForeColor = AmoTheme.Text };
            var brandSub = new Label { Text = "by DesarrollAMO\nTodo orbita aqu\u00ED.", Left = 144, Top = 102, Width = 220, Height = 52, ForeColor = AmoTheme.TextSoft };
            brandSection.Controls.Add(orbitMark); brandSection.Controls.Add(brandTitle); brandSection.Controls.Add(brandSub);

            flow.Controls.Add(buttons); flow.Controls.Add(status); flow.Controls.Add(loaders); flow.Controls.Add(forms);
            flow.Controls.Add(misc); flow.Controls.Add(skeletons); flow.Controls.Add(feedback); flow.Controls.Add(brandSection);
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
            var page = new TabPage("Nueva pesta\u00F1a");
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
            AttachAccelerators(web);
            downloadManager.Attach(web.CoreWebView2);
            securityManager.Attach(web.CoreWebView2);
            AttachFavicon(web, page);
            await InstallRecorder(web);

            web.CoreWebView2.WebMessageReceived += delegate(object sender, CoreWebView2WebMessageReceivedEventArgs e)
            {
                string msg = "";
                try { msg = e.TryGetWebMessageAsString(); } catch { }
                if (String.IsNullOrWhiteSpace(msg)) return;

                if (recording && !replaying)
                {
                    var parts = msg.Split(new[] { '	' }, 3);
                    if (parts.Length >= 2 && parts[0] == "CLICK") RecordStep("CLICK", parts[1], "");
                    else if (parts.Length >= 2 && parts[0] == "INPUT") RecordStep("INPUT", parts[1], parts.Length > 2 ? parts[2] : "");
                }

                if (msg.StartsWith("AVATAR|", StringComparison.Ordinal))
                {
                    Uri origin;
                    if (!Uri.TryCreate(e.Source, UriKind.Absolute, out origin) ||
                        origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port != 18771) return;
                    if (msg.StartsWith("AVATAR|STATE|", StringComparison.Ordinal))
                    {
                        voiceMark.VoiceState = msg.Substring(13);
                    }
                    else if (msg == "AVATAR|INSTAGRAM") BeginInvoke(new Action(async delegate { await AddTab("https://www.instagram.com/"); }));
                    else if (msg == "AVATAR|HISTORY") BeginInvoke(new Action(ShowHistory));
                    else if (msg.StartsWith("AVATAR|URL|", StringComparison.Ordinal))
                    {
                        Uri dest;
                        if (Uri.TryCreate(msg.Substring(11), UriKind.Absolute, out dest) &&
                            (dest.Scheme == "https" || dest.Scheme == "http") && dest.UserInfo.Length == 0)
                            BeginInvoke(new Action(async delegate { await AddTab(dest.AbsoluteUri); }));
                    }
                    return;
                }
                if (msg.StartsWith("AMO|", StringComparison.Ordinal))
                    HandleInternalMessage(msg.Substring(4));
            };

            web.CoreWebView2.NavigationStarting += delegate(object sender, CoreWebView2NavigationStartingEventArgs e)
            {
                if (!e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && !e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
                    page.Tag = e.Uri;
                if (tabs.SelectedTab == page)
                {
                    string tagged = CanonicalInternal(Convert.ToString(page.Tag));
                    bool internalPage = tagged.StartsWith("arrobamo://", StringComparison.OrdinalIgnoreCase);
                    bool engineInternal = e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || e.Uri.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase);
                    address.Text = internalPage && engineInternal ? tagged : DisplayAddress(e.Uri);
                }
                loadingTabs.Add(page);
                tabs.Invalidate();
                SetLoading(true);
                if (recording && !replaying && !e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    RecordStep("NAV", e.Uri, "");
            };

            web.CoreWebView2.SourceChanged += delegate
            {
                if (tabs.SelectedTab == page && web.Source != null)
                {
                    string tagged = CanonicalInternal(Convert.ToString(page.Tag));
                    string src = web.Source.ToString();
                    bool internalPage = tagged.StartsWith("arrobamo://", StringComparison.OrdinalIgnoreCase);
                    bool engineInternal = src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || src.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase);
                    address.Text = internalPage && engineInternal ? tagged : DisplayAddress(src);
                }
            };

            web.CoreWebView2.DocumentTitleChanged += delegate
            {
                string title = web.CoreWebView2.DocumentTitle;
                page.Text = ShortTitle(title);
                if (tabs.SelectedTab == page) Text = title + " - ArrobAMO";
                tabs.Invalidate();
            };

            web.CoreWebView2.NewWindowRequested += async delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e)
            {
                if (securityManager.ShouldBlockPopup(e.IsUserInitiated, e.Uri))
                {
                    e.Handled = true;
                    return;
                }

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
                loadingTabs.Remove(page);
                tabs.Invalidate();
                SetLoading(false);
                if (tabs.SelectedTab == page) SyncActive();
                if (web.Source != null && !web.Source.ToString().StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    AddHistory(web.Source.ToString(), web.CoreWebView2.DocumentTitle);
            };

            if (IsInternal(url, "inicio"))
            {
                page.Tag = "arrobamo://inicio";
                page.Text = "Inicio";
                if (tabs.SelectedTab == page) address.Text = "arrobamo://inicio";
                web.NavigateToString(GetStartPageHtml());
            }
            else
            {
                page.Tag = Normalize(url);
                web.Source = new Uri(Convert.ToString(page.Tag));
            }
        }

        void HandleInternalMessage(string command)
        {
            if (String.IsNullOrWhiteSpace(command)) return;
            if (command.StartsWith("NAV|", StringComparison.Ordinal)) Navigate(command.Substring(4));
            else if (command == "IA") aiMenu.Show(aiButton, new Point(0, aiButton.Height));
            else if (command == "LOOP") ShowLoopMenu();
            else if (command == "SCRIPTS") ShowScriptsMenu();
            else if (command == "UIKIT") ShowUIKit();
        }

        string GetStartPageHtml()
        {
            return @"<!doctype html><html lang='es'><head><meta charset='utf-8'><title>Inicio</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#f7f7f5;color:#111;font-family:Segoe UI,Arial,sans-serif}
.wrap{max-width:1080px;margin:0 auto;padding:54px 42px 48px}.hero{display:flex;align-items:center;gap:34px;margin-bottom:44px}
.mark{width:126px;height:126px;border-radius:30px;background:#141414;position:relative;box-shadow:0 16px 44px rgba(0,0,0,.14)}
.core{position:absolute;width:32px;height:32px;border:8px solid #fff;border-radius:50%;left:47px;top:47px}
.orbit{position:absolute;border:7px solid #fff;border-radius:50%;left:23px;top:23px;width:80px;height:80px;border-right-color:transparent;transform:rotate(-25deg)}
.dot{position:absolute;width:15px;height:15px;background:#fff;border-radius:50%}.d1{left:92px;top:28px}.d2{left:16px;top:63px}.d3{left:72px;top:99px}
h1{font-size:46px;letter-spacing:-1.8px;margin:0 0 6px}h1 b{font-weight:800}.sub{font-size:16px;color:#5f5f5f}.tag{margin-top:12px;font-size:12px;letter-spacing:4px;color:#8c8c8c;text-transform:uppercase}
.search{display:flex;align-items:center;background:#fff;border:1px solid #ddddda;border-radius:14px;padding:7px 9px 7px 18px;box-shadow:0 2px 8px rgba(0,0,0,.06);margin-bottom:28px}
.search input{border:0;outline:0;flex:1;font-size:16px;padding:12px;background:transparent}.search button{border:0;background:#111;color:#fff;border-radius:10px;padding:12px 22px;font-weight:700;cursor:pointer}
.grid{display:grid;grid-template-columns:repeat(4,1fr);gap:14px}.card{background:#fff;border:1px solid #e2e2df;border-radius:16px;padding:20px;min-height:126px;cursor:pointer;transition:.15s}
.card:hover{transform:translateY(-2px);border-color:#bcbcb7;box-shadow:0 8px 24px rgba(0,0,0,.08)}.ico{font-size:25px;margin-bottom:13px}.card strong{display:block;font-size:14px;margin-bottom:5px}.card span{font-size:12px;color:#777}
.foot{display:flex;justify-content:space-between;margin-top:44px;color:#8c8c8c;font-size:11px;letter-spacing:2px}.live{color:#18a558;font-weight:700}
@media(max-width:760px){.grid{grid-template-columns:repeat(2,1fr)}.hero{align-items:flex-start}.mark{width:96px;height:96px;min-width:96px}.wrap{padding:32px 22px}h1{font-size:34px}}
</style></head><body><div class='wrap'><div class='hero'><div class='mark'><i class='orbit'></i><i class='core'></i><i class='dot d1'></i><i class='dot d2'></i><i class='dot d3'></i></div><div><h1>Arrob<b>AMO</b></h1><div class='sub'>by DesarrollAMO &middot; Tu centro operativo en la web</div><div class='tag'>Navega &middot; automatiza &middot; conecta &middot; crea</div></div></div>
<form class='search' onsubmit=""event.preventDefault();chrome.webview.postMessage('AMO|NAV|'+document.getElementById('q').value)""><input id='q' autofocus placeholder='Buscar o escribir una direcci&oacute;n'><button>Ir</button></form>
<div class='grid'>
<div class='card' onclick=""chrome.webview.postMessage('AMO|NAV|https://desarrollamo.com.ar/')""><div class='ico'>&#9678;</div><strong>Sitios web</strong><span>Todo en un mismo lugar.</span></div>
<div class='card' onclick=""chrome.webview.postMessage('AMO|SCRIPTS')""><div class='ico'>&#9635;</div><strong>Scripts</strong><span>Automatiz&aacute; tareas repetitivas.</span></div>
<div class='card' onclick=""chrome.webview.postMessage('AMO|LOOP')""><div class='ico'>&#8635;</div><strong>Automatizaciones</strong><span>Que trabajen por vos.</span></div>
<div class='card' onclick=""chrome.webview.postMessage('AMO|IA')""><div class='ico'>&#10022;</div><strong>Inteligencia artificial</strong><span>Eleg&iacute; con qui&eacute;n trabajar.</span></div>
</div><div class='foot'><span>TODO ORBITA AQU&Iacute;.</span><span class='live'>&#9679; ArrobAMO listo</span></div></div></body></html>";
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
            AttachAccelerators(web);
            downloadManager.Attach(web.CoreWebView2);
            securityManager.Attach(web.CoreWebView2);
            AttachFavicon(web, page);
            HookPopupWebView(web, page);
            return web;
        }

        void HookPopupWebView(WebView2 web, TabPage page)
        {
            web.CoreWebView2.NewWindowRequested += async delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e)
            {
                if (securityManager.ShouldBlockPopup(e.IsUserInitiated, e.Uri))
                {
                    e.Handled = true;
                    return;
                }
                var deferral = e.GetDeferral();
                try
                {
                    var popupWeb = await AddPopupTab();
                    e.NewWindow = popupWeb.CoreWebView2;
                }
                finally { deferral.Complete(); }
            };

            web.CoreWebView2.SourceChanged += delegate { if (tabs.SelectedTab == page) address.Text = web.Source == null ? "" : web.Source.ToString(); };
            web.CoreWebView2.DocumentTitleChanged += delegate { page.Text = ShortTitle(web.CoreWebView2.DocumentTitle); tabs.Invalidate(); };
            web.CoreWebView2.NavigationCompleted += delegate { if (tabs.SelectedTab == page) SyncActive(); AddHistory(web.Source == null ? "" : web.Source.ToString(), web.CoreWebView2.DocumentTitle); };
            web.CoreWebView2.WindowCloseRequested += delegate { BeginInvoke(new Action(delegate { var p = tabs.TabPages.Cast<TabPage>().FirstOrDefault(x => x.Controls.Contains(web)); if (p != null) CloseTab(tabs.TabPages.IndexOf(p)); })); };
        }

        void AttachFavicon(WebView2 web, TabPage page)
        {
            web.CoreWebView2.FaviconChanged += async delegate
            {
                try
                {
                    using (Stream stream = await web.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png))
                    {
                        if (stream == null) return;
                        using (var temp = Image.FromStream(stream))
                        {
                            Image clone = new Bitmap(temp);
                            Image old;
                            if (tabFavicons.TryGetValue(page, out old) && old != null) old.Dispose();
                            tabFavicons[page] = clone;
                        }
                    }
                    tabs.Invalidate();
                }
                catch { }
            };
        }

        void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabs.TabPages.Count) return;
            var page = tabs.TabPages[e.Index];
            var r = tabs.GetTabRect(e.Index);
            bool selected = tabs.SelectedIndex == e.Index;
            bool hovered = hoverTabIndex == e.Index;

            using (var bg = new SolidBrush(selected ? Ink3 : hovered ? Color.FromArgb(33,33,36) : Ink2))
                e.Graphics.FillRectangle(bg, r);

            var iconRect = new Rectangle(r.X + 10, r.Y + 9, 16, 16);
            Image favicon;
            if (tabFavicons.TryGetValue(page, out favicon) && favicon != null)
                e.Graphics.DrawImage(favicon, iconRect);
            else
            {
                using (var p = new Pen(selected ? Color.White : Muted, 1.5f))
                    e.Graphics.DrawEllipse(p, iconRect);
                if (loadingTabs.Contains(page))
                    using (var b = new SolidBrush(AmoTheme.Info)) e.Graphics.FillEllipse(b, iconRect.Right-5, iconRect.Top, 5, 5);
            }

            var textRect = new Rectangle(r.X + 32, r.Y + 7, r.Width - 62, r.Height - 12);
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
            if (pinnedTabs.Contains(page))
            {
                ShowToast("Esta pesta\u00F1a est\u00E1 fijada. Desfijala para cerrarla.", AmoTheme.Info);
                return;
            }
            string closingUrl = CurrentTabUrl(page);
            if (!String.IsNullOrWhiteSpace(closingUrl))
            {
                closedTabs.Push(closingUrl);
                while (closedTabs.Count > 30)
                {
                    var keep = closedTabs.Reverse().Take(30).Reverse().ToArray();
                    closedTabs.Clear();
                    foreach (var x in keep) closedTabs.Push(x);
                }
            }
            var web = page.Controls.OfType<WebView2>().FirstOrDefault();
            if (web != null) web.Dispose();
            Image fav;
            if (tabFavicons.TryGetValue(page, out fav)) { if (fav != null) fav.Dispose(); tabFavicons.Remove(page); }
            loadingTabs.Remove(page);
            tabs.TabPages.Remove(page);
            page.Dispose();

            if (tabs.TabPages.Count == 0)
                BeginInvoke(new Action(async delegate { await AddTab("amo://inicio"); }));
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

        void AttachAccelerators(WebView2 web)
        {
            if (web == null) return;
            web.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                bool ctrl = e.Control;
                bool shift = e.Shift;

                if (ctrl && shift && e.KeyCode == Keys.T)
                {
                    e.SuppressKeyPress = true;
                    BeginInvoke(new Action(ReopenClosedTab));
                }
                else if (ctrl && e.KeyCode == Keys.T)
                {
                    e.SuppressKeyPress = true;
                    BeginInvoke(new Action(async delegate { await AddTab("amo://inicio"); }));
                }
                else if (ctrl && e.KeyCode == Keys.W)
                {
                    e.SuppressKeyPress = true;
                    BeginInvoke(new Action(delegate { if (tabs.SelectedIndex >= 0) CloseTab(tabs.SelectedIndex); }));
                }
                else if (ctrl && e.KeyCode == Keys.L)
                {
                    e.SuppressKeyPress = true;
                    BeginInvoke(new Action(delegate { address.Focus(); address.SelectAll(); }));
                }
                else if (ctrl && e.KeyCode == Keys.R)
                {
                    e.SuppressKeyPress = true;
                    BeginInvoke(new Action(delegate { var active = Active(); if (active != null) active.Reload(); }));
                }
                else if (ctrl && e.KeyCode == Keys.K)
                {
                    e.SuppressKeyPress = true;
                    BeginInvoke(new Action(DuplicateActiveTab));
                }
                else if (e.KeyCode == Keys.F5)
                {
                    e.SuppressKeyPress = true;
                    BeginInvoke(new Action(delegate { var active = Active(); if (active != null) active.Reload(); }));
                }
            };
        }

        void EnableInspectMenu(WebView2 web)
        {
            web.CoreWebView2.ContextMenuRequested += delegate(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
            {
                var avatar = web.CoreWebView2.Environment.CreateContextMenuItem(
                    "Abrir AvatarAMO", null, CoreWebView2ContextMenuItemKind.Command);
                avatar.CustomItemSelected += delegate
                {
                    BeginInvoke(new Action(delegate { Navigate("arrobamo://avataramo"); }));
                };
                e.MenuItems.Insert(0, avatar);
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
                    await EnsureEnvironment();
                await aiWeb.EnsureCoreWebView2Async(sharedEnvironment);
                aiWeb.CoreWebView2.Settings.AreDevToolsEnabled = true;
                aiWeb.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                aiWeb.CoreWebView2.Settings.IsStatusBarEnabled = false;
                downloadManager.Attach(aiWeb.CoreWebView2);
                securityManager.Attach(aiWeb.CoreWebView2);
                aiWeb.CoreWebView2.NavigationStarting += delegate
                {
                    aiState.Text = "Cargando";
                    aiState.ForeColor = AmoTheme.Info;
                    aiDots.Visible = true;
                };
                aiWeb.CoreWebView2.NavigationCompleted += delegate(object sender, CoreWebView2NavigationCompletedEventArgs e)
                {
                    aiDots.Visible = false;
                    aiState.Text = e.IsSuccess ? "Lista" : "Error";
                    aiState.ForeColor = e.IsSuccess ? AmoTheme.Success : AmoTheme.Danger;
                };
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
                UpdateAddressSuggestions();
                ShowToast("Marcador eliminado.", AmoTheme.Info);
            }
            else
            {
                string title = w.CoreWebView2 == null ? url : w.CoreWebView2.DocumentTitle;
                bookmarks.Insert(0, (String.IsNullOrWhiteSpace(title) ? url : title.Replace("|"," ")) + "|" + url);
                if (bookmarks.Count > 200) bookmarks.RemoveRange(200, bookmarks.Count - 200);
                bookmarkButton.Text = ((char)0x2605).ToString();
                SaveBookmarks();
                UpdateAddressSuggestions();
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
            loopState.Text = "Ejecutando";
            loopState.ForeColor = AmoTheme.Info;
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
                loopState.Text = "Loop detenido";
                loopState.ForeColor = Muted;
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
        void ShowHistoryWindow()
        {
            var f = new Form
            {
                Text = "Historial ? ArrobAMO",
                Width = 820,
                Height = 600,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = AmoTheme.Bg,
                ForeColor = AmoTheme.Text,
                Font = AmoTheme.UI(9f)
            };

            var title = new Label
            {
                Text = "Historial",
                Left = 24, Top = 18, Width = 380, Height = 34,
                Font = AmoTheme.UI(18f, FontStyle.Bold),
                ForeColor = AmoTheme.Text
            };
            var sub = new Label
            {
                Text = "P?ginas visitadas recientemente en este perfil local.",
                Left = 25, Top = 55, Width = 520, Height = 22,
                ForeColor = AmoTheme.TextSoft
            };
            f.Controls.Add(title);
            f.Controls.Add(sub);

            var list = new ListView
            {
                Left = 24, Top = 92, Width = 754, Height = 410,
                View = View.Details, FullRowSelect = true, GridLines = false,
                BorderStyle = BorderStyle.FixedSingle, BackColor = AmoTheme.Surface,
                ForeColor = AmoTheme.Text, Font = AmoTheme.UI(9f)
            };
            list.Columns.Add("P?gina", 300);
            list.Columns.Add("Direcci\u00F3n", 430);

            foreach (string row in history.Take(300))
            {
                var parts = row.Split(new[] { '|' }, 2);
                string pageTitle = parts.Length > 1 ? parts[0] : parts[0];
                string url = parts.Length > 1 ? parts[1] : parts[0];
                var item = new ListViewItem(pageTitle);
                item.SubItems.Add(url);
                item.Tag = url;
                list.Items.Add(item);
            }

            list.DoubleClick += delegate
            {
                if (list.SelectedItems.Count == 0) return;
                string url = Convert.ToString(list.SelectedItems[0].Tag);
                if (!String.IsNullOrWhiteSpace(url))
                {
                    Navigate(url);
                    f.Close();
                }
            };
            f.Controls.Add(list);

            var clear = new AmoButton { Text = "Borrar historial", Variant = AmoButtonVariant.Danger, Left = 24, Top = 522, Width = 130, Height = 38 };
            var close = new AmoButton { Text = "Cerrar", Variant = AmoButtonVariant.Primary, Left = 678, Top = 522, Width = 100, Height = 38 };
            clear.Click += delegate
            {
                using (var modal = new AmoModalForm("Borrar historial", "Se eliminar? el historial local de ArrobAMO. Los marcadores no se modificar?n.", "Borrar", true))
                {
                    if (modal.ShowDialog(f) != DialogResult.OK) return;
                }
                history.Clear();
                SaveHistory();
                list.Items.Clear();
                ShowToast("Historial borrado.", AmoTheme.Success);
            };
            close.Click += delegate { f.Close(); };
            f.Controls.Add(clear);
            f.Controls.Add(close);
            f.Show(this);
        }

        string EditBookmarkEntry(string row)
        {
            var parts = (row ?? "").Split(new[] { '|' }, 2);
            string currentName = parts.Length > 1 ? parts[0] : "";
            string currentUrl = parts.Length > 1 ? parts[1] : parts[0];

            using (var f = new Form
            {
                Text = "Editar marcador ? ArrobAMO",
                ClientSize = new Size(560, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = AmoTheme.Bg, ForeColor = AmoTheme.Text,
                Font = AmoTheme.UI(9f)
            })
            {
                f.Controls.Add(new Label { Text = "Editar marcador", Left = 24, Top = 20, Width = 400, Height = 32, Font = AmoTheme.UI(17f, FontStyle.Bold), ForeColor = AmoTheme.Text });
                f.Controls.Add(new Label { Text = "Nombre", Left = 24, Top = 72, Width = 120, Height = 20, ForeColor = AmoTheme.TextSoft });
                var name = new TextBox { Left = 24, Top = 96, Width = 510, Height = 34, Text = currentName };
                AmoTheme.StyleInput(name, false);
                f.Controls.Add(name);

                f.Controls.Add(new Label { Text = "Direcci?n", Left = 24, Top = 146, Width = 120, Height = 20, ForeColor = AmoTheme.TextSoft });
                var url = new TextBox { Left = 24, Top = 170, Width = 510, Height = 34, Text = currentUrl };
                AmoTheme.StyleInput(url, false);
                f.Controls.Add(url);

                var cancel = new AmoButton { Text = "Cancelar", Variant = AmoButtonVariant.Secondary, Left = 334, Top = 232, Width = 96, Height = 38, DialogResult = DialogResult.Cancel };
                var save = new AmoButton { Text = "Guardar", Variant = AmoButtonVariant.Primary, Left = 438, Top = 232, Width = 96, Height = 38, DialogResult = DialogResult.OK };
                f.Controls.Add(cancel); f.Controls.Add(save);
                f.AcceptButton = save; f.CancelButton = cancel;

                if (f.ShowDialog(this) != DialogResult.OK) return null;
                string n = (name.Text ?? "").Trim().Replace("|", " ");
                string u = (url.Text ?? "").Trim();
                if (String.IsNullOrWhiteSpace(u)) return null;
                if (String.IsNullOrWhiteSpace(n)) n = u;
                return n + "|" + u;
            }
        }

        void ExportBookmarksHtml()
        {
            using (var dlg = new SaveFileDialog
            {
                Title = "Exportar marcadores",
                Filter = "Marcadores HTML (*.html)|*.html",
                FileName = "ArrobAMO-marcadores-" + DateTime.Now.ToString("yyyy-MM-dd") + ".html"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var lines = new List<string>();
                lines.Add("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
                lines.Add("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
                lines.Add("<TITLE>Marcadores ArrobAMO</TITLE>");
                lines.Add("<H1>Marcadores ArrobAMO</H1>");
                lines.Add("<DL><p>");
                foreach (string row in bookmarks)
                {
                    var p = row.Split(new[] { '|' }, 2);
                    string name = p.Length > 1 ? p[0] : p[0];
                    string url = p.Length > 1 ? p[1] : p[0];
                    lines.Add("  <DT><A HREF=\"" + System.Security.SecurityElement.Escape(url) + "\">" +
                              System.Security.SecurityElement.Escape(name) + "</A>");
                }
                lines.Add("</DL><p>");
                File.WriteAllLines(dlg.FileName, lines.ToArray(), Encoding.UTF8);
                ShowToast("Marcadores exportados.", AmoTheme.Success);
            }
        }

        void ShowBookmarksWindow()
        {
            var f = new Form
            {
                Text = "Marcadores \u00B7 ArrobAMO",
                Width = 820,
                Height = 600,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = AmoTheme.Bg,
                ForeColor = AmoTheme.Text,
                Font = AmoTheme.UI(9f)
            };

            f.Controls.Add(new Label
            {
                Text = "Marcadores",
                Left = 24, Top = 18, Width = 380, Height = 34,
                Font = AmoTheme.UI(18f, FontStyle.Bold),
                ForeColor = AmoTheme.Text
            });
            f.Controls.Add(new Label
            {
                Text = "Favoritos guardados e importados en este perfil.",
                Left = 25, Top = 55, Width = 520, Height = 22,
                ForeColor = AmoTheme.TextSoft
            });

            var list = new ListView
            {
                Left = 24, Top = 92, Width = 754, Height = 410,
                View = View.Details, FullRowSelect = true,
                BorderStyle = BorderStyle.FixedSingle, BackColor = AmoTheme.Surface,
                ForeColor = AmoTheme.Text, Font = AmoTheme.UI(9f)
            };
            list.Columns.Add("Nombre", 300);
            list.Columns.Add("Direcci\u00F3n", 430);

            foreach (string row in bookmarks.Take(1000))
            {
                var parts = row.Split(new[] { '|' }, 2);
                string name = parts.Length > 1 ? parts[0] : parts[0];
                string url = parts.Length > 1 ? parts[1] : parts[0];
                var item = new ListViewItem(name);
                item.SubItems.Add(url);
                item.Tag = url;
                list.Items.Add(item);
            }

            list.DoubleClick += delegate
            {
                if (list.SelectedItems.Count == 0) return;
                Navigate(Convert.ToString(list.SelectedItems[0].Tag));
                f.Close();
            };
            f.Controls.Add(list);

            var import = new AmoButton { Text = "Importar", Variant = AmoButtonVariant.Secondary, Left = 24, Top = 522, Width = 92, Height = 38 };
            var edit = new AmoButton { Text = "Editar", Variant = AmoButtonVariant.Secondary, Left = 126, Top = 522, Width = 92, Height = 38 };
            var remove = new AmoButton { Text = "Eliminar", Variant = AmoButtonVariant.Danger, Left = 228, Top = 522, Width = 92, Height = 38 };
            var export = new AmoButton { Text = "Exportar", Variant = AmoButtonVariant.Outline, Left = 330, Top = 522, Width = 96, Height = 38 };
            var close = new AmoButton { Text = "Cerrar", Variant = AmoButtonVariant.Primary, Left = 678, Top = 522, Width = 100, Height = 38 };

            import.Click += delegate { f.Close(); ShowImportData(); };
            edit.Click += delegate
            {
                if (list.SelectedItems.Count == 0) return;
                var selected = list.SelectedItems[0];
                string oldUrl = Convert.ToString(selected.Tag);
                int index = bookmarks.FindIndex(x => x.EndsWith("|" + oldUrl, StringComparison.OrdinalIgnoreCase));
                if (index < 0) return;
                string edited = EditBookmarkEntry(bookmarks[index]);
                if (String.IsNullOrWhiteSpace(edited)) return;
                bookmarks[index] = edited;
                SaveBookmarks();
                UpdateAddressSuggestions();
                var p2 = edited.Split(new[] { '|' }, 2);
                selected.Text = p2[0];
                selected.SubItems[1].Text = p2.Length > 1 ? p2[1] : p2[0];
                selected.Tag = p2.Length > 1 ? p2[1] : p2[0];
            };
            remove.Click += delegate
            {
                if (list.SelectedItems.Count == 0) return;
                string url = Convert.ToString(list.SelectedItems[0].Tag);
                bookmarks.RemoveAll(x => x.EndsWith("|" + url, StringComparison.OrdinalIgnoreCase));
                SaveBookmarks();
                UpdateAddressSuggestions();
                list.Items.Remove(list.SelectedItems[0]);
            };
            export.Click += delegate { ExportBookmarksHtml(); };
            close.Click += delegate { f.Close(); };
            f.Controls.Add(import); f.Controls.Add(edit); f.Controls.Add(remove); f.Controls.Add(export); f.Controls.Add(close);
            f.Show(this);
        }

        void ShowHistory()
        {
            historyMenu.Items.Clear();
            AmoTheme.StyleMenu(historyMenu);

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
            if (privateMode) return;
            if (String.IsNullOrWhiteSpace(url) || (!url.StartsWith("http://") && !url.StartsWith("https://"))) return;
            string safeTitle = String.IsNullOrWhiteSpace(title) ? url : title.Replace("|", " ");
            string row = safeTitle + "|" + url;
            history.RemoveAll(x => x.EndsWith("|" + url, StringComparison.OrdinalIgnoreCase));
            history.Insert(0, row);
            if (history.Count > 100) history.RemoveRange(100, history.Count - 100);
            SaveHistory();
            UpdateAddressSuggestions();
        }

        void UpdateAddressSuggestions()
        {
            try
            {
                var source = new AutoCompleteStringCollection();
                source.Add("arrobamo://inicio");
                source.Add("arrobamo://settings");
                source.Add("arrobamo://downloads");
                source.Add("arrobamo://history");
                source.Add("arrobamo://bookmarks");

                foreach (string row in history.Concat(bookmarks).Take(800))
                {
                    var parts = row.Split(new[] { '|' }, 2);
                    string url = parts.Length > 1 ? parts[1] : parts[0];
                    if (!String.IsNullOrWhiteSpace(url) && !source.Contains(url))
                        source.Add(url);
                }
                address.AutoCompleteCustomSource = source;
            }
            catch { }
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
            if (privateMode) return;
            try { File.WriteAllLines(HistoryFile, history.Take(100).ToArray()); } catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            bool ctrl = (keyData & Keys.Control) == Keys.Control;
            bool shift = (keyData & Keys.Shift) == Keys.Shift;
            bool alt = (keyData & Keys.Alt) == Keys.Alt;

            if (ctrl && shift && key == Keys.T)
            {
                ReopenClosedTab();
                return true;
            }
            if (ctrl && key == Keys.T)
            {
                BeginInvoke(new Action(async delegate { await AddTab("arrobamo://inicio"); }));
                return true;
            }
            if (ctrl && key == Keys.W)
            {
                if (tabs.SelectedIndex >= 0) CloseTab(tabs.SelectedIndex);
                return true;
            }
            if (ctrl && key == Keys.L)
            {
                address.Focus();
                address.SelectAll();
                return true;
            }
            if (ctrl && key == Keys.R)
            {
                var w = Active();
                if (w != null) w.Reload();
                return true;
            }
            if (ctrl && key == Keys.K)
            {
                DuplicateActiveTab();
                return true;
            }
            if (key == Keys.F5)
            {
                var w = Active();
                if (w != null) w.Reload();
                return true;
            }
            if (key == Keys.F12)
            {
                var w = Active();
                if (w != null && w.CoreWebView2 != null) w.CoreWebView2.OpenDevToolsWindow();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
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
                BeginInvoke(new Action(async delegate { await AddTab("amo://inicio"); }));
                e.SuppressKeyPress = true;
            }

            if (e.Control && e.KeyCode == Keys.W)
            {
                if (tabs.SelectedIndex >= 0) CloseTab(tabs.SelectedIndex);
                e.SuppressKeyPress = true;
            }
            if (e.Control && e.Shift && e.KeyCode == Keys.T)
            {
                ReopenClosedTab();
                e.SuppressKeyPress = true;
            }

            if (e.Control && e.KeyCode == Keys.K)
            {
                DuplicateActiveTab();
                e.SuppressKeyPress = true;
            }

            if (e.Control && e.KeyCode == Keys.R)
            {
                var w = Active();
                if (w != null) w.Reload();
                e.SuppressKeyPress = true;
            }

            if (e.KeyCode == Keys.F5)
            {
                var w = Active();
                if (w != null) w.Reload();
                e.SuppressKeyPress = true;
            }
        }

        string CurrentTabUrl(TabPage page)
        {
            if (page == null) return "";
            string tagged = Convert.ToString(page.Tag);
            if (!String.IsNullOrWhiteSpace(tagged)) return tagged;
            var web = page.Controls.OfType<WebView2>().FirstOrDefault();
            if (web == null || web.Source == null) return "";
            string src = web.Source.ToString();
            return src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || src.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase) ? "arrobamo://inicio" : src;
        }

        void DuplicateActiveTab()
        {
            if (tabs.SelectedTab == null) return;
            string url = CurrentTabUrl(tabs.SelectedTab);
            if (String.IsNullOrWhiteSpace(url)) url = "arrobamo://inicio";
            BeginInvoke(new Action(async delegate { await AddTab(url); }));
        }

        void ReopenClosedTab()
        {
            if (closedTabs.Count == 0)
            {
                ShowToast("No hay pesta\u00F1as cerradas para reabrir.", AmoTheme.Info);
                return;
            }
            string url = closedTabs.Pop();
            BeginInvoke(new Action(async delegate { await AddTab(url); }));
        }

        void ShowTabMenu(Point location)
        {
            if (tabs.SelectedTab == null) return;
            tabMenu.Items.Clear();
            AmoTheme.StyleMenu(tabMenu);

            var page = tabs.SelectedTab;
            var web = Active();
            bool pinned = pinnedTabs.Contains(page);
            bool muted = false;
            try { if (web != null && web.CoreWebView2 != null) muted = web.CoreWebView2.IsMuted; } catch { }

            var pin = new ToolStripMenuItem(pinned ? "Desfijar pesta\u00F1a" : "Fijar pesta\u00F1a");
            pin.Click += delegate
            {
                if (pinnedTabs.Contains(page)) pinnedTabs.Remove(page); else pinnedTabs.Add(page);
                tabs.Invalidate();
            };
            tabMenu.Items.Add(pin);

            var mute = new ToolStripMenuItem(muted ? "Activar sonido" : "Silenciar pesta\u00F1a");
            mute.Click += delegate
            {
                try { if (web != null && web.CoreWebView2 != null) web.CoreWebView2.IsMuted = !web.CoreWebView2.IsMuted; } catch { }
            };
            tabMenu.Items.Add(mute);

            var duplicate = new ToolStripMenuItem("Duplicar pesta\u00F1a");
            duplicate.Click += delegate { DuplicateActiveTab(); };
            tabMenu.Items.Add(duplicate);

            var reopen = new ToolStripMenuItem("Reabrir pesta\u00F1a cerrada    Ctrl+Shift+T");
            reopen.Enabled = closedTabs.Count > 0;
            reopen.Click += delegate { ReopenClosedTab(); };
            tabMenu.Items.Add(reopen);

            tabMenu.Items.Add(new ToolStripSeparator());
            var close = new ToolStripMenuItem("Cerrar pesta\u00F1a    Ctrl+W");
            close.Enabled = !pinned;
            close.Click += delegate { if (tabs.SelectedIndex >= 0) CloseTab(tabs.SelectedIndex); };
            tabMenu.Items.Add(close);

            tabMenu.Show(tabs, location);
        }

        void SaveSession()
        {
            if (privateMode) return;
            try
            {
                var urls = tabs.TabPages.Cast<TabPage>()
                    .Select(CurrentTabUrl)
                    .Where(x => !String.IsNullOrWhiteSpace(x))
                    .Take(40)
                    .ToArray();
                File.WriteAllLines(SessionFile, urls, Encoding.UTF8);
            }
            catch { }
        }

        void Address_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Navigate(address.Text);
        }

        string CanonicalInternal(string value)
        {
            value = (value ?? "").Trim();
            if (value.Length == 0) return "arrobamo://inicio";
            if (String.Equals(value, "http://localhost/avataramo", StringComparison.OrdinalIgnoreCase) || String.Equals(value, "http://localhost/avataramo/", StringComparison.OrdinalIgnoreCase)) return "arrobamo://avataramo";
            if (value.StartsWith("amo://", StringComparison.OrdinalIgnoreCase))
                return "arrobamo://" + value.Substring(6);
            return value;
        }

        bool IsInternal(string value, string route)
        {
            string canonical = CanonicalInternal(value);
            return String.Equals(canonical, "arrobamo://" + route, StringComparison.OrdinalIgnoreCase);
        }

        async Task<bool> RestoreSessionTabs()
        {
            try
            {
                if (!File.Exists(SessionFile)) return false;
                var urls = File.ReadAllLines(SessionFile, Encoding.UTF8)
                    .Select(CanonicalInternal)
                    .Where(x => !String.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(12)
                    .ToArray();

                if (urls.Length == 0) return false;
                foreach (string url in urls) await AddTab(url);
                return tabs.TabCount > 0;
            }
            catch
            {
                return false;
            }
        }

        void Navigate(string input)
        {
            input = CanonicalInternal(input);
            if (String.IsNullOrWhiteSpace(input)) input = "arrobamo://inicio";

            if (IsInternal(input, "settings"))
            {
                ShowSettings();
                SyncActive();
                return;
            }
            if (IsInternal(input, "downloads"))
            {
                downloadManager.Show(this);
                SyncActive();
                return;
            }
            if (IsInternal(input, "history"))
            {
                ShowHistoryWindow();
                SyncActive();
                return;
            }
            if (IsInternal(input, "bookmarks"))
            {
                ShowBookmarksWindow();
                SyncActive();
                return;
            }

            var web = Active();
            if (web == null) return;

            if (IsInternal(input, "inicio"))
            {
                if (tabs.SelectedTab != null)
                {
                    tabs.SelectedTab.Tag = "arrobamo://inicio";
                    tabs.SelectedTab.Text = "Inicio";
                }
                web.NavigateToString(GetStartPageHtml());
                address.Text = "arrobamo://inicio";
                securityLabel.Text = "LOCAL";
                securityLabel.ForeColor = AmoTheme.Info;
                return;
            }

            string normalized = Normalize(input);
            if (tabs.SelectedTab != null) tabs.SelectedTab.Tag = normalized;
            web.Source = new Uri(normalized);
        }

        static string DisplayAddress(string value)
        {
            Uri uri;
            if (Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                uri.Scheme == "http" && uri.Host == "127.0.0.1" && uri.Port == 18771 &&
                (uri.AbsolutePath == "/" || uri.AbsolutePath == "/index.html") &&
                String.IsNullOrEmpty(uri.Query) && String.IsNullOrEmpty(uri.Fragment))
                return "arrobamo://avataramo";
            return value;
        }

        string Normalize(string input)
        {
            input = CanonicalInternal(input);
            if (input.Length == 0) return "arrobamo://inicio";
            if (IsInternal(input, "avataramo")) return "http://127.0.0.1:18771/";
            if (input.StartsWith("arrobamo://", StringComparison.OrdinalIgnoreCase)) return input;

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
            string tagged = tabs.SelectedTab == null ? "" : CanonicalInternal(Convert.ToString(tabs.SelectedTab.Tag));
            string source = w.Source == null ? "" : w.Source.ToString();
            if (tagged.StartsWith("arrobamo://", StringComparison.OrdinalIgnoreCase) &&
                (source.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase) || source.StartsWith("data:", StringComparison.OrdinalIgnoreCase)))
                address.Text = tagged;
            else
                address.Text = DisplayAddress(source);
            back.Enabled = w.CanGoBack;
            forward.Enabled = w.CanGoForward;
            Text = w.CoreWebView2.DocumentTitle + (privateMode ? " - ArrobAMO ? Privado" : " - ArrobAMO");
            UpdateSecurityState();
        }

        string ShortTitle(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return "Nueva pestaña";
            return s.Length <= 28 ? s : s.Substring(0, 27) + "...";
        }

        // Semáforo de uso: 0-33 verde, >33-66 amarillo, >66 rojo.
        static Color MetricColor(double pct)
        {
            if (pct <= 33.3) return Color.FromArgb(73, 207, 115);
            if (pct <= 66.6) return Color.FromArgb(255, 209, 102);
            return Color.FromArgb(247, 91, 91);
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
                SetPrivacyLabel(cameraLabel, "CAM", PrivacyInUse("webcam"));
                SetPrivacyLabel(microphoneLabel, "MIC", PrivacyInUse("microphone"));
                cpuLabel.ForeColor = MetricColor(cpu);
                ramLabel.ForeColor = MetricColor(ram);
                gpuLabel.ForeColor = gpu < 0 ? Muted : MetricColor(gpu);
            }
            catch { }
        }

        static void SetPrivacyLabel(Label label, string name, bool? inUse)
        {
            label.Text = name + (inUse == true ? " USO" : inUse == false ? " LIBRE" : " ?");
            label.ForeColor = inUse == true ? Color.FromArgb(73, 207, 115) :
                              inUse == false ? Color.FromArgb(247, 91, 91) : AmoTheme.TextMuted;
        }

        static bool? PrivacyInUse(string kind)
        {
            try
            {
                using (var root = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + kind))
                {
                    if (root == null) return null;
                    bool found = false, active = false;
                    ReadPrivacy(root, 0, ref found, ref active);
                    return found ? (bool?)active : null;
                }
            }
            catch { return null; }
        }

        static void ReadPrivacy(RegistryKey key, int depth, ref bool found, ref bool active)
        {
            if (depth > 4) return;
            try
            {
                long start = Convert.ToInt64(key.GetValue("LastUsedTimeStart", 0L));
                long stop = Convert.ToInt64(key.GetValue("LastUsedTimeStop", 0L));
                if (start > 0 || stop > 0) { found = true; if (start > stop) active = true; }
                foreach (string name in key.GetSubKeyNames())
                    using (var child = key.OpenSubKey(name))
                        if (child != null) ReadPrivacy(child, depth + 1, ref found, ref active);
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
            string url=null, script=null; bool mini=false, record=false, importData=false, openAI=false, openElite=false, privateMode=false;
            if(args!=null) foreach(string a in args)
            {
                if(a.StartsWith("--script=",StringComparison.OrdinalIgnoreCase)) script=a.Substring(9).Trim('"');
                else if(a.Equals("--minimized",StringComparison.OrdinalIgnoreCase)) mini=true;
                else if(a.Equals("--record",StringComparison.OrdinalIgnoreCase)) record=true;
                else if(a.Equals("--import",StringComparison.OrdinalIgnoreCase)) importData=true;
                else if(a.Equals("--ai",StringComparison.OrdinalIgnoreCase)) openAI=true;
                else if(a.Equals("--elite",StringComparison.OrdinalIgnoreCase)) openElite=true;
                else if(a.Equals("--private",StringComparison.OrdinalIgnoreCase)) privateMode=true;
                else if(!a.StartsWith("--") && url==null) url=a;
            }
            var splash = new AmoSplashForm();
            splash.Show();
            Application.DoEvents();

            var browser = new BrowserForm(url, script, mini, record, importData, openAI, openElite, privateMode);
            browser.Opacity = 0;
            browser.Shown += delegate
            {
                var reveal = new Timer();
                reveal.Interval = 650;
                reveal.Tick += delegate
                {
                    reveal.Stop();
                    reveal.Dispose();
                    browser.Opacity = 1;
                    if (!splash.IsDisposed) splash.Close();
                };
                reveal.Start();
            };
            Application.Run(browser);
        }
    }
}































