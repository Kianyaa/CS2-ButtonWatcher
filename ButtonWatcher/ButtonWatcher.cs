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
        HookEntityOutput("func_button", "OnPressed", OnPlayerPressedButton, HookMode.Post);
        HookEntityOutput("trigger_once", "OnStartTouch", OnPlayerStartTouch, HookMode.Post);
    }

    // Class func_button usually is a button that can be pressed by players on the map.
    public HookResult OnPlayerPressedButton(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        // Get the entity of the button
        var buttonEntity = caller.Entity;
        if (buttonEntity == null)
            return HookResult.Continue;

        // Get the player who pressed the button
        var pawnIdx = (int)activator.Index;
        var playerPawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>(pawnIdx);

        // Check if the player is valid
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.OriginalController == null || !playerPawn.OriginalController.IsValid)
            return HookResult.Continue;

        // Get the player controller
        CCSPlayerController? playerController = playerPawn.OriginalController.Value;

        // Check if the player controller is valid
        if (playerController == null || !playerController.IsValid)
            return HookResult.Continue;

        // Get the player's name, Steam ID, User ID, button entity name, and button entity index
        var sPlayerName = playerController.PlayerName;
        var sSteamID = playerController.SteamID;
        var sUserID = playerController.UserId;
        var sButtonEntityName = buttonEntity.Name;
        var iButtonEntityIndex = caller.Index;
        var sPlayerTeam = playerController.Team.ToString();

        // Convert the player's team
        sPlayerTeam = sPlayerTeam == "CounterTerrorist" ? "Human" : "Zombie";

        // Get the game rules
        var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

        // Get the time left in the round
        var iTime = gameRules.RoundTime - (int)(gameRules.LastThinkTime - gameRules.RoundStartTime);
        var iMin = iTime / 60;
        var iSec = iTime % 60;

        Server.PrintToChatAll($" {ChatColors.White}[{ChatColors.Yellow}{sPlayerTeam}{ChatColors.White}][{ChatColors.LightRed}{iMin}:{iSec}{ChatColors.White}]{ChatColors.Lime}{sPlayerName}{ChatColors.White}[{ChatColors.Orange}{sSteamID}{ChatColors.White}][{ChatColors.Lime}#{sUserID}{ChatColors.White}] triggered {ChatColors.LightRed}{sButtonEntityName}[#{iButtonEntityIndex}] {ChatColors.White}this button!");
        Logger.LogInformation($"[{sPlayerTeam}] [{iMin}:{iSec}]{sPlayerName}[{sSteamID}][#{sUserID}] triggered {sButtonEntityName}[#{iButtonEntityIndex}] this button!");

        return HookResult.Continue;
    }

    // Class trigger_once usually is teleportation, invisible wall or platform etc.
    public HookResult OnPlayerStartTouch(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        // Get the entity of the button
        CEntityIdentity? TouchEntity = caller.Entity;
        if (TouchEntity == null)
            return HookResult.Continue;

        // Get the player who touched the button
        int pawnIdx = (int)activator.Index;
        CCSPlayerPawn? playerPawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>(pawnIdx);
        if (playerPawn == null || !playerPawn.IsValid || playerPawn.OriginalController == null || !playerPawn.OriginalController.IsValid)
            return HookResult.Continue;

        // Get the player controller is valid
        CCSPlayerController? playerController = playerPawn.OriginalController.Value;

        if (playerController == null || !playerController.IsValid)
            return HookResult.Continue;

        // Get the player's name, Steam ID, User ID, button entity name, and button entity index
        string sPlayerName = playerController.PlayerName;
        ulong sSteamID = playerController.SteamID;
        int? sUserID = playerController.UserId;
        string sButtonEntityName = TouchEntity.Name;
        uint iButtonEntityIndex = caller.Index;
        var sPlayerTeam = playerController.Team.ToString();

        // Convert the player's team
        sPlayerTeam = sPlayerTeam == "CounterTerrorist" ? "Human" : "Zombie";

        // Get the game rules
        CCSGameRules gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!;

        // Get the time left in the round
        int iTime = gameRules.RoundTime - (int)(gameRules.LastThinkTime - gameRules.RoundStartTime);
        int iMin = iTime / 60;
        int iSec = iTime % 60;

        Server.PrintToChatAll($" {ChatColors.White}[{ChatColors.Yellow}{sPlayerTeam}{ChatColors.White}][{ChatColors.LightRed}{iMin}:{iSec}{ChatColors.White}]{ChatColors.Lime}{sPlayerName}{ChatColors.White}[{ChatColors.Orange}{sSteamID}{ChatColors.White}][{ChatColors.Lime}#{sUserID}{ChatColors.White}] triggered {ChatColors.LightRed}{sButtonEntityName}[#{iButtonEntityIndex}] {ChatColors.White}this!");
        Logger.LogInformation($"[{sPlayerTeam}] [{iMin}:{iSec}]{sPlayerName}[{sSteamID}][#{sUserID}] triggered {sButtonEntityName}[#{iButtonEntityIndex}] this!");

        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        UnhookEntityOutput("func_button", "OnPressed", OnPlayerPressedButton, HookMode.Post);
        UnhookEntityOutput("trigger_once", "OnStartTouch", OnPlayerStartTouch, HookMode.Post);
    }
}