using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArrobAMO
{
    public sealed class OrbitMark : Control
    {
        public bool Inverted { get; set; }
        readonly Timer orbitTimer = new Timer();
        string voiceState = "idle";
        float phase;
        public string VoiceState
        {
            get { return voiceState; }
            set
            {
                string next = value == "listening" || value == "processing" || value == "speaking" ? value : "idle";
                if (voiceState == next) return;
                voiceState = next; orbitTimer.Enabled = next != "idle"; Invalidate();
            }
        }
        public OrbitMark()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            orbitTimer.Interval = 90;
            orbitTimer.Tick += delegate { phase = (phase + 3f) % 360f; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(Width, Height);
            float cx = Width / 2f, cy = Height / 2f;
            Color fg = voiceState == "listening" ? Color.FromArgb(109,207,246) :
                       voiceState == "processing" ? Color.FromArgb(241,155,222) :
                       voiceState == "speaking" ? Color.FromArgb(170,184,249) :
                       Inverted ? Color.White : AmoTheme.Text;
            Color core = Inverted ? Color.FromArgb(220,220,220) : Color.FromArgb(55,55,55);
            using (var pen = new Pen(fg, Math.Max(2f, s * .055f)))
            using (var brush = new SolidBrush(fg))
            using (var coreBrush = new SolidBrush(core))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                float r1 = s * .18f;
                e.Graphics.DrawEllipse(pen, cx-r1, cy-r1, r1*2, r1*2);
                float r2 = s * .34f;
                e.Graphics.DrawArc(pen, cx-r2, cy-r2, r2*2, r2*2, 205, 230);
                float r3 = s * .44f;
                e.Graphics.DrawArc(pen, cx-r3, cy-r3, r3*2, r3*2, 28, 125);
                e.Graphics.DrawArc(pen, cx-r3, cy-r3, r3*2, r3*2, 190, 118);
                e.Graphics.FillEllipse(coreBrush, cx-s*.075f, cy-s*.075f, s*.15f, s*.15f);
                DrawElectron(e.Graphics, brush, cx, cy, s*.39f, 334 + phase, s*.055f);
                DrawElectron(e.Graphics, brush, cx, cy, s*.43f, 99 + phase, s*.05f);
                DrawElectron(e.Graphics, brush, cx, cy, s*.40f, 156 + phase, s*.06f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) orbitTimer.Dispose();
            base.Dispose(disposing);
        }

        static void DrawElectron(Graphics g, Brush b, float cx, float cy, float radius, float deg, float dot)
        {
            double a = deg * Math.PI / 180.0;
            float x = cx + (float)Math.Cos(a) * radius;
            float y = cy + (float)Math.Sin(a) * radius;
            g.FillEllipse(b, x-dot, y-dot, dot*2, dot*2);
        }
    }

    public sealed class AmoSplashForm : Form
    {
        readonly AmoSpinner spinner = new AmoSpinner();
        readonly Label status = new Label();
        public AmoSplashForm()
        {
            Width = 560; Height = 340;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = AmoTheme.SurfaceDark;
            var mark = new OrbitMark { Left = 230, Top = 46, Width = 100, Height = 100, Inverted = true, BackColor = AmoTheme.SurfaceDark };
            Controls.Add(mark);
            var title = new Label { Text = "ArrobAMO", Left = 0, Top = 152, Width = Width, Height = 42, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, Font = AmoTheme.UI(25f, FontStyle.Bold) };
            Controls.Add(title);
            var by = new Label { Text = "by DesarrollAMO", Left = 0, Top = 194, Width = Width, Height = 24, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(185,185,185), Font = AmoTheme.UI(10f) };
            Controls.Add(by);
            spinner.Left = 268; spinner.Top = 230; spinner.Width = 24; spinner.Height = 24; spinner.ForeColor = Color.White; spinner.BackColor = AmoTheme.SurfaceDark;
            Controls.Add(spinner);
            status.Text = "Preparando tu centro operativo…"; status.Left = 0; status.Top = 266; status.Width = Width; status.Height = 24; status.TextAlign = ContentAlignment.MiddleCenter; status.ForeColor = Color.FromArgb(165,165,165); status.Font = AmoTheme.UI(8.5f);
            Controls.Add(status);
            var orbit = new Label { Text = "TODO ORBITA AQUÍ.", Left = 0, Top = 304, Width = Width, Height = 18, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(110,110,110), Font = AmoTheme.UI(7.5f, FontStyle.Bold) };
            Controls.Add(orbit);
        }
        public void SetStatus(string value) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetStatus), value); return; } status.Text = value; }
    }
}
