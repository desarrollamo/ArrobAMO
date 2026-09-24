using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ArrobAMOSetup
{
    public sealed class OrbitMark : Control
    {
        public OrbitMark()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height);
            float cx = Width/2f, cy = Height/2f;
            using (var pen = new Pen(ForeColor, Math.Max(2f,size*.055f)))
            using (var brush = new SolidBrush(ForeColor))
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                float r1=size*.18f; e.Graphics.DrawEllipse(pen,cx-r1,cy-r1,r1*2,r1*2);
                float r2=size*.34f; e.Graphics.DrawArc(pen,cx-r2,cy-r2,r2*2,r2*2,205,230);
                float r3=size*.44f; e.Graphics.DrawArc(pen,cx-r3,cy-r3,r3*2,r3*2,28,125); e.Graphics.DrawArc(pen,cx-r3,cy-r3,r3*2,r3*2,190,118);
                DrawDot(e.Graphics,brush,cx,cy,size*.39f,334,size*.055f);
                DrawDot(e.Graphics,brush,cx,cy,size*.43f,99,size*.05f);
                DrawDot(e.Graphics,brush,cx,cy,size*.40f,156,size*.06f);
            }
        }
        static void DrawDot(Graphics g, Brush b, float cx, float cy, float radius, float deg, float dot)
        {
            double a=deg*Math.PI/180.0;
            float x=cx+(float)Math.Cos(a)*radius, y=cy+(float)Math.Sin(a)*radius;
            g.FillEllipse(b,x-dot,y-dot,dot*2,dot*2);
        }
    }
    public sealed class SetupForm : Form
    {
        readonly Color Ink = ColorTranslator.FromHtml("#141414");
        readonly Color Bg = ColorTranslator.FromHtml("#F7F7F5");
        readonly Color Muted = ColorTranslator.FromHtml("#B8B8B8");
        readonly Color Border = ColorTranslator.FromHtml("#DDDDDA");

        readonly RichTextBox terms = new RichTextBox();
        readonly CheckBox accept = new CheckBox();
        readonly CheckBox launch = new CheckBox();
        readonly Button install = new Button();
        readonly Label status = new Label();
        readonly ProgressBar progress = new ProgressBar();

        string Destination
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ArrobAMO"); }
        }

        public SetupForm()
        {
            Text = "Instalar ArrobAMO";
            Width = 720;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Bg;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Ink };
            Controls.Add(header);

            var mark = new OrbitMark { Left = 24, Top = 20, Width = 76, Height = 46, ForeColor = Color.White, BackColor = Ink };
            header.Controls.Add(mark);

            var title = new Label {
                Text = "ArrobAMO",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = true, Left = 116, Top = 18
            };
            header.Controls.Add(title);

            var subtitle = new Label {
                Text = "Navegador · v0.5.3 · Tecnología con alma.",
                ForeColor = Muted,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true, Left = 119, Top = 58
            };
            header.Controls.Add(subtitle);

            var intro = new Label {
                Text = "Instalación para este usuario",
                Left = 24, Top = 112, AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Ink
            };
            Controls.Add(intro);

            var path = new Label {
                Text = Destination,
                Left = 24, Top = 140, Width = 650, Height = 22,
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.DimGray
            };
            Controls.Add(path);

            terms.Left = 24; terms.Top = 174; terms.Width = 650; terms.Height = 250;
            terms.ReadOnly = true; terms.BackColor = Color.FromArgb(248,248,248);
            terms.BorderStyle = BorderStyle.FixedSingle;
            terms.Font = new Font("Segoe UI", 9f);
            terms.Text = ReadResourceText("LICENSE-USER.txt");
            Controls.Add(terms);

            accept.Text = "Acepto las condiciones de uso";
            accept.Left = 24; accept.Top = 438; accept.Width = 300; accept.Height = 24;
            accept.CheckedChanged += delegate { install.Enabled = accept.Checked; };
            Controls.Add(accept);

            launch.Text = "Abrir ArrobAMO al terminar";
            launch.Left = 24; launch.Top = 466; launch.Width = 330; launch.Height = 24;
            launch.Checked = false;
            Controls.Add(launch);

            progress.Left = 24; progress.Top = 500; progress.Width = 460; progress.Height = 22;
            progress.Minimum = 0; progress.Maximum = 5; progress.Value = 0;
            Controls.Add(progress);

            status.Left = 24; status.Top = 530; status.Width = 460; status.Height = 28;
            status.Text = "Listo para instalar.";
            status.ForeColor = Color.DimGray;
            Controls.Add(status);

            install.Text = "Instalar";
            install.Left = 540; install.Top = 492; install.Width = 134; install.Height = 40;
            install.Enabled = false;
            install.BackColor = Ink; install.ForeColor = Color.White;
            install.FlatStyle = FlatStyle.Flat; install.FlatAppearance.BorderSize = 0;
            install.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            install.Click += Install_Click;
            Controls.Add(install);
        }

        void Install_Click(object sender, EventArgs e)
        {
            if (status.Text == "Instalación completada.") { Close(); return; }
            Install();
        }

        void Install()
        {
            try
            {
                install.Enabled = false; accept.Enabled = false;
                Directory.CreateDirectory(Destination);

                Extract("ArrobAMO.exe"); progress.Value = 1;
                Extract("Microsoft.Web.WebView2.Core.dll"); progress.Value = 2;
                Extract("Microsoft.Web.WebView2.WinForms.dll"); progress.Value = 3;
                Extract("WebView2Loader.dll"); progress.Value = 4;
                File.WriteAllText(Path.Combine(Destination, "LICENSE-USER.txt"), ReadResourceText("LICENSE-USER.txt"), Encoding.UTF8);
                WriteUninstaller();
                CreateShortcuts();
                RegisterUninstall();
                progress.Value = 5;

                status.Text = "Instalación completada.";
                status.ForeColor = Color.ForestGreen;
                install.Text = "Cerrar";
                install.Enabled = true;


                if (launch.Checked)
                    Process.Start(Path.Combine(Destination, "ArrobAMO.exe"));

                ShowWelcome();
            }
            catch (Exception ex)
            {
                status.Text = "No se pudo completar la instalación.";
                status.ForeColor = Color.Firebrick;
                install.Enabled = true; accept.Enabled = true;
                MessageBox.Show(ex.Message, "Error de instalación", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ShowWelcome()
        {
            terms.Visible = false;
            accept.Visible = false;
            launch.Visible = false;
            progress.Visible = false;
            install.Visible = false;

            status.Left = 32;
            status.Top = 138;
            status.Width = 620;
            status.Height = 56;
            status.Text = "ArrobAMO est\u00E1 listo";
            status.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            status.ForeColor = Ink;

            var desc = new Label {
                Text = "Tu navegador para navegar, automatizar y trabajar con IA.",
                Left = 32, Top = 192, Width = 620, Height = 28,
                Font = new Font("Segoe UI", 10f), ForeColor = Color.DimGray
            };
            Controls.Add(desc);

            AddWelcomeButton("Importar datos", 32, 245, true, delegate { LaunchImport(); });
            AddWelcomeButton("Conectar IA", 354, 245, true, delegate { LaunchAI(); });
            AddWelcomeButton("Activar Loop", 32, 305, true, delegate { LaunchLoop(); });
            AddWelcomeButton("Abrir ArrobAMO", 354, 305, true, delegate { LaunchBrowser(); });
            AddWelcomeButton("Ver tutorial r\u00E1pido", 32, 365, true, delegate { Process.Start("https://github.com/desarrollamo/ArrobAMO"); });

            var note = new Label {
                Text = "Favoritos de Chrome, Edge, Brave y Opera ya pueden importarse. Historial, contrase\u00F1as y sesiones siguen deshabilitados hasta contar con una migraci\u00F3n segura.",
                Left = 32, Top = 430, Width = 620, Height = 44,
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.DimGray
            };
            Controls.Add(note);
        }

        void AddWelcomeButton(string text, int left, int top, bool enabled, EventHandler action)
        {
            var b = new Button { Text = text, Left = left, Top = top, Width = 290, Height = 44, Enabled = enabled };
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = enabled ? Ink : Border;
            b.BackColor = enabled ? Color.White : Color.FromArgb(238,238,236);
            b.ForeColor = enabled ? Ink : Color.Gray;
            b.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            b.Click += action;
            Controls.Add(b);
        }

        void LaunchBrowser()
        {
            string exe = Path.Combine(Destination, "ArrobAMO.exe");
            if (File.Exists(exe)) Process.Start(exe);
        }
        void LaunchLoop()
        {
            string exe = Path.Combine(Destination, "ArrobAMO.exe");
            if (File.Exists(exe)) Process.Start(exe, "--record");
        }
        void LaunchImport()
        {
            string exe = Path.Combine(Destination, "ArrobAMO.exe");
            if (File.Exists(exe)) Process.Start(exe, "--import");
        }
        void LaunchAI()
        {
            string exe = Path.Combine(Destination, "ArrobAMO.exe");
            if (File.Exists(exe)) Process.Start(exe, "--ai");
        }
        void Extract(string name)
        {
            string output = Path.Combine(Destination, name);
            using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (input == null) throw new Exception("Falta recurso: " + name);
                using (FileStream file = File.Create(output)) input.CopyTo(file);
            }
        }

        string ReadResourceText(string name)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) return "Condiciones de uso no disponibles.";
                using (var r = new StreamReader(s, Encoding.UTF8)) return r.ReadToEnd();
            }
        }

        void WriteUninstaller()
        {
            string self = Path.Combine(Destination, "Desinstalar.cmd");
            string content =
@"@echo off
taskkill /IM ArrobAMO.exe /F >nul 2>nul
timeout /t 1 /nobreak >nul
reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\ArrobAMO"" /f >nul 2>nul
del ""%USERPROFILE%\Desktop\ArrobAMO.lnk"" >nul 2>nul
del ""%APPDATA%\Microsoft\Windows\Start Menu\Programs\ArrobAMO.lnk"" >nul 2>nul
cd /d ""%TEMP%""
rmdir /s /q ""%LOCALAPPDATA%\Programs\ArrobAMO""
";
            File.WriteAllText(self, content, Encoding.ASCII);
        }

        void CreateShortcuts()
        {
            string exe = Path.Combine(Destination, "ArrobAMO.exe");
            string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ArrobAMO.lnk");
            string start = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "ArrobAMO.lnk");
            string ps =
                "$ws=New-Object -ComObject WScript.Shell;" +
                "$targets=@('" + Esc(desktop) + "','" + Esc(start) + "');" +
                "foreach($p in $targets){$s=$ws.CreateShortcut($p);$s.TargetPath='" + Esc(exe) + "';$s.WorkingDirectory='" + Esc(Destination) + "';$s.Description='ArrobAMO';$s.Save()}";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(ps));
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + encoded);
            psi.CreateNoWindow = true; psi.UseShellExecute = false;
            using (var p = Process.Start(psi)) { p.WaitForExit(); }
        }

        string Esc(string s) { return s.Replace("'", "''"); }

        void RegisterUninstall()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ArrobAMO"))
            {
                key.SetValue("DisplayName", "ArrobAMO");
                key.SetValue("DisplayVersion", "0.5.3");
                key.SetValue("Publisher", "DesarrollAMO");
                key.SetValue("InstallLocation", Destination);
                key.SetValue("DisplayIcon", Path.Combine(Destination, "ArrobAMO.exe"));
                key.SetValue("UninstallString", Path.Combine(Destination, "Desinstalar.cmd"));
                key.SetValue("URLInfoAbout", "https://github.com/desarrollamo/ArrobAMO");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }
}







