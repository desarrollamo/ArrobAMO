using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DesarrollAMOBrowser
{
    public class BrowserForm : Form
    {
        readonly Color Ink = ColorTranslator.FromHtml("#111827");
        readonly Color Pink = ColorTranslator.FromHtml("#FF5AA5");
        readonly Color Sky = ColorTranslator.FromHtml("#7DD3FC");

        Panel bar = new Panel();
        Button back = new Button();
        Button forward = new Button();
        Button reload = new Button();
        Button home = new Button();
        Button plus = new Button();
        TextBox address = new TextBox();
        TabControl tabs = new TabControl();

        public BrowserForm()
        {
            Text = "DesarrollAMO Browser";
            Width = 1280;
            Height = 820;
            MinimumSize = new Size(800, 520);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ink;

            bar.Dock = DockStyle.Top;
            bar.Height = 50;
            bar.BackColor = Ink;
            Controls.Add(bar);

            AddButton(back, "←", 8);
            AddButton(forward, "→", 50);
            AddButton(reload, "↻", 92);
            AddButton(home, "⌂", 134);

            address.Left = 180;
            address.Top = 10;
            address.Height = 29;
            address.Width = ClientSize.Width - 236;
            address.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            address.Font = new Font("Segoe UI", 10f);
            address.KeyDown += Address_KeyDown;
            bar.Controls.Add(address);

            AddButton(plus, "+", ClientSize.Width - 48);
            plus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            tabs.Dock = DockStyle.Fill;
            tabs.Font = new Font("Segoe UI", 9f);
            tabs.SelectedIndexChanged += delegate { SyncActive(); };
            Controls.Add(tabs);
            tabs.BringToFront();

            back.Click += delegate { var w = Active(); if (w != null && w.CanGoBack) w.GoBack(); };
            forward.Click += delegate { var w = Active(); if (w != null && w.CanGoForward) w.GoForward(); };
            reload.Click += delegate { var w = Active(); if (w != null) w.Reload(); };
            home.Click += delegate { Navigate("https://desarrollamo.com.ar/"); };
            plus.Click += async delegate { await AddTab("https://desarrollamo.com.ar/"); };
            Shown += async delegate { await AddTab("https://desarrollamo.com.ar/"); };
        }

        void AddButton(Button b, string text, int left)
        {
            b.Text = text;
            b.Left = left;
            b.Top = 8;
            b.Width = 38;
            b.Height = 32;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Pink;
            b.BackColor = Ink;
            b.ForeColor = text == "+" ? Pink : Sky;
            b.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            bar.Controls.Add(b);
        }

        async Task AddTab(string url)
        {
            var page = new TabPage("Nueva pestaña");
            var web = new WebView2();
            web.Dock = DockStyle.Fill;
            page.Controls.Add(web);
            tabs.TabPages.Add(page);
            tabs.SelectedTab = page;

            string data = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesarrollAMO", "Browser", "WebView2");
            Directory.CreateDirectory(data);

            var env = await CoreWebView2Environment.CreateAsync(null, data, null);
            await web.EnsureCoreWebView2Async(env);
            web.CoreWebView2.Settings.IsStatusBarEnabled = false;

            web.CoreWebView2.NavigationStarting += delegate(object s, CoreWebView2NavigationStartingEventArgs e)
            {
                if (tabs.SelectedTab == page) address.Text = e.Uri;
            };
            web.CoreWebView2.SourceChanged += delegate
            {
                if (tabs.SelectedTab == page) address.Text = web.Source == null ? "" : web.Source.ToString();
            };
            web.CoreWebView2.DocumentTitleChanged += delegate
            {
                string title = web.CoreWebView2.DocumentTitle;
                page.Text = ShortTitle(title);
                if (tabs.SelectedTab == page) Text = title + " — DesarrollAMO Browser";
            };
            web.CoreWebView2.NewWindowRequested += async delegate(object s, CoreWebView2NewWindowRequestedEventArgs e)
            {
                e.Handled = true;
                await AddTab(e.Uri);
            };
            web.CoreWebView2.NavigationCompleted += delegate { if (tabs.SelectedTab == page) SyncActive(); };

            web.Source = new Uri(Normalize(url));
        }

        void Address_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            Navigate(address.Text);
        }

        void Navigate(string input)
        {
            var web = Active();
            if (web == null) return;
            web.Source = new Uri(Normalize(input));
        }

        string Normalize(string input)
        {
            input = (input ?? "").Trim();
            if (input.Length == 0) return "https://desarrollamo.com.ar/";
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
            var w = Active();
            if (w == null || w.CoreWebView2 == null) return;
            address.Text = w.Source == null ? "" : w.Source.ToString();
            back.Enabled = w.CanGoBack;
            forward.Enabled = w.CanGoForward;
            Text = w.CoreWebView2.DocumentTitle + " — DesarrollAMO Browser";
        }

        string ShortTitle(string s)
        {
            if (String.IsNullOrWhiteSpace(s)) return "Nueva pestaña";
            return s.Length <= 24 ? s : s.Substring(0, 23) + "…";
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BrowserForm());
        }
    }
}
