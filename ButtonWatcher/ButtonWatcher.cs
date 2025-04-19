using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Text;
using System.Text.Json;


namespace ButtonWatcher
{
    public class ButtonWatcherPlugin : BasePlugin
    {
        public override string ModuleName => "ButtonWatcher";
        public override string ModuleVersion => "1.1.1";
        public override string ModuleAuthor => "石 and Kianya";
        public override string ModuleDescription => "Watcher func_button and trigger_once when player trigger";

        private List<string> _itemNames = new List<string?>()!;
        private bool _toggle;

        public override void Load(bool hotReload)
        {
            HookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
            HookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);
        }

        private HookResult OnEntityTriggered(CEntityIOOutput output, string name, CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
        {
            if (activator == null || !activator.IsValid)
                return HookResult.Continue;

            var playerPawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>((int)activator.Index);
            if (playerPawn == null || !playerPawn.IsValid || playerPawn.OriginalController?.IsValid != true)
                return HookResult.Continue;

            var playerController = playerPawn.OriginalController.Value;
            if (playerController != null) LogPlayerInteraction(playerController, caller);

            return HookResult.Continue;
        }

        private void LogPlayerInteraction(CCSPlayerController playerController, CEntityInstance caller)
        {
            var entity = caller?.Entity;
            if (entity == null) return;

            var playerName = playerController.PlayerName;
            var steamId = playerController.SteamID.ToString();
            var userId = (int)playerController.UserId!;

            var entityName = entity.Name;

            if (entityName != null)
            {
                // Entity Have Name
            }
            else
            {
                // Named Entity assume they all button for trigger
                entityName = "trigger_button";
            }

            if (caller != null)
            {
                var entityIndex = (int)caller.Index;
                var playerTeam = playerController.Team.ToString() == "CounterTerrorist" ? "Human" : "Zombie";

                var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
                if (gameRulesProxy?.GameRules == null) return;

                var gameRules = gameRulesProxy.GameRules;
                var timeSinceRoundStart = (int)(gameRules.LastThinkTime - gameRules.RoundStartTime);

                if (timeSinceRoundStart <= 2 || gameRules.WarmupPeriod == true) return;

                var timeLeft = gameRules.RoundTime - timeSinceRoundStart;
                var minutes = timeLeft / 60;
                var seconds = timeLeft % 60;

                var touchText = $"{playerName} just touched the trigger!";
                var buttonText = $"{playerName} just triggered the button!";

                _toggle = true;
                _itemNames = _itemNames
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!.Trim().ToLower())
                    .Distinct()
                    .ToList();

                if (entity.DesignerName == "func_button")
                {
                    if (_itemNames.Any(word => entityName.ToLower().Split("_").Contains(word)))
                    {
                        _toggle = false;
                    }

                    if (_toggle)
                    {
                        PrintToChatAll(playerName, steamId, userId, entityName, entityIndex, playerTeam, minutes, seconds, "pressed button");
                    }
                }
                else if (entity.DesignerName == "trigger_once")
                {
                    if (_itemNames.Any(word => entityName.ToLower().Split("_").Contains(word)))
                    {
                        // When Zombie touch the trigger(item), set _toggle to false and not show
                        if (playerController.Team == CsTeam.Terrorist)
                        {
                            _toggle = false;
                        }
                    }

                    if (_toggle)
                    {
                        PrintToChatAll(playerName, steamId, userId, entityName, entityIndex, playerTeam, minutes, seconds, "touched trigger");
                    }
                }
                Logger.LogInformation($"[{playerTeam}] [{minutes:0}:{seconds:00}] {playerName}[{steamId}][#{userId}] triggered {entityName}[#{entityIndex}]!");
            }
        }

        private void PrintToChatAll(string playerName, string steamId, int userId, string entityName, int entityIndex, string playerTeam, int minutes, int seconds, string action)
        {
            var message = new StringBuilder();
            message.AppendFormat($" {ChatColors.White}[{ChatColors.Yellow}{playerTeam}{ChatColors.White}][{ChatColors.LightRed}{minutes:0}:{seconds:00}{ChatColors.White}]");
            message.AppendFormat($"{ChatColors.Lime}{playerName}{ChatColors.White}[{ChatColors.Orange}{steamId}{ChatColors.White}][{ChatColors.Lime}#{userId}{ChatColors.White}] {action} {ChatColors.LightRed}{entityName}[#{entityIndex}]");
            Server.PrintToChatAll(message.ToString());
        }

        // When new map load, clear the _itemNames list and load the new map config
        [GameEventHandler(HookMode.Post)]
        public HookResult OnEventWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
        {
            _itemNames.Clear();

            string[] jsonFiles = Directory.GetFiles("../../csgo/addons/counterstrikesharp/configs/entwatch/maps", "*.jsonc");

            foreach (var file in jsonFiles)
            {
                if (Path.GetFileNameWithoutExtension(file) != Server.MapName) continue;

                //Server.PrintToChatAll($"[ButtonWatcher] Loaded config for map: {Server.MapName}");

                string jsonContent = RemoveComments(File.ReadAllText(file));
                using JsonDocument doc = JsonDocument.Parse(jsonContent);

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("name", out JsonElement nameElement))
                    {

                        string? name = nameElement.GetString()?.ToLower();
                        if (!string.IsNullOrWhiteSpace(name))
                            _itemNames.AddRange(name.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                    }
                }

                _itemNames.AddRange(new List<string> { "item", "human", "weapon" });

                break;

            }

            _itemNames = _itemNames
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLower())
                .Distinct()
                .ToList();

            return HookResult.Continue;
        }

        // Remove comments from the JSON file when the file is loaded (when map warm up end)
        private static string RemoveComments(string input)
        {
            var lines = input.Split('\n');
            var cleanLines = new List<string>();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("//"))
                    cleanLines.Add(line);
            }
            return string.Join("\n", cleanLines);
        }

        // This method is called when the plugin is unloaded
        public override void Unload(bool hotReload)
        {
            UnhookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
            UnhookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);
        }
    }
}