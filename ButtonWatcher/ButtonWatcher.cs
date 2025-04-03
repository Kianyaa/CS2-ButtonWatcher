using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;

namespace ButtonWatcher;

public class ButtonWatcherPlugin : BasePlugin
{
    public override string ModuleName => "ButtonWatcher";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "石 and Kianya";
    public override string ModuleDescription => "Watcher func_button and trigger_once when player trigger";

    public override void Load(bool hotReload)
    {
        HookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
        HookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);
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
        var entity = caller.Entity;
        if (entity == null) return;

        // Get player information
        var playerName = playerController.PlayerName;
        var steamId = playerController.SteamID;
        var userId = playerController.UserId;
        var entityName = entity.Name;
        var entityIndex = caller.Index;
        var playerTeam = playerController.Team.ToString() == "CounterTerrorist" ? "Human" : "Zombie";

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
        var minutes = timeLeft / 60;
        var seconds = timeLeft % 60;

        // Print to chat and log

        if (entity.DesignerName == "func_button")
        {
            Server.PrintToChatAll($" {ChatColors.White}[{ChatColors.Yellow}{playerTeam}{ChatColors.White}][{ChatColors.LightRed}{minutes}:{seconds}{ChatColors.White}]{ChatColors.Lime}{playerName}{ChatColors.White}[{ChatColors.Orange}{steamId}{ChatColors.White}][{ChatColors.Lime}#{userId}{ChatColors.White}] pressed button {ChatColors.LightRed}{entityName}[#{entityIndex}]");

        }

        else if (entity.DesignerName == "trigger_once")
        {
            Server.PrintToChatAll($" {ChatColors.White}[{ChatColors.Yellow}{playerTeam}{ChatColors.White}][{ChatColors.LightRed}{minutes}:{seconds}{ChatColors.White}]{ChatColors.Lime}{playerName}{ChatColors.White}[{ChatColors.Orange}{steamId}{ChatColors.White}][{ChatColors.Lime}#{userId}{ChatColors.White}] touched trigger {ChatColors.LightRed}{entityName}[#{entityIndex}]");
        }


        Logger.LogInformation($"[{playerTeam}] [{minutes}:{seconds}] {playerName}[{steamId}][#{userId}] triggered {entityName}[#{entityIndex}]!");
    }

    public override void Unload(bool hotReload)
    {
        UnhookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
        UnhookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);
    }
}
