using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ArrobAMOSetup
{
    public static class UninstallProgram
    {
        static string InstallDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ArrobAMO"); }
        }

        static bool InstalledBrowserRunning()
        {
            string root = Path.GetFullPath(InstallDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (var process in Process.GetProcessesByName("ArrobAMO"))
            {
                try
                {
                    string path = Path.GetFullPath(process.MainModule.FileName);
                    if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch { return true; }
                finally { process.Dispose(); }
            }
            return false;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            if (InstalledBrowserRunning())
            {
                MessageBox.Show("Cerrá todas las ventanas de ArrobAMO y volvé a ejecutar el desinstalador.",
                    "ArrobAMO está en uso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 2;
            }

            using (var dialog = new Form())
            {
                dialog.Text = "Desinstalar ArrobAMO";
                dialog.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ClientSize = new Size(500, 220);
                dialog.BackColor = Color.White;
                var message = new Label
                {
                    Text = "¿Querés desinstalar ArrobAMO?\n\nTus favoritos, historial, sesiones y configuración se conservarán.",
                    Left = 25, Top = 23, Width = 450, Height = 100,
                    Font = new Font("Segoe UI", 10f)
                };
                var cancel = new Button { Text = "Cancelar", Left = 224, Top = 151, Width = 110, Height = 40, DialogResult = DialogResult.Cancel };
                var confirm = new Button { Text = "Desinstalar", Left = 347, Top = 151, Width = 128, Height = 40, DialogResult = DialogResult.OK };
                dialog.Controls.AddRange(new Control[] { message, cancel, confirm });
                dialog.AcceptButton = confirm;
                dialog.CancelButton = cancel;
                if (dialog.ShowDialog() != DialogResult.OK) return 1;
            }

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall", true))
                    if (key != null) key.DeleteSubKeyTree("ArrobAMO", false);

                string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ArrobAMO.lnk");
                string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "ArrobAMO.lnk");
                foreach (string link in new[] { desktop, startMenu })
                {
                    // Only remove the shortcut if it is known to point to our installation.
                    if (!File.Exists(link)) continue;
                    try
                    {
                        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                        object shell = Activator.CreateInstance(shellType);
                        object shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { link });
                        string target = Convert.ToString(shortcut.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null));
                        if (Path.GetFullPath(target).StartsWith(Path.GetFullPath(InstallDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                            File.Delete(link);
                    }
                    catch { } // Never delete a shortcut whose target cannot be verified.
                }
                // Run removal only after this executable exits. Never touch %LOCALAPPDATA%\ArrobAMO (user profile).
                string dest = InstallDirectory;
                string script = "timeout /t 3 /nobreak >nul & rmdir /s /q \"" + dest + "\"";
                var psi = new ProcessStartInfo("cmd.exe", "/d /c " + script)
                {
                    UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden
                };
                MessageBox.Show("ArrobAMO se desinstalará. Se conservaron tus datos de navegación.",
                    "Desinstalación", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start(psi);
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo completar la desinstalación: " + ex.Message,
                    "ArrobAMO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 3;
            }
        }
    }
}
