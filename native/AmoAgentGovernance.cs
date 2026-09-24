using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrobAMO
{
    public enum AmoAgentActionKind
    {
        ReadPage,
        Navigate,
        OpenTab,
        UseTool,
        RunScript,
        WriteForm,
        Download,
        SendInformation,
        Publish,
        Purchase,
        Transfer,
        Delete,
        InstallSoftware,
        ChangePermissions,
        AdministrativeAction
    }

    public sealed class AmoAgentAction
    {
        public string Id;
        public string AgentId;
        public string ResponsibleUserId;
        public AmoAgentActionKind Kind;
        public string Target;
        public string Tool;
        public string Domain;
        public bool Reversible;
        public string Summary;
        public decimal EstimatedCostUsd;
        public AmoAgentAction()
        {
            Id=Guid.NewGuid().ToString("N");
        }
    }

    public sealed class AmoAgentRunState
    {
        public string RunId;
        public int ActionsExecuted;
        public DateTime StartedUtc;
        public decimal CostUsd;
        public bool Stopped;
        public AmoAgentRunState()
        {
            RunId=Guid.NewGuid().ToString("N");
            StartedUtc=DateTime.UtcNow;
        }
    }

    public sealed class AmoAgentDecision
    {
        public bool Allowed;
        public bool RequiresApproval;
        public string Code;
        public string Reason;
    }

    public static class AmoAgentGovernor
    {
        public static AmoAgentDecision Evaluate(AmoOrganization org,AmoAgent agent,AmoAgentAction action,AmoAgentRunState run)
        {
            if(org==null)return Deny("organization.missing","No hay organizaci\u00F3n.");
            if(agent==null)return Deny("agent.missing","Agente inexistente.");
            if(action==null)return Deny("action.missing","Acci\u00F3n inexistente.");
            if(run==null)return Deny("run.missing","Ejecuci\u00F3n inexistente.");

            if(org.AgentKillSwitch || run.Stopped)
                return Deny("agent.kill_switch","Los agentes est\u00E1n detenidos.");

            if(!agent.Enabled)
                return Deny("agent.disabled","El agente est\u00E1 deshabilitado.");

            if(agent.MaxActions>0 && run.ActionsExecuted>=agent.MaxActions)
                return Deny("agent.max_actions","Se alcanz\u00F3 el m\u00E1ximo de acciones.");

            if(agent.MaxMinutes>0 && (DateTime.UtcNow-run.StartedUtc).TotalMinutes>=agent.MaxMinutes)
                return Deny("agent.timeout","Se alcanz\u00F3 el tiempo m\u00E1ximo.");

            if(agent.MaxCostUsd>0m && run.CostUsd+action.EstimatedCostUsd>agent.MaxCostUsd)
                return Deny("agent.max_cost","La acci\u00F3n excede el presupuesto del agente.");

            if(!String.IsNullOrWhiteSpace(action.Domain) &&
               agent.AllowedDomains!=null && agent.AllowedDomains.Count>0 &&
               !agent.AllowedDomains.Contains(action.Domain,StringComparer.OrdinalIgnoreCase))
                return Deny("agent.domain_blocked","El dominio no est\u00E1 permitido.");

            if(!String.IsNullOrWhiteSpace(action.Tool) &&
               agent.AllowedTools!=null && agent.AllowedTools.Count>0 &&
               !agent.AllowedTools.Contains(action.Tool,StringComparer.OrdinalIgnoreCase))
                return Deny("agent.tool_blocked","La herramienta no est\u00E1 permitida.");

            string needed=PermissionFor(action.Kind);
            if(!String.IsNullOrWhiteSpace(needed) &&
               (agent.Permissions==null || !agent.Permissions.Contains(needed,StringComparer.OrdinalIgnoreCase)))
                return Deny("agent.permission_denied","El agente no tiene permiso para esta acci\u00F3n.");

            if(agent.Autonomy==AmoAutonomyLevel.RecommendOnly)
                return Approval("agent.level0","Nivel 0: s\u00F3lo puede recomendar.");

            if(agent.Autonomy==AmoAutonomyLevel.PrepareActions)
                return Approval("agent.level1","Nivel 1: puede preparar, no ejecutar.");

            if(IsAlwaysSensitive(action.Kind))
                return Approval("action.sensitive","La acci\u00F3n requiere aprobaci\u00F3n humana.");

            if(agent.Autonomy==AmoAutonomyLevel.ReversibleActions && !action.Reversible)
                return Approval("agent.level2.irreversible","Nivel 2 s\u00F3lo ejecuta acciones reversibles.");

            if(agent.Autonomy==AmoAutonomyLevel.ApprovedWorkflows)
                return Approval("agent.level3.workflow","Nivel 3 requiere un workflow previamente aprobado.");

            return Allow();
        }

        public static void RegisterExecuted(AmoAgentRunState run,AmoAgentAction action)
        {
            if(run==null)return;
            run.ActionsExecuted++;
            if(action!=null)run.CostUsd+=action.EstimatedCostUsd;
        }

        public static string PermissionFor(AmoAgentActionKind kind)
        {
            switch(kind)
            {
                case AmoAgentActionKind.ReadPage: return "page.read";
                case AmoAgentActionKind.Navigate: return "navigation.use";
                case AmoAgentActionKind.OpenTab: return "tabs.open";
                case AmoAgentActionKind.UseTool: return "tool.use";
                case AmoAgentActionKind.RunScript: return "automation.run";
                case AmoAgentActionKind.WriteForm: return "form.write";
                case AmoAgentActionKind.Download: return "downloads.create";
                case AmoAgentActionKind.SendInformation: return "data.send";
                case AmoAgentActionKind.Publish: return "publish.execute";
                case AmoAgentActionKind.Purchase: return "purchase.execute";
                case AmoAgentActionKind.Transfer: return "transfer.execute";
                case AmoAgentActionKind.Delete: return "delete.execute";
                case AmoAgentActionKind.InstallSoftware: return "software.install";
                case AmoAgentActionKind.ChangePermissions: return "permission.change";
                case AmoAgentActionKind.AdministrativeAction: return "admin.execute";
                default: return "";
            }
        }

        static bool IsAlwaysSensitive(AmoAgentActionKind kind)
        {
            return kind==AmoAgentActionKind.SendInformation ||
                   kind==AmoAgentActionKind.Publish ||
                   kind==AmoAgentActionKind.Purchase ||
                   kind==AmoAgentActionKind.Transfer ||
                   kind==AmoAgentActionKind.Delete ||
                   kind==AmoAgentActionKind.InstallSoftware ||
                   kind==AmoAgentActionKind.ChangePermissions ||
                   kind==AmoAgentActionKind.AdministrativeAction;
        }

        static AmoAgentDecision Allow()
        {
            return new AmoAgentDecision{Allowed=true,RequiresApproval=false,Code="allowed",Reason="Acci\u00F3n permitida dentro de los l\u00EDmites."};
        }
        static AmoAgentDecision Approval(string code,string reason)
        {
            return new AmoAgentDecision{Allowed=false,RequiresApproval=true,Code=code,Reason=reason};
        }
        static AmoAgentDecision Deny(string code,string reason)
        {
            return new AmoAgentDecision{Allowed=false,RequiresApproval=false,Code=code,Reason=reason};
        }
    }
}

