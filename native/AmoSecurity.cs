using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace ArrobAMO
{
    public sealed class AmoSecurityManager
    {
        readonly string file;
        public bool MicrophoneEnabled { get; set; }
        public bool CameraEnabled { get; set; }
        readonly Dictionary<string, CoreWebView2PermissionState> rules =
            new Dictionary<string, CoreWebView2PermissionState>(StringComparer.OrdinalIgnoreCase);
        readonly Form owner;
        readonly Action<string, Color> notify;

        public AmoSecurityManager(string root, Form ownerForm, Action<string, Color> notifier)
        {
            MicrophoneEnabled = true; CameraEnabled = true;
            owner = ownerForm;
            notify = notifier;
            file = Path.Combine(root, "permissions.txt");
            Load();
        }

        public void Attach(CoreWebView2 core)
        {
            if (core == null) return;
            try { core.Settings.IsReputationCheckingRequired = true; } catch { }

            core.PermissionRequested += delegate(object sender, CoreWebView2PermissionRequestedEventArgs e)
            {
                HandlePermission(e);
            };
        }

        void HandlePermission(CoreWebView2PermissionRequestedEventArgs e)
        {
            if ((e.PermissionKind == CoreWebView2PermissionKind.Microphone && !MicrophoneEnabled) ||
                (e.PermissionKind == CoreWebView2PermissionKind.Camera && !CameraEnabled))
            {
                e.State = CoreWebView2PermissionState.Deny; e.Handled = true; e.SavesInProfile = false; return;
            }
            string host = HostFrom(e.Uri);
            string key = host + "|" + e.PermissionKind;
            CoreWebView2PermissionState saved;
            if (rules.TryGetValue(key, out saved))
            {
                e.State = saved;
                e.Handled = true;
                e.SavesInProfile = false;
                return;
            }

            var result = AmoPermissionPrompt.Show(owner, host, e.PermissionKind.ToString());
            if (result == AmoPermissionChoice.AllowOnce)
            {
                e.State = CoreWebView2PermissionState.Allow;
                e.Handled = true;
                e.SavesInProfile = false;
            }
            else if (result == AmoPermissionChoice.AllowAlways)
            {
                e.State = CoreWebView2PermissionState.Allow;
                e.Handled = true;
                e.SavesInProfile = false;
                rules[key] = CoreWebView2PermissionState.Allow;
                Save();
            }
            else
            {
                e.State = CoreWebView2PermissionState.Deny;
                e.Handled = true;
                e.SavesInProfile = false;
                if (result == AmoPermissionChoice.BlockAlways)
                {
                    rules[key] = CoreWebView2PermissionState.Deny;
                    Save();
                    if (notify != null) notify("Bloqueo recordado para " + host + ".", AmoTheme.Success);
                }
            }
        }

        public bool ShouldBlockPopup(bool isUserInitiated, string uri)
        {
            if (isUserInitiated) return false;
            if (notify != null) notify("Popup bloqueado: " + HostFrom(uri), AmoTheme.Warning);
            return true;
        }

        public KeyValuePair<string, CoreWebView2PermissionState>[] Snapshot()
        {
            return rules.OrderBy(x => x.Key).ToArray();
        }

        public void Remove(string key)
        {
            if (rules.Remove(key)) Save();
        }

        public void Clear()
        {
            rules.Clear();
            Save();
        }

        static string HostFrom(string uri)
        {
            try { return new Uri(uri).Host; }
            catch { return String.IsNullOrWhiteSpace(uri) ? "sitio desconocido" : uri; }
        }

        void Load()
        {
            try
            {
                if (!File.Exists(file)) return;
                foreach (string line in File.ReadAllLines(file))
                {
                    var p = line.Split('|');
                    if (p.Length != 3) continue;
                    CoreWebView2PermissionState state;
                    if (!Enum.TryParse(p[2], true, out state)) continue;
                    rules[p[0] + "|" + p[1]] = state;
                }
            }
            catch { }
        }

        void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(file);
                if (!String.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

                string[] lines = rules.Select(x =>
                {
                    int i = x.Key.LastIndexOf('|');
                    string host = i > 0 ? x.Key.Substring(0, i) : x.Key;
                    string kind = i > 0 ? x.Key.Substring(i + 1) : "";
                    return host + "|" + kind + "|" + x.Value;
                }).ToArray();

                string temp = file + ".tmp";
                File.WriteAllLines(temp, lines);
                if (File.Exists(file)) File.Delete(file);
                File.Move(temp, file);
            }
            catch (Exception ex)
            {
                try
                {
                    File.AppendAllText(Path.Combine(Path.GetDirectoryName(file), "security-errors.log"),
                        DateTime.Now.ToString("s") + " " + ex + Environment.NewLine);
                }
                catch { }
            }
        }
    }

    public enum AmoPermissionChoice
    {
        BlockOnce,
        BlockAlways,
        AllowOnce,
        AllowAlways
    }

    public sealed class AmoPermissionPrompt : Form
    {
        public AmoPermissionChoice Choice { get; private set; }

        AmoPermissionPrompt(string host, string kind)
        {
            Text = "Permiso del sitio · ArrobAMO";
            ClientSize = new Size(520, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = AmoTheme.Surface; ForeColor = AmoTheme.Text;
            Font = AmoTheme.UI(9f);

            Controls.Add(new Label
            {
                Text = "Permiso solicitado",
                Left = 24, Top = 22, Width = 420, Height = 32,
                Font = AmoTheme.UI(16f, FontStyle.Bold), ForeColor = AmoTheme.Text
            });
            Controls.Add(new Label
            {
                Text = host + " solicita: " + Friendly(kind),
                Left = 24, Top = 64, Width = 460, Height = 46,
                ForeColor = AmoTheme.TextSoft
            });
            Controls.Add(new Label
            {
                Text = "Elegí si ArrobAMO debe permitirlo sólo ahora o recordar la decisión para este sitio.",
                Left = 24, Top = 116, Width = 460, Height = 44,
                ForeColor = AmoTheme.TextSoft
            });

            var block = new AmoButton { Text = "Bloquear", Variant = AmoButtonVariant.Secondary, Left = 24, Top = 196, Width = 104, Height = 38 };
            var blockAlways = new AmoButton { Text = "Bloquear siempre", Variant = AmoButtonVariant.Outline, Left = 138, Top = 196, Width = 130, Height = 38 };
            var allow = new AmoButton { Text = "Permitir una vez", Variant = AmoButtonVariant.Secondary, Left = 278, Top = 196, Width = 118, Height = 38 };
            var allowAlways = new AmoButton { Text = "Permitir siempre", Variant = AmoButtonVariant.Primary, Left = 406, Top = 196, Width = 92, Height = 38 };

            block.Click += delegate { Choice = AmoPermissionChoice.BlockOnce; DialogResult = DialogResult.OK; Close(); };
            blockAlways.Click += delegate { Choice = AmoPermissionChoice.BlockAlways; DialogResult = DialogResult.OK; Close(); };
            allow.Click += delegate { Choice = AmoPermissionChoice.AllowOnce; DialogResult = DialogResult.OK; Close(); };
            allowAlways.Click += delegate { Choice = AmoPermissionChoice.AllowAlways; DialogResult = DialogResult.OK; Close(); };

            Controls.Add(block); Controls.Add(blockAlways); Controls.Add(allow); Controls.Add(allowAlways);
        }

        public static AmoPermissionChoice Show(IWin32Window owner, string host, string kind)
        {
            using (var f = new AmoPermissionPrompt(host, kind))
            {
                f.Choice = AmoPermissionChoice.BlockOnce;
                f.ShowDialog(owner);
                return f.Choice;
            }
        }

        static string Friendly(string kind)
        {
            switch (kind)
            {
                case "Microphone": return "micrófono";
                case "Camera": return "cámara";
                case "Geolocation": return "ubicación";
                case "Notifications": return "notificaciones";
                case "ClipboardRead": return "leer el portapapeles";
                case "MultipleAutomaticDownloads": return "múltiples descargas automáticas";
                case "Autoplay": return "reproducción automática";
                default: return kind;
            }
        }
    }
}

