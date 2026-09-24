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

        string isolatedTestDirectory;
        bool isolatedTest;
        string Destination
        {
            get { return isolatedTest ? isolatedTestDirectory :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ArrobAMO"); }
        }
        internal void SetIsolatedTestDirectory(string directory)
        {
            string temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string proposed = Path.GetFullPath(directory);
            if(!proposed.StartsWith(temp+"ArrobAMO-Installer-Integration-", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La prueba solo puede instalar en una carpeta temporal aislada.");
            if(Directory.Exists(proposed)) throw new InvalidOperationException("La carpeta de prueba ya existe.");
            isolatedTestDirectory=proposed;
            isolatedTest=true;
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
                Text = "Navegador · v0.5.3.1 · Tecnología con alma.",
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
            launch.Checked = true;
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
            var requirement = new Label
            {
                Text = "Windows 10/11 x64 Â· WebView2 Runtime requerido Â· sin permisos de administrador.",
                Left = 24, Top = 563, Width = 650, Height = 20,
                ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8f)
            };
            Controls.Add(requirement);
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
                if (Environment.Is64BitOperatingSystem == false)
                    throw new Exception("ArrobAMO requiere Windows de 64 bits.");
                if (!isolatedTest && InstalledBrowserRunning())
                    throw new Exception("CerrÃ¡ ArrobAMO y volvÃ© a intentar. No se reemplazÃ³ ninguna instalaciÃ³n activa.");
                InstallPayload();
                progress.Value = 4;
                WriteUninstaller();
                if (!isolatedTest)
                {
                    CreateShortcuts();
                    RegisterUninstall();
                }
                progress.Value = 5;
                status.Text = "Archivos, accesos y desinstalador registrados.";

                status.Text = "Instalación completada.";
                status.ForeColor = Color.ForestGreen;
                install.Text = "Cerrar";
                install.Enabled = true;


                bool runtimePresent=WebView2RuntimePresent();
                if (!isolatedTest && !runtimePresent)
                {
                    launch.Checked=false;
                    var answer=MessageBox.Show(
                        "ArrobAMO se instaló, pero falta Microsoft WebView2 Runtime. ¿Abrir la página oficial de Microsoft para instalarlo?",
                        "Falta WebView2 Runtime", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if(answer==DialogResult.Yes)
                        Process.Start(new ProcessStartInfo("https://developer.microsoft.com/en-us/microsoft-edge/webview2/") { UseShellExecute=true });
                }
                if (launch.Checked && !isolatedTest && runtimePresent)
                    Process.Start(Path.Combine(Destination, "ArrobAMO.exe"));

                ShowWelcome();
            }
            catch (Exception ex)
            {
                status.Text = "No se pudo completar la instalación.";
                status.ForeColor = Color.Firebrick;
                install.Enabled = true; accept.Enabled = true;
                if (isolatedTest) throw;
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
        static readonly string[] PayloadNames = new [] {
            "ArrobAMO.exe", "Microsoft.Web.WebView2.Core.dll",
            "Microsoft.Web.WebView2.WinForms.dll", "WebView2Loader.dll",
            "LICENSE-USER.txt", "ArrobAMOUninstaller.exe"
        };

        void InstallPayload()
        {
            string staging = Path.Combine(Path.GetTempPath(), "ArrobAMO-Setup-"+Guid.NewGuid().ToString("N"));
            string backup = isolatedTest ? Path.Combine(Path.GetTempPath(), "ArrobAMO-Installer-Integration-Backup-"+Guid.NewGuid().ToString("N")) :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArrobAMO", "InstallerBackups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
            string[] committed = new string[PayloadNames.Length];
            int commitCount = 0;
            try
            {
                Directory.CreateDirectory(staging);
                foreach(string name in PayloadNames)
                {
                    string path = Path.Combine(staging,name);
                    using(Stream input=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                    {
                        if(input == null) throw new IOException("Falta un archivo del instalador: "+name);
                        using(FileStream output=File.Create(path)) input.CopyTo(output);
                    }
                    if(new FileInfo(path).Length < 200) throw new IOException("Archivo incompleto: "+name);
                }
                string version = FileVersionInfo.GetVersionInfo(Path.Combine(staging,"ArrobAMO.exe")).FileVersion;
                if(version!="0.5.3.1") throw new IOException("Versión del instalador no coincide con los archivos.");
                Directory.CreateDirectory(Destination);
                Directory.CreateDirectory(backup);
                foreach(string name in PayloadNames)
                {
                    string target=Path.Combine(Destination,name);
                    if(File.Exists(target)) File.Copy(target,Path.Combine(backup,name),true);
                    File.Copy(Path.Combine(staging,name),target,true);
                    committed[commitCount++]=name;
                }
            }
            catch
            {
                for(int i=commitCount-1;i>=0;i--)
                {
                    string name=committed[i], target=Path.Combine(Destination,name), old=Path.Combine(backup,name);
                    try { if(File.Exists(old)) File.Copy(old,target,true); else if(File.Exists(target)) File.Delete(target); } catch { }
                }
                throw;
            }
            finally
            {
                try { if(Directory.Exists(staging)) Directory.Delete(staging,true); } catch {}
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

        static bool WebView2RuntimePresent()
        {
            const string path=@"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
            foreach(var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach(var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using(var root=RegistryKey.OpenBaseKey(hive,view))
                    using(var key=root.OpenSubKey(path))
                    {
                        if(key==null)continue;
                        string version=Convert.ToString(key.GetValue("pv"));
                        if(!String.IsNullOrWhiteSpace(version) && version!="0.0.0.0")
                            return true;
                    }
                }
                catch { }
            }
            return false;
        }

        bool InstalledBrowserRunning()
        {
            string root = Path.GetFullPath(Destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach(var process in Process.GetProcessesByName("ArrobAMO"))
            {
                try
                {
                    string path = Path.GetFullPath(process.MainModule.FileName);
                    if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch { return true; } // Don't overwrite an unknown running instance.
                finally { process.Dispose(); }
            }
            return false;
        }

        void WriteUninstaller()
        {
            string path = Path.Combine(Destination, "ArrobAMOUninstaller.exe");
            if (!File.Exists(path)) throw new IOException("No se pudo instalar el desinstalador.");
            // Profile stays in %LOCALAPPDATA%\ArrobAMO; only program files are managed here.
        }

        void CreateShortcuts()
        {
            string exe = Path.Combine(Destination, "ArrobAMO.exe");
            string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ArrobAMO.lnk");
            string start = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "ArrobAMO.lnk");
            string ps =
                "$ws=New-Object -ComObject WScript.Shell;" +
                "$targets=@('" + Esc(desktop) + "','" + Esc(start) + "');" +
                "foreach($p in $targets){$s=$ws.CreateShortcut($p);$s.TargetPath='" + Esc(exe) + "';$s.WorkingDirectory='" + Esc(Destination) + "';$s.IconLocation='" + Esc(exe) + ",0';$s.Description='ArrobAMO 0.5.3.1';$s.Save()}";
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(ps));
            var psi = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -EncodedCommand " + encoded);
            psi.CreateNoWindow = true; psi.UseShellExecute = false;
            using (var p = Process.Start(psi)) { p.WaitForExit(); if (p.ExitCode != 0) throw new IOException("No se pudieron crear los accesos directos."); }
        }

        string Esc(string s) { return s.Replace("'", "''"); }

        void RegisterUninstall()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ArrobAMO"))
            {
                key.SetValue("DisplayName", "ArrobAMO");
                key.SetValue("DisplayVersion", "0.5.3.1");
                key.SetValue("Publisher", "DesarrollAMO");
                key.SetValue("InstallLocation", Destination);
                key.SetValue("DisplayIcon", Path.Combine(Destination, "ArrobAMO.exe"));
                key.SetValue("UninstallString", "\""+Path.Combine(Destination, "ArrobAMOUninstaller.exe")+"\"");
                key.SetValue("URLInfoAbout", "https://github.com/desarrollamo/ArrobAMO");
                key.SetValue("EstimatedSize", 1600, RegistryValueKind.DWord);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        static int VerifyPackage()
        {
            string test = Path.Combine(Path.GetTempPath(), "ArrobAMO-Installer-Test-"+Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(test);
                foreach (string name in new [] { "ArrobAMO.exe", "ArrobAMOUninstaller.exe",
                    "Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.WinForms.dll", "WebView2Loader.dll" })
                {
                    using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                    {
                        if (input == null) return 2;
                        using (FileStream output = File.Create(Path.Combine(test,name))) input.CopyTo(output);
                    }
                    if(new FileInfo(Path.Combine(test,name)).Length < 1000) return 3;
                }
                using(Stream input=Assembly.GetExecutingAssembly().GetManifestResourceStream("LICENSE-USER.txt"))
                    if(input == null || input.Length < 200) return 4;
                string version = FileVersionInfo.GetVersionInfo(Path.Combine(test,"ArrobAMO.exe")).FileVersion;
                return version=="0.5.3.1" ? 0 : 5;
            }
            catch { return 6; }
            finally
            {
                try { if(Directory.Exists(test)) Directory.Delete(test,true); } catch {}
            }
        }

        [STAThread]
        static int Main(string[] args)
        {
            if(args != null && args.Length==1 && args[0]=="--self-test")
                return VerifyPackage();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
            return 0;
        }
    }
}







