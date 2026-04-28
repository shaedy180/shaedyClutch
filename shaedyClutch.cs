using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using ShaedyHudManager;

namespace shaedyClutch;

public class ClutchState
{
    public CCSPlayerController? Clutcher { get; set; }
    public int EnemyCount { get; set; }
    public bool IsActive { get; set; }
    public bool HasWon { get; set; }
    public bool HasAnnounced { get; set; }
}

public class ShaedyClutchAnnouncer : BasePlugin
{
    public override string ModuleName => "shaedy Clutch Announcer";
    public override string ModuleVersion => "1.3.0";
    public override string ModuleAuthor => "shaedy";

    private ClutchState _clutchState = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _hudTimer;

    private string ChatPrefix => string.Concat(ChatColors.White, "[", ChatColors.Green, "shaedy-Clutch", ChatColors.White, "]");

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventBombDefused>(OnBombDefused);
        RegisterEventHandler<EventBombExploded>(OnBombExploded);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ResetClutchState();
        StopHudTimer();
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        StopHudTimer();
        if (_clutchState.IsActive && _clutchState.Clutcher != null)
        {
            int winnerTeam = @event.Winner;
            bool clutcherWon = _clutchState.Clutcher.TeamNum == winnerTeam;
            ShowClutchResult(_clutchState.Clutcher, _clutchState.EnemyCount, clutcherWon);
        }
        ResetClutchState();
        return HookResult.Continue;
    }

    private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
    {
        if (_clutchState.IsActive && _clutchState.Clutcher != null && @event.Userid == _clutchState.Clutcher)
        {
            _clutchState.HasWon = true;
        }
        return HookResult.Continue;
    }

    private HookResult OnBombExploded(EventBombExploded @event, GameEventInfo info)
    {
        if (_clutchState.IsActive && _clutchState.Clutcher != null)
        {
            _clutchState.HasWon = false;
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        AddTimer(0.15f, () =>
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
                TriggerClutchState(lastT, ctAlive);
            else if (ctAlive == 1 && tAlive >= 1 && lastCT != null)
                TriggerClutchState(lastCT, tAlive);
            else if ((tAlive == 0 || ctAlive == 0) && _clutchState.IsActive)
            {
                _clutchState.IsActive = false;
            }
        });

        return HookResult.Continue;
    }

    private void TriggerClutchState(CCSPlayerController clutcher, int enemyCount)
    {
        bool isSameClutch = _clutchState.IsActive && _clutchState.Clutcher == clutcher;

        if (!isSameClutch)
        {
            _clutchState = new ClutchState
            {
                Clutcher = clutcher,
                EnemyCount = enemyCount,
                IsActive = true,
                HasWon = false,
                HasAnnounced = false
            };
        }
        else
        {
            _clutchState.EnemyCount = enemyCount;
        }

        if (!_clutchState.HasAnnounced)
        {
            _clutchState.HasAnnounced = true;
            ShowClutchAnnouncement(clutcher, enemyCount);
        }

        if (_hudTimer == null)
            StartHudTimer();
    }

    private void ShowClutchAnnouncement(CCSPlayerController clutcher, int enemyCount)
    {
        string clutchLabel = "1v" + enemyCount + " CLUTCH";

        string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'>";
        html += "<div style='font-size:14px;color:#aaa;letter-spacing:3px;'>CLUTCH SITUATION</div>";
        html += "<div style='font-size:40px;font-weight:bold;color:#ff4444;text-shadow:0 0 30px #ff4444,0 0 60px #ff0000;margin-top:4px;'>" + clutchLabel + "</div>";
        html += "<div style='font-size:16px;color:#ffffff;margin-top:4px;'><span style='color:#4ade80;'>" + clutcher.PlayerName + "</span> is alone!</div>";
        html += "</div></body></html>";

        var allPlayers = Utilities.GetPlayers();
        foreach (var p in allPlayers)
        {
            if (p != clutcher && p.IsValid && !p.IsBot)
                HudManager.Show(p.SteamID, html, HudPriority.Critical, 4);
        }

        clutcher.PrintToChat(ChatPrefix + " " + ChatColors.Red + "YOU ARE ALONE! " + ChatColors.White + "Win this 1v" + enemyCount + "!");

        var allPlayers2 = Utilities.GetPlayers();
        foreach (var p in allPlayers2)
        {
            if (p != clutcher)
                p.PrintToChat(ChatPrefix + " " + ChatColors.Green + clutcher.PlayerName + " " + ChatColors.White + "is clutching a " + ChatColors.Red + "1v" + enemyCount + ChatColors.White + "!");
        }
    }

    private void StartHudTimer()
    {
        _hudTimer?.Kill();
        _hudTimer = AddTimer(1.0f, OnHudTick, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

    private void StopHudTimer()
    {
        _hudTimer?.Kill();
        _hudTimer = null;
    }

    private void OnHudTick()
    {
        if (!_clutchState.IsActive || _clutchState.Clutcher == null || !_clutchState.Clutcher.IsValid)
            return;

        var clutcher = _clutchState.Clutcher;
        int enemies = _clutchState.EnemyCount;

        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
        if (gameRules == null) return;

        float timeRemaining = gameRules.RoundTime - (Server.CurrentTime - gameRules.RoundStartTime);
        if (timeRemaining < 0) timeRemaining = 0;

        int minutes = (int)timeRemaining / 60;
        int seconds = (int)timeRemaining % 60;
        string timeStr = minutes + ":" + seconds.ToString("D2");

        string timerColor = timeRemaining <= 15 ? "#ff4444" : timeRemaining <= 30 ? "#ffaa00" : "#4ade80";
        string enemyColor = enemies >= 3 ? "#ff4444" : enemies >= 2 ? "#ffaa00" : "#ff8800";

        string enemyDotsHtml = "";
        for (int i = 0; i < 5; i++)
        {
            string dotColor = i < enemies ? enemyColor : "#333";
            enemyDotsHtml += "<span style='font-size:18px;color:" + dotColor + ";margin:0 2px;'>*</span>";
        }

        string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'>";
        html += "<div style='font-size:12px;color:#888;letter-spacing:2px;'>CLUTCH</div>";
        html += "<div style='font-size:28px;font-weight:bold;color:" + enemyColor + ";text-shadow:0 0 10px " + enemyColor + ";margin-top:2px;'>" + enemyDotsHtml + "</div>";
        html += "<div style='font-size:14px;color:#aaa;margin-top:2px;'>" + enemies + " opponent" + (enemies != 1 ? "s" : "") + " remaining</div>";
        html += "<div style='font-size:22px;font-weight:bold;color:" + timerColor + ";text-shadow:0 0 10px " + timerColor + ";margin-top:4px;'>" + timeStr + "</div>";
        html += "</div></body></html>";

        HudManager.Show(clutcher.SteamID, html, HudPriority.Critical, 1);

        var allPlayers = Utilities.GetPlayers();
        foreach (var p in allPlayers)
        {
            if (p != clutcher && p.IsValid && !p.IsBot && p.PawnIsAlive)
            {
                string specHtml = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'>";
                specHtml += "<div style='font-size:12px;color:#888;letter-spacing:2px;'><span style='color:#4ade80;'>" + clutcher.PlayerName + "</span> clutching 1v" + enemies + "</div>";
                specHtml += "<div style='font-size:18px;font-weight:bold;color:" + timerColor + ";margin-top:2px;'>" + timeStr + "</div>";
                specHtml += "</div></body></html>";
                HudManager.Show(p.SteamID, specHtml, HudPriority.Critical, 1);
            }
        }
    }

    private void ShowClutchResult(CCSPlayerController clutcher, int enemyCount, bool won)
    {
        string resultSymbol = won ? ">" : "X";
        string color = won ? "#4ade80" : "#f87171";
        string label = won ? "CLUTCH WON" : "CLUTCH FAILED";

        string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'>";
        html += "<div style='font-size:36px;font-weight:bold;color:" + color + ";text-shadow:0 0 20px " + color + ";'>" + resultSymbol + " " + label + "</div>";
        html += "<div style='font-size:16px;color:#ccc;margin-top:4px;'>" + clutcher.PlayerName + " - 1v" + enemyCount + "</div>";
        html += "</div></body></html>";

        var allPlayers = Utilities.GetPlayers();
        foreach (var p in allPlayers)
        {
            if (p.IsValid && !p.IsBot)
                HudManager.Show(p.SteamID, html, HudPriority.Critical, 4);
        }
    }

    private void ResetClutchState()
    {
        _clutchState = new ClutchState();
    }
}
