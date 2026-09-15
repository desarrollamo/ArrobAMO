using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArrobAMO
{
    public static class AmoTheme
    {
        public static readonly Color Bg = Color.FromArgb(247,247,245);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceSoft = Color.FromArgb(241,241,239);
        public static readonly Color SurfaceHover = Color.FromArgb(233,233,230);
        public static readonly Color SurfaceDark = Color.FromArgb(20,20,20);
        public static readonly Color SurfaceDark2 = Color.FromArgb(29,29,31);
        public static readonly Color SurfaceDark3 = Color.FromArgb(39,39,42);

        public static readonly Color Text = Color.FromArgb(17,17,17);
        public static readonly Color TextSoft = Color.FromArgb(95,95,95);
        public static readonly Color TextMuted = Color.FromArgb(140,140,140);
        public static readonly Color TextInverse = Color.White;

        public static readonly Color Border = Color.FromArgb(221,221,218);
        public static readonly Color BorderStrong = Color.FromArgb(188,188,183);

        public static readonly Color Success = Color.FromArgb(24,165,88);
        public static readonly Color SuccessSoft = Color.FromArgb(234,248,240);
        public static readonly Color Danger = Color.FromArgb(217,58,58);
        public static readonly Color DangerSoft = Color.FromArgb(253,236,236);
        public static readonly Color Warning = Color.FromArgb(217,146,22);
        public static readonly Color WarningSoft = Color.FromArgb(255,244,221);
        public static readonly Color Info = Color.FromArgb(47,111,237);
        public static readonly Color InfoSoft = Color.FromArgb(237,243,255);

        public const int RadiusSmall = 8;
        public const int RadiusMedium = 12;
        public const int RadiusLarge = 16;
        public const int ClickTarget = 36;

        public static Font UI(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", size, style);
        }

        public static Font Mono(float size)
        {
            return new Font("Consolas", size, FontStyle.Regular);
        }

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = Math.Max(2, radius * 2);
            var p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void StyleMenu(ContextMenuStrip menu)
        {
            menu.BackColor = Surface;
            menu.ForeColor = Text;
            menu.Font = UI(9f);
            menu.ShowImageMargin = false;
            menu.Padding = new Padding(6);
        }

        public static void StyleInput(TextBox box, bool dark)
        {
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Font = UI(10f);
            box.BackColor = dark ? SurfaceDark3 : Surface;
            box.ForeColor = dark ? TextInverse : Text;
        }
    }

    public enum AmoButtonVariant
    {
        Primary,
        Secondary,
        Outline,
        Ghost,
        Danger
    }

    public sealed class AmoButton : Button
    {
        public AmoButtonVariant Variant { get; set; }
        public int CornerRadius { get; set; }

        bool hovering;
        bool pressed;

        public AmoButton()
        {
            Variant = AmoButtonVariant.Secondary;
            CornerRadius = AmoTheme.RadiusSmall;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = AmoTheme.UI(9f, FontStyle.Bold);
            Height = AmoTheme.ClickTarget;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovering = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            Color bg = Color.Transparent;
            Color fg = AmoTheme.Text;
            Color border = Color.Transparent;

            if (!Enabled)
            {
                bg = Color.FromArgb(220,220,220);
                fg = Color.FromArgb(150,150,150);
            }
            else
            {
                switch (Variant)
                {
                    case AmoButtonVariant.Primary:
                        bg = pressed ? Color.FromArgb(42,42,42) : hovering ? Color.FromArgb(28,28,28) : AmoTheme.Text;
                        fg = Color.White;
                        break;
                    case AmoButtonVariant.Secondary:
                        bg = pressed ? Color.FromArgb(220,220,218) : hovering ? AmoTheme.SurfaceHover : AmoTheme.SurfaceSoft;
                        fg = AmoTheme.Text;
                        break;
                    case AmoButtonVariant.Outline:
                        bg = hovering ? AmoTheme.SurfaceSoft : AmoTheme.Surface;
                        fg = AmoTheme.Text;
                        border = AmoTheme.BorderStrong;
                        break;
                    case AmoButtonVariant.Ghost:
                        bg = hovering ? AmoTheme.SurfaceHover : Color.Transparent;
                        fg = AmoTheme.Text;
                        break;
                    case AmoButtonVariant.Danger:
                        bg = pressed ? Color.FromArgb(184,44,44) : hovering ? Color.FromArgb(205,48,48) : AmoTheme.Danger;
                        fg = Color.White;
                        break;
                }
            }

            using (var path = AmoTheme.RoundRect(rect, CornerRadius))
            {
                using (var b = new SolidBrush(bg)) e.Graphics.FillPath(b, path);
                if (border != Color.Transparent)
                {
                    using (var p = new Pen(border, 1f)) e.Graphics.DrawPath(p, path);
                }
            }

            TextRenderer.DrawText(e.Graphics, Text, Font, rect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (Focused && Enabled)
            {
                var focus = new Rectangle(3, 3, Width - 7, Height - 7);
                using (var p = new Pen(AmoTheme.Info, 1f)) e.Graphics.DrawRectangle(p, focus);
            }
        }
    }

    public sealed class AmoBadge : Label
    {
        public Color BadgeColor { get; set; }

        public AmoBadge()
        {
            BadgeColor = AmoTheme.SurfaceSoft;
            AutoSize = false;
            Height = 24;
            TextAlign = ContentAlignment.MiddleCenter;
            Font = AmoTheme.UI(8.5f, FontStyle.Bold);
            Padding = new Padding(8, 0, 8, 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = AmoTheme.RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 12))
            using (var b = new SolidBrush(BadgeColor))
                e.Graphics.FillPath(b, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public sealed class AmoSpinner : Control
    {
        readonly Timer timer = new Timer();
        int angle;

        public AmoSpinner()
        {
            Size = new Size(24,24);
            timer.Interval = 90;
            timer.Tick += delegate { angle = (angle + 30) % 360; Invalidate(); };
            VisibleChanged += delegate { if (Visible) timer.Start(); else timer.Stop(); };
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int pad = Math.Max(2, Width / 7);
            var r = new Rectangle(pad, pad, Width - pad * 2 - 1, Height - pad * 2 - 1);
            using (var p = new Pen(ForeColor, Math.Max(2f, Width / 10f)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                e.Graphics.DrawArc(p, r, angle, 250);
            }
            int dot = Math.Max(4, Width / 5);
            double rad = (angle + 250) * Math.PI / 180.0;
            int cx = r.Left + r.Width / 2 + (int)(Math.Cos(rad) * r.Width / 2);
            int cy = r.Top + r.Height / 2 + (int)(Math.Sin(rad) * r.Height / 2);
            using (var b = new SolidBrush(ForeColor))
                e.Graphics.FillEllipse(b, cx - dot / 2, cy - dot / 2, dot, dot);
        }
    }

    public sealed class AmoProgress : Control
    {
        int value;
        public int Value
        {
            get { return value; }
            set { this.value = Math.Max(0, Math.Min(100, value)); Invalidate(); }
        }

        public AmoProgress()
        {
            Height = 3;
            BackColor = Color.Transparent;
            ForeColor = AmoTheme.Text;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var bg = new SolidBrush(Color.FromArgb(75,75,78)))
                e.Graphics.FillRectangle(bg, ClientRectangle);
            if (value > 0)
            {
                int w = Math.Max(1, Width * value / 100);
                using (var fg = new SolidBrush(ForeColor))
                    e.Graphics.FillRectangle(fg, new Rectangle(0,0,w,Height));
            }
        }
    }

    public sealed class AmoToastForm : Form
    {
        readonly Timer timer = new Timer();

        public AmoToastForm(string message, Color accent, int durationMs)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = AmoTheme.SurfaceDark2;
            ForeColor = Color.White;
            Width = 360;
            Height = 72;
            Padding = new Padding(14);

            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent };
            var label = new Label { Dock = DockStyle.Fill, Text = message, ForeColor = Color.White, Font = AmoTheme.UI(9f), TextAlign = ContentAlignment.MiddleLeft };
            var close = new Button { Dock = DockStyle.Right, Width = 34, Text = "×", FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = AmoTheme.SurfaceDark2 };
            close.FlatAppearance.BorderSize = 0;
            close.Click += delegate { Close(); };

            Controls.Add(label);
            Controls.Add(close);
            Controls.Add(bar);

            timer.Interval = Math.Max(1000, durationMs);
            timer.Tick += delegate { timer.Stop(); Close(); };
            Shown += delegate
            {
                var wa = Screen.FromControl(this).WorkingArea;
                Location = new Point(wa.Right - Width - 20, wa.Bottom - Height - 20);
                timer.Start();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }
}

