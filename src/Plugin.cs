using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Cuphead 3-Player Co-op Mod
    /// Enables stable, balanced 3-player cooperative multiplayer for Cuphead & DLC.
    /// Supports Player 1 (Cuphead), Player 2 (Mugman), and Player 3 (Ms. Chalice / Custom).
    /// </summary>
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.cuphead.coop3p";
        public const string Name = "Cuphead 3-Player Co-op Mod";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        // General settings
        internal static ConfigEntry<int> MaxPlayers;
        internal static ConfigEntry<bool> ShowCoopWatermark;

        // Save synchronization
        internal static ConfigEntry<bool> SyncExtrasToSave;

        // Mr. Wheezy and hazard spawns
        internal static ConfigEntry<bool> FixWheezySpawns;
        internal static ConfigEntry<bool> WheezyFixAllDicePalace;
        internal static ConfigEntry<float> HazardClearance;
        internal static ConfigEntry<float> ExtraSpawnSpacing;

        // Camera and rescue
        internal static ConfigEntry<bool> RescueOffscreenPlayers;
        internal static ConfigEntry<float> ViewportMargin;
        internal static ConfigEntry<float> RescueCooldown;

        // Visual tints
        // Visuals & Typography
        internal static ConfigEntry<int> UIFontMode;
        internal static ConfigEntry<float> UIFontAspectScale;
        internal static ConfigEntry<bool> PersistExtraTints;
        internal static ConfigEntry<string> Player3Color;
        internal static ConfigEntry<string> Player4Color;
        internal static ConfigEntry<int> P3BaseCharacter;

        // DLC transitions
        internal static ConfigEntry<bool> FixAnyPlayerInput;
        internal static ConfigEntry<bool> ParkExtrasForDlc;
        internal static ConfigEntry<bool> GiftChaliceToExtras;

        // Damage scaling
        internal static ConfigEntry<bool> EnableDamageScaling;
        internal static ConfigEntry<string> DamageDifficultyPreset;
        internal static ConfigEntry<float> DamageMultiplier1P;
        internal static ConfigEntry<float> DamageMultiplier2P;
        internal static ConfigEntry<float> DamageMultiplier3P;
        internal static ConfigEntry<float> DamageMultiplier4P;
        internal static ConfigEntry<bool> ScaleByAlivePlayers;
        internal static ConfigEntry<bool> EnableDynamicDefaultScaling;
        internal static ConfigEntry<bool> LogDamageScaling;

        // Boss targeting
        internal static ConfigEntry<bool> FixBossTargeting;
        internal static ConfigEntry<bool> FixChessBossTargeting;
        internal static ConfigEntry<bool> RedirectDeadPlayerAim;
        internal static ConfigEntry<bool> LogTargetSwitches;

        // Overworld map interactions
        internal static ConfigEntry<bool> ExtraPlayersCanInteract;
        internal static ConfigEntry<bool> ExtrasUseMugmanEntities;
        internal static ConfigEntry<bool> VerboseMapInteractions;
        internal static ConfigEntry<bool> MapInteractionWatchdog;
        internal static ConfigEntry<bool> FixChaliceGhostArray;
        internal static ConfigEntry<bool> FixChaliceVictoryBranch;
        internal static ConfigEntry<bool> ForceVictoryCleanup;
        internal static ConfigEntry<bool> RegroupStrandedExtras;
        internal static ConfigEntry<float> MapRegroupDistance;

        // Join gating
        internal static ConfigEntry<bool> BlockJoinOutsideGameplay;
        internal static ConfigEntry<bool> RestoreJoinOnGameplayLoad;
        internal static ConfigEntry<bool> GateTitleScreen;

        // Input and controllers
        internal static ConfigEntry<bool> AssignGamepadsToExtras;
        internal static ConfigEntry<bool> SinglePressJoin;

        // Difficulty and Hardcore
        internal static ConfigEntry<bool> UnlockExpertAlways;
        internal static ConfigEntry<bool> ParryTimeFreeze;
        internal static ConfigEntry<bool> RestartOnGhostLost;
        internal static ConfigEntry<bool> RestartOnlyOnBosses;
        internal static ConfigEntry<float> GhostLostGraceSeconds;
        internal static ConfigEntry<bool> ShowProgressOnGhostLost;
        internal static ConfigEntry<float> GameOverProgressSeconds;

        // Quick Restart
        internal static ConfigEntry<bool> QuickRestartEnabled;
        internal static ConfigEntry<float> QuickRestartHoldDuration;

        // Base health
        internal static ConfigEntry<int> BaseHealthP1;
        internal static ConfigEntry<int> BaseHealthP2;
        internal static ConfigEntry<int> BaseHealthP3;
        internal static ConfigEntry<int> BaseHealthP4;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogMessage("Cuphead 3-Player Co-op Mod: Initializing...");

            Stage("BindConfig", BindConfig);
            Stage("DamageScaling.Init", DamageScaling.InitFromConfig);
            Stage("JoinGate.Init", JoinGate.Init);
            Stage("CoopStatsManager.Init", CoopStatsManager.Init);

            Stage("Harmony.PatchAll", () =>
            {
                _harmony = new Harmony(Guid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
            });

            Stage("FixRunner.Mount", () =>
            {
                var host = new GameObject("Cuphead3P_FixRunner");
                host.AddComponent<FixRunner>();
                DontDestroyOnLoad(host);
            });

            Log.LogMessage("Cuphead 3-Player Co-op Mod: Loaded successfully!");
        }

        private static void Stage(string name, Action action)
        {
            try
            {
                action();
                Log.LogInfo("[3P-Coop] Stage OK: " + name);
            }
            catch (Exception e)
            {
                Log.LogError("[3P-Coop] Stage FAILED (" + name + "): " + e);
            }
        }

        private void BindConfig()
        {
            MaxPlayers = Config.Bind("General", "MaxPlayers", 3,
                "Maximum number of players allowed. Default is 3 for 3-player co-op (blocks Player 4 from joining). Set to 4 if you want 4 players.");
            ShowCoopWatermark = Config.Bind("General", "ShowCoopWatermark", true,
                "Show a clean 3-Player Co-op HUD status in the top-right corner of the screen.");

            SyncExtrasToSave = Config.Bind("Saves", "SyncExtrasToSave", true,
                "Synchronize Player 3 coin balances, weapons, and charm unlocks with Player 1's save.");

            FixWheezySpawns = Config.Bind("Spawns", "FixWheezySpawns", true,
                "Prevent Player 3 from spawning inside cigar/fire hazards on Mr. Wheezy (DicePalaceCigar).");
            WheezyFixAllDicePalace = Config.Bind("Spawns", "WheezyFixAllDicePalace", true,
                "Apply safe hazard-clearance spawns across all King Dice Palace minibosses.");
            HazardClearance = Config.Bind("Spawns", "HazardClearance", 120f,
                "Minimum distance in pixels between extra player spawns and known hazard transforms.");
            ExtraSpawnSpacing = Config.Bind("Spawns", "ExtraSpawnSpacing", 80f,
                "Horizontal spacing between extra player spawn points.");

            RescueOffscreenPlayers = Config.Bind("Camera", "RescueOffscreenPlayers", true,
                "Pull players who fall behind offscreen in scrolling Run & Gun stages back to Player 1.");
            ViewportMargin = Config.Bind("Camera", "ViewportMargin", 0.05f,
                "Distance outside camera viewport before offscreen rescue triggers.");
            RescueCooldown = Config.Bind("Camera", "RescueCooldown", 1.0f,
                "Cooldown in seconds per player before rescue can re-trigger.");

            UIFontMode = Config.Bind("Visuals", "UIFontMode", 0,
                "Font style for UI: 0 = Cuphead Authentic (Vogue & Memphis), 1 = Clean Sans (Arial/System).");
            UIFontAspectScale = Config.Bind("Visuals", "UIFontAspectScale", 0.88f,
                "Font aspect ratio scale (horizontal width multiplier). 0.88 = slender/tall, 1.00 = standard wide.");
            PersistExtraTints = Config.Bind("Visuals", "PersistExtraTints", true,
                "Re-apply Player 3 custom color tint every frame and after lighting changes.");
            Player3Color = Config.Bind("Visuals", "Player3Color", "#10FF00",
                "Color tint for Player 3 (Cuphead/Mugman). Ms. Chalice uses her own textures. Default is vibrant emerald green.");
            Player4Color = Config.Bind("Visuals", "Player4Color", "#FFD700",
                "Color tint for Player 4 (if 4-player mode is active). Default is gold.");
            P3BaseCharacter = Config.Bind("Visuals", "P3BaseCharacter", 1,
                "Base character model for Player 3: 0 = Cuphead, 1 = Mugman. Default is 1 (Mugman - different nose & straw than Cuphead).");

            FixAnyPlayerInput = Config.Bind("DLC", "FixAnyPlayerInput", true,
                "Prevent cutscene and DLC entrance freezes caused by null Rewired player slots.");
            ParkExtrasForDlc = Config.Bind("DLC", "ParkExtrasForDlc", true,
                "Cleanly handle Player 3 during the boatman transition to DLC island.");
            GiftChaliceToExtras = Config.Bind("DLC", "GiftChaliceToExtras", true,
                "Grant Player 3 the Astral Cookie / Chalice charm when unlocking DLC so Player 3 can play as Ms. Chalice.");

            EnableDamageScaling = Config.Bind("DamageScaling", "EnableDamageScaling", true,
                "Balanced damage scaling tailored for multiplayer co-op.");
            DamageDifficultyPreset = Config.Bind("DamageScaling", "DamageDifficultyPreset", "Normal",
                "Difficulty preset for player damage scaling: Easy (2P: 75%, 3P: 50%), Normal (Vanilla: 2P: 50%, 3P: 35%), Hard (2P: 35%, 3P: 25%), or Custom.");
            DamageMultiplier1P = Config.Bind("DamageScaling", "DamageMultiplier_1P", 1.0f,
                "Damage multiplier when 1 player is alive (1.00 = full power).");
            DamageMultiplier2P = Config.Bind("DamageScaling", "DamageMultiplier_2P", 0.50f,
                "Damage multiplier when 2 players are alive (0.50 = vanilla 2P rate).");
            DamageMultiplier3P = Config.Bind("DamageScaling", "DamageMultiplier_3P", 0.35f,
                "Damage multiplier when 3 players are alive (0.35 = balanced 3P rate).");
            DamageMultiplier4P = Config.Bind("DamageScaling", "DamageMultiplier_4P", 0.25f,
                "Damage multiplier when 4 players are alive (0.25).");
            ScaleByAlivePlayers = Config.Bind("DamageScaling", "ScaleByAlivePlayers", true,
                "Dynamically adjust damage multiplier as players die (e.g. if P2 dies in a 3P game, damage increases to 0.50x).");
            EnableDynamicDefaultScaling = Config.Bind("DamageScaling", "EnableDynamicDefaultScaling", true,
                "Fall back to 1.0 / activePlayerCount if configured multiplier is invalid.");
            LogDamageScaling = Config.Bind("DamageScaling", "LogDamageScaling", true,
                "Log damage scaling changes in BepInEx console.");

            FixBossTargeting = Config.Bind("Bosses", "FixBossTargeting", true,
                "Fix KeyNotFoundException in boss targeting coroutines and distribute attacks uniformly across all players.");
            FixChessBossTargeting = Config.Bind("Bosses", "FixChessBossTargeting", true,
                "King's Leap: Ensure Chess Knight, Bishop, and Candle bosses can target Player 3.");
            RedirectDeadPlayerAim = Config.Bind("Bosses", "RedirectDeadPlayerAim", true,
                "Redirect boss attacks aimed at fallen/dead players toward nearest living teammate.");
            LogTargetSwitches = Config.Bind("Bosses", "LogTargetSwitches", false,
                "Log boss target switches (verbose, for diagnostics).");

            ExtraPlayersCanInteract = Config.Bind("Map", "ExtraPlayersCanInteract", true,
                "Allow Player 3 to talk to NPCs, enter levels, and enter Porkrind's Shop on overworld map.");
            ExtrasUseMugmanEntities = Config.Bind("Map", "ExtrasUseMugmanEntities", true,
                "Allow Player 3 to trigger entities flagged for Mugman.");
            MapInteractionWatchdog = Config.Bind("Map", "MapInteractionWatchdog", true,
                "Prevent overworld map softlock if an interaction is interrupted.");
            FixChaliceGhostArray = Config.Bind("Map", "FixChaliceGhostArray", true,
                "Fix ghostInPortal array out-of-bounds softlock when Player 3 is Ms. Chalice returning to map.");
            FixChaliceVictoryBranch = Config.Bind("Map", "FixChaliceVictoryBranch", true,
                "Fix victory branch coroutine so all 3 players celebrate and return safely without freezing.");
            ForceVictoryCleanup = Config.Bind("Map", "ForceVictoryCleanup", true,
                "Release player motion and pause menu when overworld map returns to Ready state.");
            RegroupStrandedExtras = Config.Bind("Map", "RegroupStrandedExtras", true,
                "Teleport stranded extra players back beside Player 1 upon map load.");
            MapRegroupDistance = Config.Bind("Map", "MapRegroupDistance", 5f,
                "Distance in map units before regrouping extra players.");
            VerboseMapInteractions = Config.Bind("Map", "VerboseMapInteractions", false,
                "Verbose log of map interaction triggers.");

            BlockJoinOutsideGameplay = Config.Bind("Join", "BlockJoinOutsideGameplay", true,
                "Block extra players from joining during loading screens, cutscenes, and menus.");
            RestoreJoinOnGameplayLoad = Config.Bind("Join", "RestoreJoinOnGameplayLoad", true,
                "Re-enable joining once a level or map has finished loading.");
            GateTitleScreen = Config.Bind("Join", "GateTitleScreen", false,
                "Block extra slots on title screen.");

            AssignGamepadsToExtras = Config.Bind("Input", "AssignGamepadsToExtras", true,
                "Detaches gamepads from Player 1 when 2 or more gamepads are connected or when P1 is on keyboard. Frees gamepads for Player 2 and Player 3.");
            SinglePressJoin = Config.Bind("Input", "SinglePressJoin", true,
                "Allows extra players to join on a single gamepad button press without requiring a double-press confirmation prompt.");
            QuickRestartEnabled = Config.Bind("Input", "QuickRestartEnabled", true,
                "When true, holding the Left Stick click (L3 / LS) on any gamepad (or [R] on keyboard) instantly restarts the level.");
            QuickRestartHoldDuration = Config.Bind("Input", "QuickRestartHoldDuration", 1.0f,
                "Duration in seconds to hold the Left Stick click (L3 / LS) on a gamepad to instantly reload the level (0.4s - 3.0s).");

            UnlockExpertAlways = Config.Bind("Gameplay", "UnlockExpertAlways", true,
                "When true, Expert difficulty is always unlocked and available on boss stages.");
            ParryTimeFreeze = Config.Bind("Gameplay", "ParryTimeFreeze", true,
                "When true, parrying triggers a brief time freeze (hitstop) like in vanilla Cuphead. When false, parrying does not freeze time, allowing seamless multiplayer gameplay without pausing other players.");
            RestartOnGhostLost = Config.Bind("Difficulty", "RestartOnGhostLost", true,
                "Hardcore Co-op: Automatically reload level if any player dies and their parry ghost floats off-screen (cannot be revived).");
            RestartOnlyOnBosses = Config.Bind("Difficulty", "RestartOnlyOnBosses", true,
                "When true, hardcore ghost loss reload only triggers on Boss fights, and is disabled during Run & Gun (hit & run) stages.");
            GhostLostGraceSeconds = Config.Bind("Difficulty", "GhostLostGraceSeconds", 1.0f,
                "Delay in seconds between ghost floating off-screen and level reload (gives a moment to see notification).");
            ShowProgressOnGhostLost = Config.Bind("Difficulty", "ShowProgressOnGhostLost", true,
                "When true, displays the boss progress timeline card (Game Over screen) for a moment upon ghost loss before auto-restarting.");
            GameOverProgressSeconds = Config.Bind("Difficulty", "GameOverProgressSeconds", 3.0f,
                "Duration in seconds to display the boss progress card before automatically restarting the level.");

            BaseHealthP1 = Config.Bind("Health", "BaseHealth_P1", 3,
                "Base starting health for Player 1 (Range: 1-3, default 3).");
            BaseHealthP2 = Config.Bind("Health", "BaseHealth_P2", 3,
                "Base starting health for Player 2 (Range: 1-3, default 3).");
            BaseHealthP3 = Config.Bind("Health", "BaseHealth_P3", 3,
                "Base starting health for Player 3 (Range: 1-3, default 3).");
            BaseHealthP4 = Config.Bind("Health", "BaseHealth_P4", 3,
                "Base starting health for Player 4 (if active, Range: 1-3, default 3).");
        }

        private void OnDestroy()
        {
            CoopStatsManager.Flush();
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }
        }
    }
}
