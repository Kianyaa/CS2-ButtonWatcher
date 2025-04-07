using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Text;
using System.Text.Json;


namespace ButtonWatcher
{
    public class ButtonWatcherPlugin : BasePlugin
    {
        public override string ModuleName => "ButtonWatcher";
        public override string ModuleVersion => "1.0.1";
        public override string ModuleAuthor => "石 and Kianya";
        public override string ModuleDescription => "Watcher func_button and trigger_once when player trigger";

        private const float Time = 4.00f;
        private const float Height = 20.0f; 
        private const float Range = -50.0f;
        private const bool Follow = true;
        private const bool ShowOffScreen = true;
        private const string IconOnScreen = "icon_alert_red";
        private const string IconOffScreen = "icon_alert";
        private const string Cmd = "use_binding";
        private const bool ShowTextAlways = false;
        private readonly Color _color = Color.FromArgb(255, 255, 0, 0);

        private CEnvInstructorHint? _entity;
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

                    // For Debugging

                    //Server.PrintToChatAll("---_itemNames---"); // 

                    //foreach (var itemNames in _itemNames)
                    //{
                    //    Server.PrintToChatAll(itemNames);
                    //}

                    //Server.PrintToChatAll("---entityName---"); // 

                    //foreach (var entityNames in entityName.ToLower().Split("_"))
                    //{
                    //    Server.PrintToChatAll(entityNames);
                    //}


                    if (_itemNames.Any(word => entityName.ToLower().Split("_").Contains(word)))
                    {
                        _toggle = false;
                    }

                    if (_toggle)
                    {
                        PrintToChatAll(playerName, steamId, userId, entityName, entityIndex, playerTeam, minutes, seconds, "pressed button");

                        Server.NextFrame(() =>
                        {
                            foreach (var player in Utilities.GetPlayers())
                            {
                                DisplayInstructorHint(player, Time, Height, Range, Follow, ShowOffScreen, IconOnScreen, IconOffScreen, Cmd, ShowTextAlways, _color, buttonText);
                            }
                        });
                    }
                }
                else if (entity.DesignerName == "trigger_once")
                {
                    PrintToChatAll(playerName, steamId, userId, entityName, entityIndex, playerTeam, minutes, seconds, "touched trigger");

                    Server.NextFrame(() =>
                    {
                        foreach (var player in Utilities.GetPlayers())
                        {

                            DisplayInstructorHint(player, Time, Height, Range, Follow, ShowOffScreen, IconOnScreen, IconOffScreen, Cmd, ShowTextAlways, _color, touchText);
                        }
                    });
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

        private void DisplayInstructorHint(CCSPlayerController player, float time, float height, float range, bool follow, bool showOffScreen, string iconOnScreen, string iconOffScreen, string cmd, bool showTextAlways, Color color, string text)
        {

            if (_entity == null || !_entity.IsValid)
            {
                _entity = Utilities.CreateEntityByName<CEnvInstructorHint>("env_instructor_hint");
                if (_entity == null) return;
            }

            var entity = _entity;
            if (entity == null) return;

            entity.Target = player.Index.ToString();
            entity.HintTargetEntity = player.Index.ToString();
            entity.Static = follow;
            entity.Timeout = (int)time;
            entity.IconOffset = height;
            entity.Range = range;
            entity.NoOffscreen = showOffScreen;
            entity.Icon_Onscreen = iconOnScreen;
            entity.Icon_Offscreen = iconOffScreen;
            entity.Binding = cmd;
            entity.ForceCaption = showTextAlways;
            entity.Color = color;
            entity.Caption = text.Replace("\n", " ");

            entity.DispatchSpawn();
            entity.AcceptInput("ShowHint");
        }

        // This method is called when the round starts and give all player enable sv_gameinstructor_enable
        [GameEventHandler(HookMode.Post)]
        public HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
        {

            foreach (var player in Utilities.GetPlayers())
            {
                player.ReplicateConVar("sv_gameinstructor_enable", "true");
                player.ReplicateConVar("gameinstructor_enable", "true");
            }

            return HookResult.Continue;
        }

        // When new map load, clear the _itemNames list and load the new map config
        [GameEventHandler(HookMode.Post)]
        public HookResult OnEventWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
        {
            _itemNames.Clear();

            string[] jsonFiles = Directory.GetFiles("../../csgo/addons/counterstrikesharp/configs/maps", "*.jsonc");

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