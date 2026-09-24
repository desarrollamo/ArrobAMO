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
            e.Graphics.Clear(Parent != null ? Parent.BackColor : AmoTheme.Bg);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(2, 2, Math.Max(1, Width - 5), Math.Max(1, Height - 5));

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
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

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
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            ForeColor = AmoTheme.Text;
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


namespace ArrobAMO
{
    public sealed class AmoLoadingDots : Control
    {
        readonly Timer timer = new Timer();
        int frame;
        public AmoLoadingDots()
        {
            Width = 54; Height = 18; ForeColor = AmoTheme.Text;
            timer.Interval = 220;
            timer.Tick += delegate { frame = (frame + 1) % 3; Invalidate(); };
            VisibleChanged += delegate { if (Visible) timer.Start(); else timer.Stop(); };
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            for (int i=0;i<3;i++)
            {
                int a = i == frame ? 255 : 100;
                using (var b = new SolidBrush(Color.FromArgb(a, ForeColor))) e.Graphics.FillEllipse(b, 3+i*17, 5, 8, 8);
            }
        }
    }

    public sealed class AmoSwitch : Control
    {
        bool state;
        public bool Checked { get { return state; } set { state = value; Invalidate(); } }
        public event EventHandler CheckedChanged;
        public AmoSwitch() { Width=44; Height=24; Cursor=Cursors.Hand; SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer,true); }
        protected override void OnClick(EventArgs e) { state=!state; Invalidate(); if(CheckedChanged!=null) CheckedChanged(this,EventArgs.Empty); base.OnClick(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var r = new Rectangle(0,1,Width-1,Height-3);
            using(var path=AmoTheme.RoundRect(r,Height/2)) using(var b=new SolidBrush(state?AmoTheme.Text:Color.FromArgb(205,205,205))) e.Graphics.FillPath(b,path);
            int d=18; int x=state?Width-d-3:3;
            using(var b=new SolidBrush(Color.White)) e.Graphics.FillEllipse(b,x,(Height-d)/2,d,d);
        }
    }
}


namespace ArrobAMO
{
    public sealed class AmoChip : Label
    {
        public AmoChip()
        {
            AutoSize=false; Height=28; Width=110; TextAlign=ContentAlignment.MiddleCenter;
            BackColor=AmoTheme.SurfaceSoft; ForeColor=AmoTheme.Text; Font=AmoTheme.UI(8.5f);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using(var path=AmoTheme.RoundRect(new Rectangle(0,0,Width-1,Height-1),14))
            using(var b=new SolidBrush(BackColor)) e.Graphics.FillPath(b,path);
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        }
    }

    public sealed class AmoCard : Panel
    {
        public AmoCard()
        {
            BackColor=AmoTheme.Surface; Padding=new Padding(16);
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using(var p=new Pen(AmoTheme.Border,1f))
            using(var path=AmoTheme.RoundRect(new Rectangle(0,0,Width-1,Height-1),AmoTheme.RadiusMedium)) e.Graphics.DrawPath(p,path);
        }
    }

    public sealed class AmoSkeleton : Control
    {
        readonly Timer timer=new Timer(); int phase;
        public AmoSkeleton()
        {
            BackColor=AmoTheme.SurfaceSoft; Height=16;
            timer.Interval=90; timer.Tick+=delegate{phase=(phase+7)%100;Invalidate();}; timer.Start();
            SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);
        }
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
        protected override void OnPaint(PaintEventArgs e)
        {
            using(var bg=new SolidBrush(AmoTheme.SurfaceSoft)) e.Graphics.FillRectangle(bg,ClientRectangle);
            int w=Math.Max(20,Width/4); int x=(Width+w)*phase/100-w;
            using(var b=new SolidBrush(Color.FromArgb(28,255,255,255))) e.Graphics.FillRectangle(b,x,0,w,Height);
        }
    }
}


namespace ArrobAMO
{
    public sealed class AmoModalForm : Form
    {
        public AmoModalForm(string title,string message,string confirmText,bool danger)
        {
            Text=title; Width=470; Height=230; StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; MinimizeBox=false;
            BackColor=AmoTheme.Surface; ForeColor=AmoTheme.Text; KeyPreview=true;
            var h=new Label{Text=title,Left=24,Top=22,Width=400,Height=30,Font=AmoTheme.UI(15f,FontStyle.Bold),ForeColor=AmoTheme.Text};
            var m=new Label{Text=message,Left=24,Top=64,Width=410,Height=62,Font=AmoTheme.UI(9.5f),ForeColor=AmoTheme.TextSoft};
            var cancel=new AmoButton{Text="Cancelar",Variant=AmoButtonVariant.Secondary,Left=238,Top=144,Width=90,DialogResult=DialogResult.Cancel};
            var ok=new AmoButton{Text=confirmText,Variant=danger?AmoButtonVariant.Danger:AmoButtonVariant.Primary,Left=338,Top=144,Width=96,DialogResult=DialogResult.OK};
            Controls.Add(h);Controls.Add(m);Controls.Add(cancel);Controls.Add(ok);
            AcceptButton=ok;CancelButton=cancel;
        }
    }
}

