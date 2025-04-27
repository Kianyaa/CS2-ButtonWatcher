using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Commands;
using System.Linq;


namespace ButtonWatcher
{
    public class ButtonWatcherPlugin : BasePlugin
    {
        public override string ModuleName => "ButtonWatcher";
        public override string ModuleVersion => "1.1.1";
        public override string ModuleAuthor => "石 and Kianya";
        public override string ModuleDescription => "Watcher func_button and trigger_once when player trigger";

        private const float Time = 3.00f;
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
        private List<int> _EntityIndex = new List<int>()!;
        private bool _toggle;

        private List<CCSPlayerController?> _playerList = new List<CCSPlayerController?>();

        // Time
        //float _currentTimeButton = 0;
        float _currentTimeTouch = 0;

        // playerName
        //private string? _playerName = "";

        public override void Load(bool hotReload)
        {
            HookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
            HookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);


        }
        public override void Unload(bool hotReload)
        {
            UnhookEntityOutput("func_button", "OnPressed", OnEntityTriggered, HookMode.Post);
            UnhookEntityOutput("trigger_once", "OnStartTouch", OnEntityTriggered, HookMode.Post);


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
                        if (_EntityIndex.Contains(entityIndex))
                        {
                            return;
                        }

                        //// Prevent player from triggering the button multiple times
                        //if (_currentTimeTouch + 1 >= Server.CurrentTime)
                        //{
                        //    return;
                        //}

                        //// Prevent player from triggering the button multiple times with the same name
                        //if (playerName == _playerName && _currentTimeTouch + 5 >= Server.CurrentTime)
                        //{
                        //    return;
                        //}

                        //_playerName = playerName;
                        //_currentTimeTouch = Server.CurrentTime;

                        PrintToChatAll(playerName, steamId, userId, entityName, entityIndex, playerTeam, minutes, seconds, "pressed");

                        Server.NextFrame(() =>
                        {
                            DisplayInstructorHint(playerController, Time, Height, Range, Follow, ShowOffScreen, IconOnScreen, IconOffScreen, Cmd, ShowTextAlways, _color, buttonText);
                        });

                        _EntityIndex.Add(entityIndex);
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
                        if (_currentTimeTouch + 1 >= Server.CurrentTime)
                        {
                            return;
                        }

                        _currentTimeTouch = Server.CurrentTime;

                        PrintToChatAll(playerName, steamId, userId, entityName, entityIndex, playerTeam, minutes, seconds, "touched");

                        Server.NextFrame(() =>
                        { 
                            DisplayInstructorHint(playerController, Time, Height, Range, Follow, ShowOffScreen, IconOnScreen, IconOffScreen, Cmd, ShowTextAlways, _color, touchText);
                        });
                    }
                }

                Logger.LogInformation($"[{playerTeam}] [{minutes:0}:{seconds:00}] {playerName}[{steamId}][#{userId}] triggered {entityName}[#{entityIndex}]!");
            }
        }

        private void PrintToChatAll(string playerName, string steamId, int userId, string entityName, int entityIndex, string playerTeam, int minutes, int seconds, string action)
        {
            var message = new StringBuilder();
            message.AppendFormat($" {ChatColors.White}[{ChatColors.Yellow}{playerTeam}{ChatColors.White}][{ChatColors.LightRed}{minutes:0}:{seconds:00}{ChatColors.White}]");
            message.AppendFormat($"{ChatColors.Lime}{playerName}{ChatColors.White}[{ChatColors.Orange}{steamId}{ChatColors.White}] {action} {ChatColors.LightRed}{entityName}[#{entityIndex}]");
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

            string buffer = player.Index.ToString();
            entity.Target = buffer;
            entity.HintTargetEntity = buffer;
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

            entity.DispatchSpawn(null);
            entity.AcceptInput("ShowHint");
        }

        [ConsoleCommand("css_hint", "0 = turn off trigger message on screen, 1 = turn on trigger message on screen")]
        [CommandHelper(minArgs: 1, whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void ToggleHint(CCSPlayerController player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.PawnIsAlive == false) return;

            if (commandInfo.GetArg(1) == "0")
            {

                if (_playerList.Contains(player))
                {
                    player.PrintToChat($" {ChatColors.Green}[ButtonWatcher] {ChatColors.Default}You already turn off the trigger message");

                    return;
                }

                player.ReplicateConVar("sv_gameinstructor_enable", "false");
                player.ReplicateConVar("gameinstructor_enable", "false");
                Server.ExecuteCommand("sv_gameinstructor_enable false");

                _playerList.Add(player);

                player.PrintToChat($" {ChatColors.Green}[ButtonWatcher] {ChatColors.Default}Turn off trigger message on screen");

                return;
            }

            if (commandInfo.GetArg(1) == "1")
            {
                if (_playerList.Contains(player))
                {
                    _playerList.Remove(player);

                    player.PrintToChat($" {ChatColors.Green}[ButtonWatcher] {ChatColors.Default}Turn on trigger message on screen in next round");

                    return;
                }

                player.PrintToChat($" {ChatColors.Green}[ButtonWatcher] {ChatColors.Default}You already turn on trigger message on screen");

            }

            if (string.IsNullOrEmpty(commandInfo.GetArg(1)) || !new[] { "1", "0" }.Contains(commandInfo.GetArg(1)))
            {
                player.PrintToChat($" {ChatColors.Green}[ButtonWatcher] {ChatColors.Default}Invalid input, 0 = disable, 1 = enable");

                return;
            }

        }

        // This method is called when the round starts and give all player enable sv_gameinstructor_enable
        [GameEventHandler(HookMode.Post)]
        public HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            var players =  Utilities.GetPlayers();

            _EntityIndex.Clear();
            //_currentTimeButton = 0;
            _currentTimeTouch = 0;
            //_playerName = "";

            if (players == null) return HookResult.Continue;

            foreach (var player in players)
            {
                if (_playerList.Contains(player))
                {
                    if (player == null || !player.IsValid || !player.PlayerPawn.IsValid ||
                        player.Connected != PlayerConnectedState.PlayerConnected || player.PawnIsAlive == false)
                    {
                        _playerList.Remove(player);

                    }

                    continue;
                }

                if(player == null || !player.IsValid || !player.PlayerPawn.IsValid || player.Connected != PlayerConnectedState.PlayerConnected || player.PawnIsAlive == false) continue;

                player.ReplicateConVar("sv_gameinstructor_enable", "true");
                player.ReplicateConVar("gameinstructor_enable", "true");

            }

            Server.ExecuteCommand("sv_gameinstructor_enable true");

            return HookResult.Continue;
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

                _itemNames.AddRange(new List<string> { "item", "human", "weapon", "magick" });

                break;

            }

            _itemNames = _itemNames
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLower())
                .Distinct()
                .ToList();

            return HookResult.Continue;
        }

        // Hook Event Hint 
        //[GameEventHandler(HookMode.Pre)]
        //public HookResult OnEventHintStart(EventInstructorServerHintCreate @event, GameEventInfo info)
        //{

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


    }
}