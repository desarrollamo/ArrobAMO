using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ArrobAMO;

class EnterpriseSelfTest
{
    static int fails=0;
    static void Ok(bool x,string name){Console.WriteLine((x?"PASS ":"FAIL ")+name);if(!x)fails++;}
    static void Main()
    {
        string root=Path.Combine(Path.GetTempPath(),"ArrobAMO-EnterpriseSelfTest-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store=new AmoEnterpriseStore(root);

            bool freeBlocked=false;
            try{store.CreateOrganization("NoDebe","x","",AmoPlan.Free);}catch(InvalidOperationException){freeBlocked=true;}
            Ok(freeBlocked,"Free no puede crear organización");
            Ok(AmoEntitlements.Has(AmoPlan.Free,"browser.core"),"Navegación esencial está en Free");
            Ok(!AmoEntitlements.Has(AmoPlan.Free,"organization.local"),"Organización no está en Free");
            Ok(AmoEntitlements.Has(AmoPlan.Elite,"organization.local"),"Organización disponible en Elite");

            var org=store.CreateOrganization("DesarrollAMO","Juan","juan@example.com",AmoPlan.Elite);
            Ok(org.Roles.Count==9,"Roles base creados");
            Ok(org.Users.Count==1,"Propietario creado");
            var owner=org.Users[0];
            Ok(store.HasPermission(org,owner.Id,AmoPermissions.BillingManage),"Propietario tiene billing.manage");

            var guest=store.AddUser(org,"Invitado","guest@example.com",true,"guest");
            Ok(store.HasPermission(org,guest.Id,AmoPermissions.WorkspaceRead),"Invitado puede leer workspace");
            Ok(!store.HasPermission(org,guest.Id,AmoPermissions.WorkspaceWrite),"Invitado no puede escribir workspace");

            var custom=store.AddCustomRole(org,"Editor limitado",new[]{AmoPermissions.WorkspaceRead,AmoPermissions.WorkspaceWrite,"bad.permission"});
            Ok(!custom.BuiltIn && custom.Permissions.Count==2,"Rol personalizado filtra permisos inválidos");
            store.AssignRole(org,guest.Id,custom.Id);
            Ok(store.HasPermission(org,guest.Id,AmoPermissions.WorkspaceWrite),"Asignación de rol personalizado efectiva");

            var team=store.AddTeam(org,"Desarrollo",owner.Id);
            var ws=store.AddWorkspace(org,"ArrobAMO",AmoWorkspaceScope.Team,owner.Id,team.Id);
            Ok(org.Teams.Count==1 && org.Workspaces.Count==1,"Equipo y workspace persistidos");
            Ok(ws.Scope==AmoWorkspaceScope.Team,"Scope workspace Team");

            var c=store.AddComment(org,"workspace",ws.Id,owner.Id,"@guest revisión",new[]{guest.Id});
            Ok(c.MentionUserIds.Contains(guest.Id),"Menciones de comentarios");

            var v=store.AddVersion(org,"workspace",ws.Id,"1.0.0",owner.Id,"{}","Base");
            Ok(v.Version=="1.0.0" && org.Versions.Count==1,"Historial de versiones");

            var a=store.RequestApproval(org,"automation","auto1",owner.Id,"Publicar v1");
            Ok(a.State==AmoApprovalState.Review,"Aprobación entra en revisión");
            store.DecideApproval(org,a.Id,owner.Id,true);
            Ok(a.State==AmoApprovalState.Approved,"Aprobación aprobada");

            store.SetKillSwitch(org,true,owner.Id);
            Ok(org.AgentKillSwitch,"Kill switch activo");

            var reload=new AmoEnterpriseStore(root);
            var org2=reload.ActiveOrganization();
            Ok(org2!=null && org2.Name=="DesarrollAMO","Recarga de organización");
            Ok(org2.AgentKillSwitch,"Kill switch persiste");
            Ok(org2.Activity.Count>=10,"Actividad auditable creada");
            Ok(org2.Approvals.Count==1 && org2.Approvals[0].State==AmoApprovalState.Approved,"Aprobación persiste");
            Ok(org2.Versions.Count==1,"Versiones persisten");
            Ok(org2.Users.Count==2 && org2.Roles.Count==10,"Usuarios y roles persisten");

            Console.WriteLine("FAILS="+fails);
            Environment.ExitCode=fails==0?0:1;
        }
        finally{try{Directory.Delete(root,true);}catch{}}
    }
}

