using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace ArrobAMO
{
    public enum AmoPlan { Free, Pro, Elite, Enterprise }
    public enum AmoWorkspaceScope { Personal, Shared, Team, Organization }
    public enum AmoApprovalState { Draft, Review, Approved, Published, Rejected }
    public enum AmoActivityResult { Success, Failed, Pending }
    public enum AmoAutonomyLevel { RecommendOnly=0, PrepareActions=1, ReversibleActions=2, ApprovedWorkflows=3, SupervisedAutonomy=4 }

    public static class AmoPermissions
    {
        public const string WorkspaceRead="workspace.read";
        public const string WorkspaceWrite="workspace.write";
        public const string WorkspaceShare="workspace.share";
        public const string AutomationRead="automation.read";
        public const string AutomationRun="automation.run";
        public const string AutomationEdit="automation.edit";
        public const string AutomationPublish="automation.publish";
        public const string AgentRead="agent.read";
        public const string AgentRun="agent.run";
        public const string AgentConfigure="agent.configure";
        public const string ExtensionInstall="extension.install";
        public const string ExtensionPublish="extension.publish";
        public const string UserManage="user.manage";
        public const string PolicyManage="policy.manage";
        public const string AuditRead="audit.read";
        public const string BillingManage="billing.manage";
        public const string SecretUse="secret.use";
        public const string SecretManage="secret.manage";

        public static readonly string[] All = new string[] {
            WorkspaceRead,WorkspaceWrite,WorkspaceShare,
            AutomationRead,AutomationRun,AutomationEdit,AutomationPublish,
            AgentRead,AgentRun,AgentConfigure,
            ExtensionInstall,ExtensionPublish,
            UserManage,PolicyManage,AuditRead,BillingManage,SecretUse,SecretManage
        };
    }

    public static class AmoEntitlements
    {
        static readonly Dictionary<string,AmoPlan> MinimumPlan = new Dictionary<string,AmoPlan>(StringComparer.OrdinalIgnoreCase)
        {
            {"browser.core", AmoPlan.Free},{"browser.private", AmoPlan.Free},{"browser.bookmarks", AmoPlan.Free},
            {"browser.history", AmoPlan.Free},{"browser.downloads", AmoPlan.Free},{"workspace.personal", AmoPlan.Free},
            {"automation.basic", AmoPlan.Free},{"ai.connect", AmoPlan.Free},
            {"workspace.pro", AmoPlan.Pro},{"automation.pro", AmoPlan.Pro},{"command.palette", AmoPlan.Pro},{"resource.manager", AmoPlan.Pro},
            {"organization.local", AmoPlan.Elite},{"team.local", AmoPlan.Elite},{"workspace.shared", AmoPlan.Elite},{"rbac", AmoPlan.Elite},
            {"comments", AmoPlan.Elite},{"activity", AmoPlan.Elite},{"approvals", AmoPlan.Elite},{"agents.supervised", AmoPlan.Elite},
            {"package.arrobamo", AmoPlan.Elite},
            {"admin.center", AmoPlan.Enterprise},{"policy.center", AmoPlan.Enterprise},{"audit.enterprise", AmoPlan.Enterprise},
            {"device.management", AmoPlan.Enterprise},{"private.marketplace", AmoPlan.Enterprise},{"enterprise.api", AmoPlan.Enterprise}
        };

        public static bool Has(AmoPlan plan,string feature)
        {
            AmoPlan min;
            return MinimumPlan.TryGetValue(feature,out min) && plan>=min;
        }

        public static KeyValuePair<string,AmoPlan>[] Matrix()
        {
            return MinimumPlan.OrderBy(delegate(KeyValuePair<string,AmoPlan> x){return x.Value;})
                              .ThenBy(delegate(KeyValuePair<string,AmoPlan> x){return x.Key;}).ToArray();
        }
    }

    public sealed class AmoRole
    {
        public string Id; public string Name; public bool BuiltIn; public List<string> Permissions;
        public AmoRole(){Permissions=new List<string>();}
    }

    public sealed class AmoUser
    {
        public string Id; public string DisplayName; public string Email; public bool External; public bool Active; public List<string> RoleIds;
        public AmoUser(){Active=true;RoleIds=new List<string>();}
    }

    public sealed class AmoTeam
    {
        public string Id; public string Name; public string ResponsibleUserId; public List<string> MemberUserIds; public List<string> RoleIds;
        public AmoTeam(){MemberUserIds=new List<string>();RoleIds=new List<string>();}
    }

    public sealed class AmoWorkspace
    {
        public string Id; public string Name; public AmoWorkspaceScope Scope; public string OwnerUserId; public string TeamId;
        public DateTime UpdatedUtc; public List<string> MemberUserIds; public Dictionary<string,string> MemberAccess;
        public List<string> Tabs; public List<string> ScriptIds; public List<string> AgentIds;
        public AmoWorkspace(){UpdatedUtc=DateTime.UtcNow;MemberUserIds=new List<string>();MemberAccess=new Dictionary<string,string>();Tabs=new List<string>();ScriptIds=new List<string>();AgentIds=new List<string>();}
    }

    public sealed class AmoComment
    {
        public string Id; public string ResourceType; public string ResourceId; public string AuthorUserId; public string Text;
        public DateTime CreatedUtc; public List<string> MentionUserIds;
        public AmoComment(){CreatedUtc=DateTime.UtcNow;MentionUserIds=new List<string>();}
    }

    public sealed class AmoActivity
    {
        public string Id; public DateTime CreatedUtc; public string ActorType; public string ActorId; public string Action;
        public string ResourceType; public string ResourceId; public AmoActivityResult Result; public string Summary;
        public AmoActivity(){CreatedUtc=DateTime.UtcNow;}
    }

    public sealed class AmoVersionRecord
    {
        public string Id; public string ResourceType; public string ResourceId; public string Version; public string AuthorUserId;
        public DateTime CreatedUtc; public string SnapshotJson; public string ChangeSummary;
        public AmoVersionRecord(){CreatedUtc=DateTime.UtcNow;}
    }

    public sealed class AmoApproval
    {
        public string Id; public string ResourceType; public string ResourceId; public AmoApprovalState State;
        public string RequestedByUserId; public string ReviewedByUserId; public string Reason; public DateTime UpdatedUtc;
        public AmoApproval(){State=AmoApprovalState.Draft;UpdatedUtc=DateTime.UtcNow;}
    }

    public sealed class AmoAgent
    {
        public string Id; public string Name; public string Purpose; public string OwnerUserId; public string WorkspaceId; public string Model;
        public AmoAutonomyLevel Autonomy; public int MaxActions; public int MaxMinutes; public decimal MaxCostUsd; public bool Enabled;
        public List<string> Permissions; public List<string> AllowedDomains; public List<string> AllowedTools;
        public AmoAgent(){Autonomy=AmoAutonomyLevel.RecommendOnly;MaxActions=25;MaxMinutes=20;Enabled=true;Permissions=new List<string>();AllowedDomains=new List<string>();AllowedTools=new List<string>();}
    }

    public sealed class AmoOrganization
    {
        public string Id; public string Name; public string OwnerUserId; public AmoPlan Plan; public DateTime CreatedUtc;
        public List<AmoUser> Users; public List<AmoTeam> Teams; public List<AmoRole> Roles; public List<AmoWorkspace> Workspaces;
        public List<AmoComment> Comments; public List<AmoActivity> Activity; public List<AmoVersionRecord> Versions;
        public List<AmoApproval> Approvals; public List<AmoAgent> Agents; public bool AgentKillSwitch;
        public AmoOrganization(){Plan=AmoPlan.Elite;CreatedUtc=DateTime.UtcNow;Users=new List<AmoUser>();Teams=new List<AmoTeam>();Roles=new List<AmoRole>();Workspaces=new List<AmoWorkspace>();Comments=new List<AmoComment>();Activity=new List<AmoActivity>();Versions=new List<AmoVersionRecord>();Approvals=new List<AmoApproval>();Agents=new List<AmoAgent>();}
    }

    public sealed class AmoEnterpriseState
    {
        public int SchemaVersion; public string ActiveOrganizationId; public List<AmoOrganization> Organizations;
        public AmoEnterpriseState(){SchemaVersion=1;Organizations=new List<AmoOrganization>();}
    }

    public static class AmoRoleFactory
    {
        static AmoRole Role(string id,string name,params string[] p)
        {
            AmoRole r=new AmoRole(); r.Id=id;r.Name=name;r.BuiltIn=true;r.Permissions=p.Distinct(StringComparer.OrdinalIgnoreCase).ToList();return r;
        }
        public static List<AmoRole> Defaults()
        {
            return new List<AmoRole>{
                Role("owner","Propietario",AmoPermissions.All),
                Role("admin","Administrador",AmoPermissions.All.Where(delegate(string x){return x!=AmoPermissions.BillingManage;}).ToArray()),
                Role("supervisor","Supervisor",AmoPermissions.WorkspaceRead,AmoPermissions.WorkspaceWrite,AmoPermissions.WorkspaceShare,AmoPermissions.AutomationRead,AmoPermissions.AutomationRun,AmoPermissions.AutomationEdit,AmoPermissions.AutomationPublish,AmoPermissions.AgentRead,AmoPermissions.AgentRun,AmoPermissions.AuditRead),
                Role("member","Miembro",AmoPermissions.WorkspaceRead,AmoPermissions.WorkspaceWrite,AmoPermissions.AutomationRead,AmoPermissions.AutomationRun,AmoPermissions.AgentRead),
                Role("collaborator","Colaborador",AmoPermissions.WorkspaceRead,AmoPermissions.WorkspaceWrite,AmoPermissions.AutomationRead),
                Role("guest","Invitado",AmoPermissions.WorkspaceRead),
                Role("auditor","Auditor",AmoPermissions.WorkspaceRead,AmoPermissions.AutomationRead,AmoPermissions.AgentRead,AmoPermissions.AuditRead),
                Role("developer","Desarrollador",AmoPermissions.WorkspaceRead,AmoPermissions.WorkspaceWrite,AmoPermissions.AutomationRead,AmoPermissions.AutomationRun,AmoPermissions.AutomationEdit,AmoPermissions.AgentRead,AmoPermissions.AgentRun,AmoPermissions.ExtensionInstall,AmoPermissions.ExtensionPublish),
                Role("ai-agent","Agente IA",AmoPermissions.WorkspaceRead,AmoPermissions.AutomationRead,AmoPermissions.AgentRead)
            };
        }
    }

    public sealed class AmoEnterpriseStore
    {
        readonly string root; readonly string file; readonly JavaScriptSerializer json;
        public AmoEnterpriseState State;

        public AmoEnterpriseStore(string dataRoot)
        {
            root=Path.Combine(dataRoot,"Enterprise");file=Path.Combine(root,"enterprise.json");
            Directory.CreateDirectory(root);json=new JavaScriptSerializer();json.MaxJsonLength=int.MaxValue;State=Load();
        }

        AmoEnterpriseState Load()
        {
            try{if(!File.Exists(file))return new AmoEnterpriseState();AmoEnterpriseState s=json.Deserialize<AmoEnterpriseState>(File.ReadAllText(file));return s??new AmoEnterpriseState();}
            catch{return new AmoEnterpriseState();}
        }

        public void Save()
        {
            Directory.CreateDirectory(root);string tmp=file+".tmp";File.WriteAllText(tmp,json.Serialize(State));
            if(File.Exists(file))File.Delete(file);File.Move(tmp,file);
        }

        public AmoOrganization CreateOrganization(string name,string ownerName,string ownerEmail,AmoPlan plan)
        {
            if(plan<AmoPlan.Elite)throw new InvalidOperationException("Las organizaciones requieren Elite o Enterprise.");
            if(String.IsNullOrWhiteSpace(name))throw new ArgumentException("Nombre requerido.");
            string uid=Guid.NewGuid().ToString("N");
            AmoOrganization org=new AmoOrganization();org.Id=Guid.NewGuid().ToString("N");org.Name=name.Trim();org.OwnerUserId=uid;org.Plan=plan;org.Roles=AmoRoleFactory.Defaults();
            AmoUser owner=new AmoUser();owner.Id=uid;owner.DisplayName=ownerName??"Propietario";owner.Email=ownerEmail??"";owner.RoleIds.Add("owner");org.Users.Add(owner);
            org.Activity.Add(NewActivity("user",uid,"organization.created","organization",org.Id,AmoActivityResult.Success,"Organizaci\u00F3n creada"));
            State.Organizations.Add(org);State.ActiveOrganizationId=org.Id;Save();return org;
        }

        public AmoOrganization ActiveOrganization(){return State.Organizations.FirstOrDefault(delegate(AmoOrganization x){return x.Id==State.ActiveOrganizationId;});}

        public AmoUser AddUser(AmoOrganization org,string displayName,string email,bool external,string roleId)
        {
            Require(org!=null,"Organizaci\u00F3n requerida.");
            Require(!String.IsNullOrWhiteSpace(displayName),"Nombre requerido.");
            if(!String.IsNullOrWhiteSpace(roleId))Require(org.Roles.Any(delegate(AmoRole r){return r.Id==roleId;}),"Rol inexistente.");
            AmoUser u=new AmoUser();u.Id=Guid.NewGuid().ToString("N");u.DisplayName=displayName.Trim();u.Email=email??"";u.External=external;
            if(!String.IsNullOrWhiteSpace(roleId))u.RoleIds.Add(roleId);
            org.Users.Add(u);
            org.Activity.Add(NewActivity("user",org.OwnerUserId,"user.added","user",u.Id,AmoActivityResult.Success,external?"Invitado agregado":"Usuario agregado"));
            Save();return u;
        }

        public AmoRole AddCustomRole(AmoOrganization org,string name,IEnumerable<string> permissions)
        {
            Require(org!=null,"Organizaci\u00F3n requerida.");
            Require(!String.IsNullOrWhiteSpace(name),"Nombre requerido.");
            AmoRole r=new AmoRole();r.Id="custom-"+Guid.NewGuid().ToString("N");r.Name=name.Trim();r.BuiltIn=false;
            r.Permissions=(permissions??Enumerable.Empty<string>()).Where(delegate(string p){return AmoPermissions.All.Contains(p,StringComparer.OrdinalIgnoreCase);}).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            org.Roles.Add(r);
            org.Activity.Add(NewActivity("user",org.OwnerUserId,"role.created","role",r.Id,AmoActivityResult.Success,"Rol personalizado creado: "+r.Name));
            Save();return r;
        }

        public void AssignRole(AmoOrganization org,string userId,string roleId)
        {
            Require(org!=null,"Organizaci\u00F3n requerida.");
            AmoUser u=org.Users.FirstOrDefault(delegate(AmoUser x){return x.Id==userId;});
            Require(u!=null,"Usuario inexistente.");
            Require(org.Roles.Any(delegate(AmoRole r){return r.Id==roleId;}),"Rol inexistente.");
            if(!u.RoleIds.Contains(roleId,StringComparer.OrdinalIgnoreCase))u.RoleIds.Add(roleId);
            org.Activity.Add(NewActivity("user",org.OwnerUserId,"role.assigned","user",userId,AmoActivityResult.Success,"Rol asignado: "+roleId));
            Save();
        }

        public AmoVersionRecord AddVersion(AmoOrganization org,string resourceType,string resourceId,string version,string authorUserId,string snapshotJson,string summary)
        {
            Require(org!=null,"Organizaci\u00F3n requerida.");
            AmoVersionRecord v=new AmoVersionRecord();v.Id=Guid.NewGuid().ToString("N");v.ResourceType=resourceType;v.ResourceId=resourceId;v.Version=version;v.AuthorUserId=authorUserId;v.SnapshotJson=snapshotJson??"";v.ChangeSummary=summary??"";
            org.Versions.Add(v);
            org.Activity.Add(NewActivity("user",authorUserId,"version.created",resourceType,resourceId,AmoActivityResult.Success,"Versi\u00F3n "+version+" creada"));
            Save();return v;
        }

        public AmoTeam AddTeam(AmoOrganization org,string name,string responsibleUserId)
        {
            Require(org!=null,"Organizaci\u00F3n requerida.");AmoTeam team=new AmoTeam();team.Id=Guid.NewGuid().ToString("N");team.Name=name.Trim();team.ResponsibleUserId=responsibleUserId;
            if(!String.IsNullOrWhiteSpace(responsibleUserId))team.MemberUserIds.Add(responsibleUserId);org.Teams.Add(team);
            org.Activity.Add(NewActivity("user",responsibleUserId,"team.created","team",team.Id,AmoActivityResult.Success,"Equipo creado: "+team.Name));Save();return team;
        }

        public AmoWorkspace AddWorkspace(AmoOrganization org,string name,AmoWorkspaceScope scope,string ownerUserId,string teamId)
        {
            Require(org!=null,"Organizaci\u00F3n requerida.");AmoWorkspace ws=new AmoWorkspace();ws.Id=Guid.NewGuid().ToString("N");ws.Name=name.Trim();ws.Scope=scope;ws.OwnerUserId=ownerUserId;ws.TeamId=teamId;
            if(!String.IsNullOrWhiteSpace(ownerUserId))ws.MemberUserIds.Add(ownerUserId);org.Workspaces.Add(ws);
            org.Activity.Add(NewActivity("user",ownerUserId,"workspace.created","workspace",ws.Id,AmoActivityResult.Success,"Workspace creado: "+ws.Name));Save();return ws;
        }

        public bool HasPermission(AmoOrganization org,string userId,string permission,string teamId)
        {
            if(org==null||String.IsNullOrWhiteSpace(userId)||String.IsNullOrWhiteSpace(permission))return false;
            AmoUser user=org.Users.FirstOrDefault(delegate(AmoUser x){return x.Id==userId&&x.Active;});if(user==null)return false;
            HashSet<string> roleIds=new HashSet<string>(user.RoleIds??new List<string>(),StringComparer.OrdinalIgnoreCase);
            if(!String.IsNullOrWhiteSpace(teamId)){AmoTeam team=org.Teams.FirstOrDefault(delegate(AmoTeam x){return x.Id==teamId&&x.MemberUserIds.Contains(userId);});if(team!=null)foreach(string r in team.RoleIds??new List<string>())roleIds.Add(r);}
            return org.Roles.Where(delegate(AmoRole r){return roleIds.Contains(r.Id);}).Any(delegate(AmoRole r){return (r.Permissions??new List<string>()).Contains(permission,StringComparer.OrdinalIgnoreCase);});
        }

        public bool HasPermission(AmoOrganization org,string userId,string permission){return HasPermission(org,userId,permission,null);}

        public AmoComment AddComment(AmoOrganization org,string resourceType,string resourceId,string authorUserId,string text,IEnumerable<string> mentions)
        {
            AmoComment c=new AmoComment();c.Id=Guid.NewGuid().ToString("N");c.ResourceType=resourceType;c.ResourceId=resourceId;c.AuthorUserId=authorUserId;c.Text=text??"";c.MentionUserIds=(mentions??Enumerable.Empty<string>()).Distinct().ToList();
            org.Comments.Add(c);org.Activity.Add(NewActivity("user",authorUserId,"comment.created",resourceType,resourceId,AmoActivityResult.Success,"Comentario agregado"));Save();return c;
        }

        public AmoApproval RequestApproval(AmoOrganization org,string resourceType,string resourceId,string userId,string reason)
        {
            AmoApproval a=new AmoApproval();a.Id=Guid.NewGuid().ToString("N");a.ResourceType=resourceType;a.ResourceId=resourceId;a.RequestedByUserId=userId;a.Reason=reason??"";a.State=AmoApprovalState.Review;
            org.Approvals.Add(a);org.Activity.Add(NewActivity("user",userId,"approval.requested",resourceType,resourceId,AmoActivityResult.Pending,"Aprobaci\u00F3n solicitada"));Save();return a;
        }

        public void DecideApproval(AmoOrganization org,string approvalId,string reviewerId,bool approve)
        {
            AmoApproval a=org.Approvals.FirstOrDefault(delegate(AmoApproval x){return x.Id==approvalId;});Require(a!=null,"Aprobaci\u00F3n inexistente.");
            a.ReviewedByUserId=reviewerId;a.State=approve?AmoApprovalState.Approved:AmoApprovalState.Rejected;a.UpdatedUtc=DateTime.UtcNow;
            org.Activity.Add(NewActivity("user",reviewerId,approve?"approval.approved":"approval.rejected",a.ResourceType,a.ResourceId,AmoActivityResult.Success,approve?"Aprobado":"Rechazado"));Save();
        }

        public void SetKillSwitch(AmoOrganization org,bool stopped,string actorUserId)
        {
            org.AgentKillSwitch=stopped;org.Activity.Add(NewActivity("user",actorUserId,stopped?"agents.stopped":"agents.resumed","organization",org.Id,AmoActivityResult.Success,stopped?"Agentes detenidos":"Agentes habilitados"));Save();
        }

        static AmoActivity NewActivity(string actorType,string actorId,string action,string resourceType,string resourceId,AmoActivityResult result,string summary)
        {
            AmoActivity a=new AmoActivity();a.Id=Guid.NewGuid().ToString("N");a.ActorType=actorType;a.ActorId=actorId;a.Action=action;a.ResourceType=resourceType;a.ResourceId=resourceId;a.Result=result;a.Summary=summary;return a;
        }
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}

