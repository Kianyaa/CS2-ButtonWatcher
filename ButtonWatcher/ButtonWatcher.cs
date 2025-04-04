using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;


namespace ButtonWatcher;

public class ButtonWatcherPlugin : BasePlugin
{
    public override string ModuleName => "ButtonWatcher";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "石 and Kianya";
    public override string ModuleDescription => "Watcher func_button and trigger_once when player trigger";

    private CEntityIdentity? _entity = null;
    private string? _playerTeam = null;
    private int _minutes = 0;
    private int _seconds = 0;
    private string? _playerName = null;
    private ulong _steamId = 0;
    private int _userId = 0;
    private string? _entityName = null;
    private uint _entityIndex = 0;

    public override void Load(bool hotReload)
    {
        HookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
        HookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);

        RegisterEventHandler<EventGameMessage>(OnEventMessageChat);

    }

    // Hook entity output to detect player interaction
    private HookResult OnEntityTriggered(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        // Validate activator
        if (activator == null || !activator.IsValid)
            return HookResult.Continue;

        // Get the player who triggered the entity
        var pawnIdx = (int)activator.Index;
        var playerPawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>(pawnIdx);
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.OriginalController == null || !playerPawn.OriginalController.IsValid)
            return HookResult.Continue;

        // Get the player controller
        var playerController = playerPawn.OriginalController.Value;
        if (playerController == null || !playerController.IsValid)
            return HookResult.Continue;

        // Log interaction
        LogPlayerInteraction(playerController, caller);

        return HookResult.Continue;
    }

    // Log player interaction with the entity
    private void LogPlayerInteraction(CCSPlayerController playerController, CEntityInstance caller)
    {
        // Validate caller
        _entity = caller.Entity;
        if (_entity == null) return;

        // Get player information
        _playerName = playerController.PlayerName;
        _steamId = playerController.SteamID;
        _userId = (int)playerController?.UserId!;
        _entityName = _entity.Name;
        _entityIndex = caller.Index;
        _playerTeam = playerController?.Team.ToString() == "CounterTerrorist" ? "Human" : "Zombie";

        // Get game rules safely
        var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        if (gameRulesProxy?.GameRules == null) return;

        // Get game rules
        var gameRules = gameRulesProxy.GameRules;

        // Ignore detect if the round just started within 2 seconds
        var timeSinceRoundStart = (int)(gameRules.LastThinkTime - gameRules.RoundStartTime);
        if (timeSinceRoundStart <= 2) return;

        // Ignore if the round is in warmup
        var isWarmup = gameRules.WarmupPeriod;
        if (isWarmup == true) return;

        // Get time left in the round
        var timeLeft = gameRules.RoundTime - timeSinceRoundStart;
        _minutes = timeLeft / 60;
        _seconds = timeLeft % 60;

        // Print to chat and log

        //if (_entity.DesignerName == "func_button")
        //{
        //    Server.PrintToChatAll($" {ChatColors.White}[{ChatColors.Yellow}{_playerTeam}{ChatColors.White}][{ChatColors.LightRed}{_minutes:0}:{_seconds:00}{ChatColors.White}]{ChatColors.Lime}{_playerName}{ChatColors.White}[{ChatColors.Orange}{_steamId}{ChatColors.White}][{ChatColors.Lime}#{_userId}{ChatColors.White}] pressed button {ChatColors.LightRed}{_entityName}[#{_entityIndex}]");

        //}

        if (_entity.DesignerName == "trigger_once")
        {
            Server.PrintToChatAll($" {ChatColors.White}[{ChatColors.Yellow}{_playerTeam}{ChatColors.White}][{ChatColors.LightRed}{_minutes:0}:{_seconds:00}{ChatColors.White}]{ChatColors.Lime}{_playerName}{ChatColors.White}[{ChatColors.Orange}{_steamId}{ChatColors.White}][{ChatColors.Lime}#{_userId}{ChatColors.White}] touched trigger {ChatColors.LightRed}{_entityName}[#{_entityIndex}]");
        }

        Logger.LogInformation($"[{_playerTeam}] [{_minutes:0}:{_seconds:00}] {_playerName}[{_steamId}][#{_userId}] triggered {_entityName}[#{_entityIndex}]!");
    }

    // Hook player chat event to detect func_button class that is used (map items)
    public HookResult OnEventMessageChat(EventGameMessage @event, GameEventInfo info)
    {

        Logger.LogInformation($"@event?.Text: {@event?.Text}");

        if (@event?.Text == null)
        {
            Server.PrintToChatAll($"@event?.Text is {@event?.Text}");
            Logger.LogInformation($"@event?.Text is {@event?.Text}");

            return HookResult.Continue;
        }

        if (!@event.Text.Contains("has used"))
        {

            Server.PrintToChatAll($"!@event.Text.Contains(\"has used\") is {!@event.Text.Contains("has used")}");

            if (_entity?.DesignerName == "func_button")
            {
                Server.PrintToChatAll($" {ChatColors.White}[{ChatColors.Yellow}{_playerTeam}{ChatColors.White}][{ChatColors.LightRed}{_minutes:0}:{_seconds:00}{ChatColors.White}]{ChatColors.Lime}{_playerName}{ChatColors.White}[{ChatColors.Orange}{_steamId}{ChatColors.White}][{ChatColors.Lime}#{_userId}{ChatColors.White}] pressed button {ChatColors.LightRed}{_entityName}[#{_entityIndex}]");
                Logger.LogInformation($" {ChatColors.White}[{ChatColors.Yellow}{_playerTeam}{ChatColors.White}][{ChatColors.LightRed}{_minutes:0}:{_seconds:00}{ChatColors.White}]{ChatColors.Lime}{_playerName}{ChatColors.White}[{ChatColors.Orange}{_steamId}{ChatColors.White}][{ChatColors.Lime}#{_userId}{ChatColors.White}] pressed button {ChatColors.LightRed}{_entityName}[#{_entityIndex}]");

                return HookResult.Continue;
            }
        }

        Server.PrintToChatAll($"Just to HookResult.Continue");
        Logger.LogInformation($"Just to HookResult.Continue");
        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        UnhookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
        UnhookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);
    }
}
