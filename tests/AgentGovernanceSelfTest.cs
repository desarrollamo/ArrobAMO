using System;
using System.Collections.Generic;
using ArrobAMO;

class AgentGovernanceSelfTest
{
    static int fails=0;
    static void Ok(bool v,string n){Console.WriteLine((v?"PASS ":"FAIL ")+n);if(!v)fails++;}
    static AmoAgent BaseAgent()
    {
        var a=new AmoAgent();
        a.Id="a1";a.Name="QA";a.Enabled=true;a.Autonomy=AmoAutonomyLevel.ReversibleActions;
        a.MaxActions=2;a.MaxMinutes=60;a.MaxCostUsd=2m;
        a.Permissions.Add("navigation.use");
        a.Permissions.Add("form.write");
        a.Permissions.Add("data.send");
        a.AllowedDomains.Add("example.com");
        a.AllowedTools.Add("browser");
        return a;
    }
    static void Main()
    {
        var org=new AmoOrganization();
        var agent=BaseAgent();
        var run=new AmoAgentRunState();

        var nav=new AmoAgentAction{AgentId="a1",Kind=AmoAgentActionKind.Navigate,Domain="example.com",Tool="browser",Reversible=true,EstimatedCostUsd=0.1m};
        var d=AmoAgentGovernor.Evaluate(org,agent,nav,run);
        Ok(d.Allowed && !d.RequiresApproval,"Nivel 2 permite navegación reversible");
        AmoAgentGovernor.RegisterExecuted(run,nav);
        Ok(run.ActionsExecuted==1,"Contador de acciones");

        var badDomain=new AmoAgentAction{AgentId="a1",Kind=AmoAgentActionKind.Navigate,Domain="blocked.test",Tool="browser",Reversible=true};
        d=AmoAgentGovernor.Evaluate(org,agent,badDomain,run);
        Ok(!d.Allowed && d.Code=="agent.domain_blocked","Bloqueo por dominio");

        var send=new AmoAgentAction{AgentId="a1",Kind=AmoAgentActionKind.SendInformation,Domain="example.com",Tool="browser",Reversible=false};
        d=AmoAgentGovernor.Evaluate(org,agent,send,run);
        Ok(!d.Allowed && d.RequiresApproval,"Enviar información requiere aprobación");

        agent.Autonomy=AmoAutonomyLevel.RecommendOnly;
        d=AmoAgentGovernor.Evaluate(org,agent,nav,run);
        Ok(d.RequiresApproval && d.Code=="agent.level0","Nivel 0 no ejecuta");

        agent.Autonomy=AmoAutonomyLevel.SupervisedAutonomy;
        org.AgentKillSwitch=true;
        d=AmoAgentGovernor.Evaluate(org,agent,nav,run);
        Ok(!d.Allowed && !d.RequiresApproval && d.Code=="agent.kill_switch","Kill switch bloquea");

        org.AgentKillSwitch=false;run.ActionsExecuted=2;
        d=AmoAgentGovernor.Evaluate(org,agent,nav,run);
        Ok(d.Code=="agent.max_actions","Máximo de acciones");

        run.ActionsExecuted=0;run.CostUsd=1.95m;
        d=AmoAgentGovernor.Evaluate(org,agent,nav,run);
        Ok(d.Code=="agent.max_cost","Máximo de coste");

        Console.WriteLine("FAILS="+fails);
        Environment.ExitCode=fails==0?0:1;
    }
}

