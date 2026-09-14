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
                float h = Height - 4, y = Height - 2, w = Math.Max(12, Width / 3f - 3);
                e.Graphics.FillPolygon(b, new PointF[] { new PointF(2,y), new PointF(2+w/2,y-h*0.72f), new PointF(2+w,y) });
                e.Graphics.FillPolygon(b, new PointF[] { new PointF(Width/2f-w/2,y), new PointF(Width/2f,2), new PointF(Width/2f+w/2,y) });
                e.Graphics.FillPolygon(b, new PointF[] { new PointF(Width-w-2,y), new PointF(Width-2-w/2,y-h*0.62f), new PointF(Width-2,y) });
            }
        }
    }
    public sealed class SetupForm : Form
    {
        readonly Color Ink = ColorTranslator.FromHtml("#111827");
        readonly Color Pink = ColorTranslator.FromHtml("#FF5AA5");
        readonly Color Sky = ColorTranslator.FromHtml("#7DD3FC");
        readonly Color Mist = ColorTranslator.FromHtml("#E5E7EB");

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
            BackColor = Color.White;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Ink };
            Controls.Add(header);

            var mark = new PyramidMark { Left = 24, Top = 20, Width = 76, Height = 46, ForeColor = Color.White, BackColor = Ink };
            header.Controls.Add(mark);

            var title = new Label {
                Text = "ArrobAMO",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = true, Left = 116, Top = 18
            };
            header.Controls.Add(title);

            var subtitle = new Label {
                Text = "Navegador · v0.3.1 · Tecnología con alma.",
                ForeColor = Sky,
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
            install.BackColor = Pink; install.ForeColor = Color.White;
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
            status.Text = "Bienvenido a ArrobAMO";
            status.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            status.ForeColor = Ink;

            var desc = new Label {
                Text = "Tu navegador para navegar, automatizar y trabajar con IA.",
                Left = 32, Top = 192, Width = 620, Height = 28,
                Font = new Font("Segoe UI", 10f), ForeColor = Color.DimGray
            };
            Controls.Add(desc);

            AddWelcomeButton("Importar datos · próximamente", 32, 245, false, delegate { });
            AddWelcomeButton("Conectar IA", 354, 245, true, delegate { LaunchBrowser(); });
            AddWelcomeButton("Activar Loop · próximamente", 32, 305, false, delegate { });
            AddWelcomeButton("Abrir ArrobAMO", 354, 305, true, delegate { LaunchBrowser(); });
            AddWelcomeButton("Ver tutorial rápido", 32, 365, true, delegate { Process.Start("https://github.com/desarrollamo/ArrobAMO"); });

            var note = new Label {
                Text = "Importación de datos y Loop se habilitarán cuando su ejecución sea real y verificable.",
                Left = 32, Top = 430, Width = 620, Height = 44,
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.DimGray
            };
            Controls.Add(note);
        }

        void AddWelcomeButton(string text, int left, int top, bool enabled, EventHandler action)
        {
            var b = new Button { Text = text, Left = left, Top = top, Width = 290, Height = 44, Enabled = enabled };
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = enabled ? Ink : Color.LightGray;
            b.BackColor = enabled ? Color.White : Color.FromArgb(245,245,245);
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
                key.SetValue("DisplayVersion", "0.3.1");
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





