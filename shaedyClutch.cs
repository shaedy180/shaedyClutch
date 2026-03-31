using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System.Linq;

namespace shaedyClutch;

public class ShaedyClutchAnnouncer : BasePlugin
{
    public override string ModuleName => "shaedy Clutch Announcer";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "shaedy";

    // Hardcoded prefix
    private string ChatPrefix => $"{ChatColors.White}[{ChatColors.Green}shaedy-Clutch{ChatColors.White}]";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var deadPlayer = @event.Userid;

        // Short delay to make sure the death is fully registered
        AddTimer(0.1f, () =>
        {
            var players = Utilities.GetPlayers();

            int tAlive = 0;
            int ctAlive = 0;
            CCSPlayerController? lastT = null;
            CCSPlayerController? lastCT = null;

            foreach (var p in players)
            {
                if (p.IsValid && p.PawnIsAlive)
                {
                    if (p.TeamNum == (byte)CsTeam.Terrorist)
                    {
                        tAlive++;
                        lastT = p;
                    }
                    else if (p.TeamNum == (byte)CsTeam.CounterTerrorist)
                    {
                        ctAlive++;
                        lastCT = p;
                    }
                }
            }

            if (tAlive == 1 && ctAlive >= 1 && lastT != null)
                TriggerClutchMessage(lastT, ctAlive);
            else if (ctAlive == 1 && tAlive >= 1 && lastCT != null)
                TriggerClutchMessage(lastCT, tAlive);
        });

        return HookResult.Continue;
    }

    private void TriggerClutchMessage(CCSPlayerController clutcher, int enemyCount)
    {
        clutcher.PrintToChat($"{ChatPrefix} {ChatColors.Red}YOU ARE ALONE! {ChatColors.White}Win this 1v{enemyCount}!");

        var allPlayers = Utilities.GetPlayers();
        foreach (var p in allPlayers)
        {
            if (p != clutcher)
                p.PrintToChat($"{ChatPrefix} {ChatColors.Green}{clutcher.PlayerName} {ChatColors.White}is clutching a {ChatColors.Red}1v{enemyCount}{ChatColors.White}!");
        }
    }
}