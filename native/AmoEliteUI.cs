using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ArrobAMO
{
    public sealed class AmoEliteCenterForm : Form
    {
        readonly AmoEnterpriseStore store;
        readonly Action<string,Color> notify;
        readonly ListView users = new ListView();
        readonly ListView teams = new ListView();
        readonly ListView workspaces = new ListView();
        readonly ListView versions = new ListView();
        readonly ListView activity = new ListView();
        readonly Label orgName = new Label();
        readonly Label mode = new Label();
        readonly Label counts = new Label();
        readonly AmoButton kill = new AmoButton();

        public AmoEliteCenterForm(AmoEnterpriseStore enterpriseStore, Action<string,Color> notifier)
        {
            store=enterpriseStore;notify=notifier;

            Text="Equipo \u00B7 ArrobAMO Elite";
            Width=1040;Height=700;
            StartPosition=FormStartPosition.CenterParent;
            BackColor=AmoTheme.Bg;ForeColor=AmoTheme.Text;Font=AmoTheme.UI(9f);

            Controls.Add(new Label{Text="Equipo",Left=24,Top=18,Width=300,Height=34,Font=AmoTheme.UI(18f,FontStyle.Bold),ForeColor=AmoTheme.Text});
            Controls.Add(new Label{Text="Organizaci\u00F3n, equipos, permisos, workspaces compartidos y actividad.",Left=25,Top=55,Width=690,Height=22,ForeColor=AmoTheme.TextSoft});

            mode.Left=760;mode.Top=22;mode.Width=240;mode.Height=24;mode.TextAlign=ContentAlignment.MiddleRight;mode.ForeColor=AmoTheme.Warning;
            Controls.Add(mode);

            var header=new AmoCard{Left=24,Top=92,Width=976,Height=92};
            orgName.Left=18;orgName.Top=14;orgName.Width=520;orgName.Height=30;orgName.Font=AmoTheme.UI(15f,FontStyle.Bold);orgName.ForeColor=AmoTheme.Text;
            counts.Left=18;counts.Top=50;counts.Width=600;counts.Height=22;counts.ForeColor=AmoTheme.TextSoft;
            header.Controls.Add(orgName);header.Controls.Add(counts);

            var createOrg=new AmoButton{Text="Crear organizaci\u00F3n local",Variant=AmoButtonVariant.Primary,Left=720,Top=26,Width=220,Height=38};
            createOrg.Click+=delegate{CreateOrganization();};
            header.Controls.Add(createOrg);
            Controls.Add(header);

            var tabs=new TabControl{Left=24,Top=202,Width=976,Height=390};
            tabs.Font=AmoTheme.UI(9f);
            tabs.TabPages.Add(BuildUsersPage());
            tabs.TabPages.Add(BuildTeamsPage());
            tabs.TabPages.Add(BuildWorkspacesPage());
            tabs.TabPages.Add(BuildRolesPage());
            tabs.TabPages.Add(BuildVersionsPage());
            tabs.TabPages.Add(BuildApprovalsPage());
            tabs.TabPages.Add(BuildActivityPage());
            tabs.TabPages.Add(BuildAgentsPage());
            Controls.Add(tabs);

            var note=new Label
            {
                Text="Estado: Elite local-first. Backend colaborativo, presencia remota, SSO/SCIM, E2EE y administraci\u00F3n central todav\u00EDa no est\u00E1n configurados.",
                Left=24,Top=607,Width=760,Height=44,ForeColor=AmoTheme.TextSoft
            };
            Controls.Add(note);

            kill.Text="DETENER AGENTES";kill.Variant=AmoButtonVariant.Danger;kill.Left=814;kill.Top=610;kill.Width=186;kill.Height=38;
            kill.Click+=delegate{ToggleKillSwitch();};
            Controls.Add(kill);

            RefreshAll();
        }

        TabPage Page(string text)
        {
            return new TabPage{Text=text,BackColor=AmoTheme.Bg,ForeColor=AmoTheme.Text};
        }

        TabPage BuildUsersPage()
        {
            var p=Page("Usuarios");
            SetupList(users,new[]{"Nombre","Email","Tipo","Roles"},new[]{250,270,120,250});
            users.Left=16;users.Top=16;users.Width=920;users.Height=285;p.Controls.Add(users);
            var add=new AmoButton{Text="+ Usuario",Variant=AmoButtonVariant.Primary,Left=16,Top=316,Width=110,Height=36};
            add.Click+=delegate{AddUser();};p.Controls.Add(add);
            return p;
        }

        TabPage BuildTeamsPage()
        {
            var p=Page("Equipos");
            SetupList(teams,new[]{"Equipo","Responsable","Miembros"},new[]{300,280,160});
            teams.Left=16;teams.Top=16;teams.Width=920;teams.Height=285;p.Controls.Add(teams);
            var add=new AmoButton{Text="+ Equipo",Variant=AmoButtonVariant.Primary,Left=16,Top=316,Width=110,Height=36};
            add.Click+=delegate{AddTeam();};p.Controls.Add(add);
            return p;
        }

        TabPage BuildWorkspacesPage()
        {
            var p=Page("Workspaces");
            SetupList(workspaces,new[]{"Workspace","\u00C1mbito","Equipo","Miembros"},new[]{300,160,250,120});
            workspaces.Left=16;workspaces.Top=16;workspaces.Width=920;workspaces.Height=285;p.Controls.Add(workspaces);
            var add=new AmoButton{Text="+ Workspace",Variant=AmoButtonVariant.Primary,Left=16,Top=316,Width=130,Height=36};
            add.Click+=delegate{AddWorkspace();};p.Controls.Add(add);
            return p;
        }

        TabPage BuildRolesPage()
        {
            var p=Page("Roles y permisos");
            var list=new ListView{Left=16,Top=16,Width=920,Height=330,View=View.Details,FullRowSelect=true,BorderStyle=BorderStyle.FixedSingle,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.Text};
            list.Columns.Add("Rol",220);list.Columns.Add("Tipo",110);list.Columns.Add("Permisos",560);
            p.Tag=list;p.Controls.Add(list);
            var add=new AmoButton{Text="+ Rol personalizado",Variant=AmoButtonVariant.Primary,Left=16,Top=316,Width=170,Height=36};
            add.Click+=delegate{AddCustomRole();};p.Controls.Add(add);
            return p;
        }

        TabPage BuildVersionsPage()
        {
            var p=Page("Versiones");
            SetupList(versions,new[]{"Recurso","Versi\u00F3n","Autor","Fecha","Cambio"},new[]{190,100,180,140,270});
            versions.Left=16;versions.Top=16;versions.Width=920;versions.Height=330;p.Controls.Add(versions);
            return p;
        }

        TabPage BuildApprovalsPage()
        {
            var p=Page("Aprobaciones");
            var list=new ListView{Left=16,Top=16,Width=920,Height=285,View=View.Details,FullRowSelect=true,BorderStyle=BorderStyle.FixedSingle,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.Text};
            list.Columns.Add("Recurso",210);list.Columns.Add("Estado",140);list.Columns.Add("Motivo",390);list.Columns.Add("Actualizado",150);
            p.Tag=list;p.Controls.Add(list);
            var approve=new AmoButton{Text="Aprobar",Variant=AmoButtonVariant.Primary,Left=16,Top=316,Width=100,Height=36};
            var reject=new AmoButton{Text="Rechazar",Variant=AmoButtonVariant.Danger,Left=126,Top=316,Width=100,Height=36};
            approve.Click+=delegate{DecideSelected(list,true);};reject.Click+=delegate{DecideSelected(list,false);};
            p.Controls.Add(approve);p.Controls.Add(reject);return p;
        }

        TabPage BuildActivityPage()
        {
            var p=Page("Actividad");
            SetupList(activity,new[]{"Hora","Actor","Acci\u00F3n","Recurso","Resultado"},new[]{150,140,260,220,120});
            activity.Left=16;activity.Top=16;activity.Width=920;activity.Height=330;p.Controls.Add(activity);return p;
        }

        TabPage BuildAgentsPage()
        {
            var p=Page("Agentes");
            var list=new ListView{Left=16,Top=16,Width=920,Height=285,View=View.Details,FullRowSelect=true,BorderStyle=BorderStyle.FixedSingle,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.Text};
            list.Columns.Add("Agente",230);list.Columns.Add("Autonom\u00EDa",180);list.Columns.Add("M\u00E1x. acciones",130);list.Columns.Add("M\u00E1x. min",120);list.Columns.Add("Estado",120);
            p.Tag=list;p.Controls.Add(list);
            p.Controls.Add(new Label{Text="Los agentes nacen en Nivel 0. Esta pantalla no concede autonom\u00EDa ni credenciales por s\u00ED sola.",Left=16,Top=316,Width=720,Height=28,ForeColor=AmoTheme.TextSoft});
            return p;
        }

        void SetupList(ListView list,string[] names,int[] widths)
        {
            list.View=View.Details;list.FullRowSelect=true;list.BorderStyle=BorderStyle.FixedSingle;list.BackColor=AmoTheme.Surface;list.ForeColor=AmoTheme.Text;
            for(int i=0;i<names.Length;i++)list.Columns.Add(names[i],widths[i]);
        }

        AmoOrganization Org(){return store.ActiveOrganization();}

        void RefreshAll()
        {
            var org=Org();
            mode.Text="Modo local de desarrollo \u00B7 sin backend Enterprise";
            orgName.Text=org==null?"Sin organizaci\u00F3n local":org.Name+" \u00B7 "+org.Plan;
            counts.Text=org==null?"Cre\u00E1 una organizaci\u00F3n local para probar Elite 1.":
                org.Users.Count+" usuarios \u00B7 "+org.Teams.Count+" equipos \u00B7 "+org.Workspaces.Count+" workspaces \u00B7 "+org.Approvals.Count+" aprobaciones";

            users.Items.Clear();teams.Items.Clear();workspaces.Items.Clear();versions.Items.Clear();activity.Items.Clear();
            if(org==null){kill.Enabled=false;RefreshTaggedLists(null);return;}

            kill.Enabled=true;kill.Text=org.AgentKillSwitch?"REANUDAR AGENTES":"DETENER AGENTES";

            foreach(var u in org.Users)
            {
                var item=new ListViewItem(u.DisplayName);item.SubItems.Add(u.Email);item.SubItems.Add(u.External?"Invitado externo":"Miembro");item.SubItems.Add(String.Join(", ",u.RoleIds.ToArray()));item.Tag=u.Id;users.Items.Add(item);
            }

            foreach(var t in org.Teams)
            {
                string responsible=UserName(org,t.ResponsibleUserId);
                var item=new ListViewItem(t.Name);item.SubItems.Add(responsible);item.SubItems.Add(t.MemberUserIds.Count.ToString());teams.Items.Add(item);
            }

            foreach(var w in org.Workspaces)
            {
                var team=org.Teams.FirstOrDefault(delegate(AmoTeam t){return t.Id==w.TeamId;});
                var item=new ListViewItem(w.Name);item.SubItems.Add(ScopeName(w.Scope));item.SubItems.Add(team==null?"\u2014":team.Name);item.SubItems.Add(w.MemberUserIds.Count.ToString());workspaces.Items.Add(item);
            }

            foreach(var v in org.Versions.OrderByDescending(delegate(AmoVersionRecord x){return x.CreatedUtc;}).Take(200))
            {
                var item=new ListViewItem(v.ResourceType+" \u00B7 "+v.ResourceId);item.SubItems.Add(v.Version);item.SubItems.Add(UserName(org,v.AuthorUserId));item.SubItems.Add(v.CreatedUtc.ToLocalTime().ToString("dd/MM HH:mm"));item.SubItems.Add(v.ChangeSummary);versions.Items.Add(item);
            }

            foreach(var a in org.Activity.OrderByDescending(delegate(AmoActivity x){return x.CreatedUtc;}).Take(200))
            {
                var item=new ListViewItem(a.CreatedUtc.ToLocalTime().ToString("dd/MM HH:mm"));
                item.SubItems.Add(UserName(org,a.ActorId));item.SubItems.Add(a.Action);item.SubItems.Add(a.ResourceType);item.SubItems.Add(ResultName(a.Result));activity.Items.Add(item);
            }

            RefreshTaggedLists(org);
        }

        void RefreshTaggedLists(AmoOrganization org)
        {
            foreach(Control c in Controls)
            {
                var tabs=c as TabControl;if(tabs==null)continue;
                foreach(TabPage p in tabs.TabPages)
                {
                    var list=p.Tag as ListView;if(list==null)continue;
                    list.Items.Clear();
                    if(org==null)continue;

                    if(p.Text.StartsWith("Roles"))
                    {
                        foreach(var r in org.Roles)
                        {
                            var i=new ListViewItem(r.Name);i.SubItems.Add(r.BuiltIn?"Base":"Personalizado");i.SubItems.Add(String.Join(", ",r.Permissions.ToArray()));list.Items.Add(i);
                        }
                    }
                    else if(p.Text=="Aprobaciones")
                    {
                        foreach(var a in org.Approvals.OrderByDescending(delegate(AmoApproval x){return x.UpdatedUtc;}))
                        {
                            var i=new ListViewItem(a.ResourceType+" \u00B7 "+a.ResourceId);i.SubItems.Add(ApprovalName(a.State));i.SubItems.Add(a.Reason);i.SubItems.Add(a.UpdatedUtc.ToLocalTime().ToString("dd/MM HH:mm"));i.Tag=a.Id;list.Items.Add(i);
                        }
                    }
                    else if(p.Text=="Agentes")
                    {
                        foreach(var a in org.Agents)
                        {
                            var i=new ListViewItem(a.Name);i.SubItems.Add(((int)a.Autonomy)+" \u00B7 "+a.Autonomy);i.SubItems.Add(a.MaxActions.ToString());i.SubItems.Add(a.MaxMinutes.ToString());i.SubItems.Add(a.Enabled?"Habilitado":"Deshabilitado");list.Items.Add(i);
                        }
                    }
                }
            }
        }

        void CreateOrganization()
        {
            if(Org()!=null){notify("Ya existe una organizaci\u00F3n local activa.",AmoTheme.Info);return;}
            string name=Prompt("Crear organizaci\u00F3n local","Nombre de la organizaci\u00F3n","DesarrollAMO");
            if(String.IsNullOrWhiteSpace(name))return;
            store.CreateOrganization(name,Environment.UserName,"",AmoPlan.Elite);
            notify("Organizaci\u00F3n Elite local creada.",AmoTheme.Success);RefreshAll();
        }

        void AddUser()
        {
            var org=Org();if(org==null){notify("Primero cre\u00E1 una organizaci\u00F3n local.",AmoTheme.Warning);return;}
            using(var f=new Form{Text="Agregar usuario \u00B7 ArrobAMO",ClientSize=new Size(540,330),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=AmoTheme.Bg,ForeColor=AmoTheme.Text,Font=AmoTheme.UI(9f)})
            {
                f.Controls.Add(new Label{Text="Nombre",Left=22,Top=22,Width=100,Height=20,ForeColor=AmoTheme.TextSoft});
                var name=new TextBox{Left=22,Top=46,Width=496,Height=32};AmoTheme.StyleInput(name,false);f.Controls.Add(name);
                f.Controls.Add(new Label{Text="Email",Left=22,Top=92,Width=100,Height=20,ForeColor=AmoTheme.TextSoft});
                var email=new TextBox{Left=22,Top=116,Width=496,Height=32};AmoTheme.StyleInput(email,false);f.Controls.Add(email);
                f.Controls.Add(new Label{Text="Rol",Left=22,Top=162,Width=100,Height=20,ForeColor=AmoTheme.TextSoft});
                var role=new ComboBox{Left=22,Top=186,Width=320,DropDownStyle=ComboBoxStyle.DropDownList};
                foreach(var r in org.Roles)role.Items.Add(new RoleItem(r));if(role.Items.Count>0)role.SelectedIndex=0;f.Controls.Add(role);
                var external=new CheckBox{Text="Invitado externo",Left=360,Top=188,Width=150,BackColor=AmoTheme.Bg,ForeColor=AmoTheme.Text};f.Controls.Add(external);
                var cancel=new AmoButton{Text="Cancelar",Variant=AmoButtonVariant.Secondary,Left=310,Top=258,Width=98,Height=38,DialogResult=DialogResult.Cancel};
                var add=new AmoButton{Text="Agregar",Variant=AmoButtonVariant.Primary,Left=420,Top=258,Width=98,Height=38,DialogResult=DialogResult.OK};
                f.Controls.Add(cancel);f.Controls.Add(add);f.AcceptButton=add;f.CancelButton=cancel;
                if(f.ShowDialog(this)!=DialogResult.OK)return;
                var ri=role.SelectedItem as RoleItem;if(ri==null||String.IsNullOrWhiteSpace(name.Text))return;
                store.AddUser(org,name.Text.Trim(),email.Text.Trim(),external.Checked,ri.Role.Id);RefreshAll();
            }
        }

        void AddCustomRole()
        {
            var org=Org();if(org==null){notify("Primero cre\u00E1 una organizaci\u00F3n local.",AmoTheme.Warning);return;}
            using(var f=new Form{Text="Rol personalizado \u00B7 ArrobAMO",ClientSize=new Size(620,520),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=AmoTheme.Bg,ForeColor=AmoTheme.Text,Font=AmoTheme.UI(9f)})
            {
                f.Controls.Add(new Label{Text="Nombre del rol",Left=22,Top=20,Width=180,Height=20,ForeColor=AmoTheme.TextSoft});
                var name=new TextBox{Left=22,Top=44,Width=574,Height=32};AmoTheme.StyleInput(name,false);f.Controls.Add(name);
                f.Controls.Add(new Label{Text="Permisos granulares",Left=22,Top=94,Width=220,Height=22,Font=AmoTheme.UI(10f,FontStyle.Bold),ForeColor=AmoTheme.Text});
                var perms=new CheckedListBox{Left=22,Top=124,Width=574,Height=300,CheckOnClick=true,BackColor=AmoTheme.Surface,ForeColor=AmoTheme.Text};
                foreach(string permission in AmoPermissions.All)perms.Items.Add(permission,false);f.Controls.Add(perms);
                var cancel=new AmoButton{Text="Cancelar",Variant=AmoButtonVariant.Secondary,Left=388,Top=454,Width=98,Height=38,DialogResult=DialogResult.Cancel};
                var add=new AmoButton{Text="Crear rol",Variant=AmoButtonVariant.Primary,Left=498,Top=454,Width=98,Height=38,DialogResult=DialogResult.OK};
                f.Controls.Add(cancel);f.Controls.Add(add);f.AcceptButton=add;f.CancelButton=cancel;
                if(f.ShowDialog(this)!=DialogResult.OK||String.IsNullOrWhiteSpace(name.Text))return;
                var selected=perms.CheckedItems.Cast<object>().Select(delegate(object x){return Convert.ToString(x);}).ToArray();
                store.AddCustomRole(org,name.Text.Trim(),selected);RefreshAll();
            }
        }

        void AddTeam()
        {
            var org=Org();if(org==null){notify("Primero cre\u00E1 una organizaci\u00F3n local.",AmoTheme.Warning);return;}
            string name=Prompt("Nuevo equipo","Nombre del equipo","Desarrollo");if(String.IsNullOrWhiteSpace(name))return;
            store.AddTeam(org,name,org.OwnerUserId);RefreshAll();
        }

        void AddWorkspace()
        {
            var org=Org();if(org==null){notify("Primero cre\u00E1 una organizaci\u00F3n local.",AmoTheme.Warning);return;}
            string name=Prompt("Nuevo workspace compartido","Nombre del workspace","Proyecto");if(String.IsNullOrWhiteSpace(name))return;
            string teamId=org.Teams.Count>0?org.Teams[0].Id:null;
            store.AddWorkspace(org,name,teamId==null?AmoWorkspaceScope.Shared:AmoWorkspaceScope.Team,org.OwnerUserId,teamId);RefreshAll();
        }

        void ToggleKillSwitch()
        {
            var org=Org();if(org==null)return;store.SetKillSwitch(org,!org.AgentKillSwitch,org.OwnerUserId);
            notify(org.AgentKillSwitch?"Todos los agentes locales quedaron detenidos.":"Kill switch liberado.",org.AgentKillSwitch?AmoTheme.Danger:AmoTheme.Success);RefreshAll();
        }

        void DecideSelected(ListView list,bool approve)
        {
            var org=Org();if(org==null||list.SelectedItems.Count==0)return;
            store.DecideApproval(org,Convert.ToString(list.SelectedItems[0].Tag),org.OwnerUserId,approve);RefreshAll();
        }

        string Prompt(string title,string label,string initial)
        {
            using(var f=new Form{Text=title,ClientSize=new Size(480,200),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,BackColor=AmoTheme.Bg,ForeColor=AmoTheme.Text,Font=AmoTheme.UI(9f)})
            {
                f.Controls.Add(new Label{Text=label,Left=22,Top=24,Width=420,Height=22,ForeColor=AmoTheme.TextSoft});
                var input=new TextBox{Left=22,Top=54,Width=436,Height=34,Text=initial};AmoTheme.StyleInput(input,false);f.Controls.Add(input);
                var cancel=new AmoButton{Text="Cancelar",Variant=AmoButtonVariant.Secondary,Left=250,Top=126,Width=98,Height=38,DialogResult=DialogResult.Cancel};
                var ok=new AmoButton{Text="Crear",Variant=AmoButtonVariant.Primary,Left=360,Top=126,Width=98,Height=38,DialogResult=DialogResult.OK};
                f.Controls.Add(cancel);f.Controls.Add(ok);f.AcceptButton=ok;f.CancelButton=cancel;
                if(f.ShowDialog(this)!=DialogResult.OK)return null;return input.Text.Trim();
            }
        }

        sealed class RoleItem
        {
            public AmoRole Role;
            public RoleItem(AmoRole role){Role=role;}
            public override string ToString(){return Role.Name;}
        }

        static string UserName(AmoOrganization org,string id)
        {
            var u=org.Users.FirstOrDefault(delegate(AmoUser x){return x.Id==id;});return u==null?(String.IsNullOrWhiteSpace(id)?"Sistema":id):u.DisplayName;
        }
        static string ScopeName(AmoWorkspaceScope s){return s==AmoWorkspaceScope.Personal?"Personal":s==AmoWorkspaceScope.Shared?"Compartido":s==AmoWorkspaceScope.Team?"Equipo":"Organizaci\u00F3n";}
        static string ApprovalName(AmoApprovalState s){return s==AmoApprovalState.Draft?"Borrador":s==AmoApprovalState.Review?"Revisi\u00F3n":s==AmoApprovalState.Approved?"Aprobado":s==AmoApprovalState.Published?"Publicado":"Rechazado";}
        static string ResultName(AmoActivityResult s){return s==AmoActivityResult.Success?"Correcto":s==AmoActivityResult.Failed?"Fall\u00F3":"Pendiente";}
    }
}


