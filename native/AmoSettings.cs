using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ArrobAMO
{
    public sealed class AmoSettings
    {
        public string HomeUrl = "arrobamo://inicio";
        public bool ShowMetrics = true;
        public bool ExpandSidebarAtStart = true;
        public bool RestoreSession = true;
        public bool MicrophoneEnabled = true;
        public bool CameraEnabled = true;

        public static AmoSettings Load(string path)
        {
            var s = new AmoSettings();
            try
            {
                if (!File.Exists(path)) return s;
                foreach (string line in File.ReadAllLines(path))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string key = line.Substring(0,i).Trim();
                    string val = line.Substring(i+1).Trim();
                    if (key == "home") s.HomeUrl = String.IsNullOrWhiteSpace(val) ? "arrobamo://inicio" : val;
                    else if (key == "metrics") s.ShowMetrics = val == "1";
                    else if (key == "sidebar") s.ExpandSidebarAtStart = val == "1";
                    else if (key == "restore_session") s.RestoreSession = val != "0";
                    else if (key == "mic_enabled") s.MicrophoneEnabled = val != "0";
                    else if (key == "cam_enabled") s.CameraEnabled = val != "0";
                }
            }
            catch { }
            if (String.Equals(s.HomeUrl, "amo://inicio", StringComparison.OrdinalIgnoreCase))
                s.HomeUrl = "arrobamo://inicio";
            return s;
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, new[]
            {
                "home=" + (String.IsNullOrWhiteSpace(HomeUrl) ? "arrobamo://inicio" : HomeUrl),
                "metrics=" + (ShowMetrics ? "1" : "0"),
                "sidebar=" + (ExpandSidebarAtStart ? "1" : "0"),
                "restore_session=" + (RestoreSession ? "1" : "0"),
                "mic_enabled=" + (MicrophoneEnabled ? "1" : "0"),
                "cam_enabled=" + (CameraEnabled ? "1" : "0")
            });
        }
    }

    public sealed class AmoSettingsForm : Form
    {
        readonly TextBox home = new TextBox();
        readonly AmoSwitch metrics = new AmoSwitch();
        readonly AmoSwitch sidebar = new AmoSwitch();
        readonly AmoSwitch restore = new AmoSwitch();

        public bool Saved { get; private set; }
        public AmoSettings Value { get; private set; }

        public AmoSettingsForm(AmoSettings current)
        {
            Value = new AmoSettings
            {
                HomeUrl = current.HomeUrl,
                ShowMetrics = current.ShowMetrics,
                ExpandSidebarAtStart = current.ExpandSidebarAtStart,
                RestoreSession = current.RestoreSession,
                MicrophoneEnabled = current.MicrophoneEnabled,
                CameraEnabled = current.CameraEnabled
            };

            Text = "Configuraci\u00F3n \u00B7 ArrobAMO";
            ClientSize = new Size(640, 520);
            MinimumSize = new Size(640, 520);
            MaximumSize = new Size(640, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AmoTheme.Bg;
            ForeColor = AmoTheme.Text;
            Font = AmoTheme.UI(9f);

            var title = new Label
            {
                Text = "Configuraci\u00F3n",
                Left = 28, Top = 24, Width = 400, Height = 34,
                Font = AmoTheme.UI(18f, FontStyle.Bold),
                ForeColor = AmoTheme.Text
            };
            var sub = new Label
            {
                Text = "Preferencias locales del navegador.",
                Left = 29, Top = 61, Width = 450, Height = 22,
                ForeColor = AmoTheme.TextSoft
            };
            Controls.Add(title);
            Controls.Add(sub);

            var card = new AmoCard { Left = 28, Top = 102, Width = 564, Height = 274 };
            Controls.Add(card);

            AddLabel(card, "P\u00E1gina de inicio", 20, 18, true);
            home.Left = 20; home.Top = 48; home.Width = 522; home.Height = 34;
            home.Text = NormalizeInternal(current.HomeUrl);
            AmoTheme.StyleInput(home, false);
            card.Controls.Add(home);

            AddToggleRow(card, "Mostrar CPU / RAM / GPU", metrics, current.ShowMetrics, 104);
            AddToggleRow(card, "Sidebar expandida al iniciar", sidebar, current.ExpandSidebarAtStart, 154);
            AddToggleRow(card, "Restaurar pesta\u00F1as de la sesi\u00F3n anterior", restore, current.RestoreSession, 204);

            var reset = new AmoButton
            {
                Text = "Restablecer",
                Variant = AmoButtonVariant.Ghost,
                Left = 28, Top = 402, Width = 118, Height = 38
            };
            var cancel = new AmoButton
            {
                Text = "Cancelar",
                Variant = AmoButtonVariant.Secondary,
                Left = 378, Top = 402, Width = 98, Height = 38,
                DialogResult = DialogResult.Cancel
            };
            var save = new AmoButton
            {
                Text = "Guardar",
                Variant = AmoButtonVariant.Primary,
                Left = 488, Top = 402, Width = 104, Height = 38
            };

            Controls.Add(reset);
            Controls.Add(cancel);
            Controls.Add(save);
            CancelButton = cancel;

            reset.Click += delegate
            {
                home.Text = "arrobamo://inicio";
                metrics.Checked = true;
                sidebar.Checked = true;
                restore.Checked = true;
            };

            save.Click += delegate
            {
                Value.HomeUrl = String.IsNullOrWhiteSpace(home.Text) ? "arrobamo://inicio" : NormalizeInternal(home.Text.Trim());
                Value.ShowMetrics = metrics.Checked;
                Value.ExpandSidebarAtStart = sidebar.Checked;
                Value.RestoreSession = restore.Checked;
                Saved = true;
                DialogResult = DialogResult.OK;
                Close();
            };
        }

        static void AddLabel(Control parent, string text, int left, int top, bool bold)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Left = left, Top = top, Width = 360, Height = 22,
                Font = AmoTheme.UI(9.5f, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = AmoTheme.Text
            });
        }

        static void AddToggleRow(Control parent, string text, AmoSwitch toggle, bool value, int top)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Left = 20, Top = top + 3, Width = 410, Height = 24,
                ForeColor = AmoTheme.Text
            });
            toggle.Left = 492; toggle.Top = top; toggle.Checked = value;
            parent.Controls.Add(toggle);
        }

        static string NormalizeInternal(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "arrobamo://inicio";
            if (value.StartsWith("amo://", StringComparison.OrdinalIgnoreCase))
                return "arrobamo://" + value.Substring(6);
            return value;
        }
    }
}

