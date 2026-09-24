using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace ArrobAMO
{
    public sealed class AmoBrowserSource
    {
        public string Name;
        public string BookmarksPath;
        public override string ToString() { return Name; }
    }

    public static class AmoBrowserImporter
    {
        public static List<AmoBrowserSource> Detect()
        {
            var result = new List<AmoBrowserSource>();
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            AddChromiumProfiles(result, "Chrome", Path.Combine(local,"Google","Chrome","User Data"));
            AddChromiumProfiles(result, "Edge", Path.Combine(local,"Microsoft","Edge","User Data"));
            AddChromiumProfiles(result, "Brave", Path.Combine(local,"BraveSoftware","Brave-Browser","User Data"));
            AddSingle(result, "Opera", Path.Combine(roaming,"Opera Software","Opera Stable","Bookmarks"));
            AddSingle(result, "Opera GX", Path.Combine(roaming,"Opera Software","Opera GX Stable","Bookmarks"));
            return result;
        }

        static void AddChromiumProfiles(List<AmoBrowserSource> result, string name, string userData)
        {
            if (!Directory.Exists(userData)) return;
            foreach (string dir in Directory.GetDirectories(userData))
            {
                string profile = Path.GetFileName(dir);
                if (profile != "Default" && !profile.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)) continue;
                string file = Path.Combine(dir,"Bookmarks");
                if (File.Exists(file)) result.Add(new AmoBrowserSource { Name = name + " · " + profile, BookmarksPath = file });
            }
        }

        static void AddSingle(List<AmoBrowserSource> result, string name, string file)
        {
            if (File.Exists(file)) result.Add(new AmoBrowserSource { Name=name, BookmarksPath=file });
        }

        public static List<string> ImportBookmarks(IEnumerable<AmoBrowserSource> sources)
        {
            var output = new List<string>();
            foreach (var src in sources)
            {
                try
                {
                    string json = File.ReadAllText(src.BookmarksPath);
                    object root = new JavaScriptSerializer().DeserializeObject(json);
                    Walk(root, output);
                }
                catch { }
            }
            return output.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        static void Walk(object node, List<string> output)
        {
            var dict = node as Dictionary<string,object>;
            if (dict != null)
            {
                object type;
                if (dict.TryGetValue("type", out type) && Convert.ToString(type) == "url")
                {
                    string name = dict.ContainsKey("name") ? Convert.ToString(dict["name"]) : "";
                    string url = dict.ContainsKey("url") ? Convert.ToString(dict["url"]) : "";
                    if (!String.IsNullOrWhiteSpace(url))
                    {
                        if (String.IsNullOrWhiteSpace(name)) name = url;
                        output.Add(name.Replace("|"," ") + "|" + url);
                    }
                }

                foreach (var value in dict.Values) Walk(value, output);
                return;
            }

            var arr = node as object[];
            if (arr != null)
            {
                foreach (object item in arr) Walk(item, output);
                return;
            }

            var list = node as ArrayList;
            if (list != null) foreach (object item in list) Walk(item, output);
        }
    }

    public sealed class AmoImportForm : Form
    {
        readonly CheckedListBox browsers = new CheckedListBox();
        readonly AmoButton import = new AmoButton();
        readonly List<AmoBrowserSource> sources;
        public List<string> Imported { get; private set; }

        public AmoImportForm()
        {
            sources = AmoBrowserImporter.Detect();
            Imported = new List<string>();

            Text = "Importar datos · ArrobAMO";
            Width = 720; Height = 590;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = AmoTheme.Bg; ForeColor = AmoTheme.Text; Font = AmoTheme.UI(9f);

            Controls.Add(new Label { Text="Importar datos",Left=24,Top=18,Width=400,Height=34,Font=AmoTheme.UI(18f,FontStyle.Bold),ForeColor=AmoTheme.Text });
            Controls.Add(new Label { Text="Migración segura desde navegadores detectados en esta PC.",Left=25,Top=56,Width=560,Height=22,ForeColor=AmoTheme.TextSoft });

            var sourceCard = new AmoCard { Left=24,Top=94,Width=650,Height=220 };
            sourceCard.Controls.Add(new Label { Text="Navegadores detectados",Left=16,Top=14,Width=260,Height=24,Font=AmoTheme.UI(10f,FontStyle.Bold),ForeColor=AmoTheme.Text });
            browsers.Left=16; browsers.Top=46; browsers.Width=610; browsers.Height=150; browsers.CheckOnClick=true;
            foreach (var src in sources) browsers.Items.Add(src, true);
            if (sources.Count==0) browsers.Items.Add("No se encontraron perfiles Chromium compatibles.", false);
            sourceCard.Controls.Add(browsers);
            Controls.Add(sourceCard);

            var dataCard = new AmoCard { Left=24,Top=330,Width=650,Height=150 };
            dataCard.Controls.Add(new Label { Text="Datos",Left=16,Top=14,Width=160,Height=24,Font=AmoTheme.UI(10f,FontStyle.Bold),ForeColor=AmoTheme.Text });
            dataCard.Controls.Add(new CheckBox { Text="Favoritos / marcadores",Left=16,Top=48,Width=200,Checked=true,Enabled=true,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.Text });
            dataCard.Controls.Add(new CheckBox { Text="Historial · próximamente",Left=230,Top=48,Width=200,Checked=false,Enabled=false,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.TextMuted });
            dataCard.Controls.Add(new CheckBox { Text="Contraseñas · próximamente",Left=430,Top=48,Width=190,Checked=false,Enabled=false,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.TextMuted });
            dataCard.Controls.Add(new CheckBox { Text="Autocompletado · próximamente",Left=16,Top=84,Width=220,Checked=false,Enabled=false,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.TextMuted });
            dataCard.Controls.Add(new CheckBox { Text="Pestañas recientes · próximamente",Left=230,Top=84,Width=230,Checked=false,Enabled=false,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.TextMuted });
            Controls.Add(dataCard);

            var cancel = new AmoButton { Text="Cancelar",Variant=AmoButtonVariant.Secondary,Left=468,Top=504,Width=96,DialogResult=DialogResult.Cancel };
            import.Text="Importar favoritos"; import.Variant=AmoButtonVariant.Primary; import.Left=574; import.Top=504; import.Width=100; import.Enabled=sources.Count>0;
            Controls.Add(cancel); Controls.Add(import); CancelButton=cancel;

            import.Click += delegate
            {
                var selected = new List<AmoBrowserSource>();
                foreach (int i in browsers.CheckedIndices)
                {
                    if (i >= 0 && i < sources.Count) selected.Add(sources[i]);
                }
                if (selected.Count == 0) return;
                import.Enabled=false; import.Text="Importando…";
                try
                {
                    Imported=AmoBrowserImporter.ImportBookmarks(selected);
                    DialogResult=DialogResult.OK;
                    Close();
                }
                finally
                {
                    import.Enabled=true; import.Text="Importar favoritos";
                }
            };
        }
    }
}

