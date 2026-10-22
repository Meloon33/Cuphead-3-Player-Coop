using System;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Enforces player join gating and limits extra controllers from joining outside gameplay.
    /// Also enforces the MaxPlayers cap (locking slot 4 when MaxPlayers == 3).
    /// </summary>
    internal static class JoinGate
    {
        internal static bool GateDisabled;
        internal static bool SlotSelectActive;

        private static bool _blocked;
        private static string _activeSceneName = string.Empty;
        private static string _lastAllowedSceneLogged;

        internal static void Init()
        {
            try
            {
                _activeSceneName = SceneManager.GetActiveScene().name;
            }
            catch (Exception e)
            {
                _activeSceneName = string.Empty;
                Plugin.Log.LogWarning("[3P-Coop] JoinGate.Init: could not read active scene: " + e.Message);
            }

            try
            {
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] JoinGate.Init: scene events unavailable: " + e.Message);
            }
        }

        private static void OnActiveSceneChanged(Scene from, Scene to)
        {
            try { _activeSceneName = to.name; } catch (Exception) { }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try { _activeSceneName = SceneManager.GetActiveScene().name; } catch (Exception) { }
        }

        internal static string ActiveScene
        {
            get { return _activeSceneName; }
        }

        private static bool IsMenuScene(string scene)
        {
            if (scene == "scene_slot_select") return true;
            if (!Plugin.GateTitleScreen.Value) return false;
            return scene == "scene_title" || scene == "scene_start";
        }

        private static bool GameplayActive()
        {
            return Level.Current != null || Map.Current != null;
        }

        private static System.Reflection.FieldInfo _playerSlotsField;
        private static System.Reflection.FieldInfo _canJoinField;
        private static System.Reflection.FieldInfo _promptField;
        private static System.Reflection.FieldInfo _joinStateField;

        private static void EnforceSlot4Disabled()
        {
            if (Plugin.MaxPlayers.Value >= 4) return;
            try
            {
                if (_playerSlotsField == null)
                    _playerSlotsField = AccessTools.Field(typeof(PlayerManager), "playerSlots");
                if (_playerSlotsField == null) return;
                var slots = _playerSlotsField.GetValue(null) as Array;
                if (slots == null || slots.Length <= 3) return;
                var slot4 = slots.GetValue(3);
                if (slot4 == null) return;

                if (_canJoinField == null)
                    _canJoinField = AccessTools.Field(slot4.GetType(), "canJoin");
                if (_promptField == null)
                    _promptField = AccessTools.Field(slot4.GetType(), "promptBeforeJoin");
                if (_joinStateField == null)
                    _joinStateField = AccessTools.Field(slot4.GetType(), "joinState");

                if (_canJoinField != null) _canJoinField.SetValue(slot4, false);
                if (_promptField != null) _promptField.SetValue(slot4, false);
                if (_joinStateField != null)
                {
                    var cur = Convert.ToInt32(_joinStateField.GetValue(slot4));
                    if (cur == 1 /* Prompt */)
                    {
                        _joinStateField.SetValue(slot4, Enum.ToObject(_joinStateField.FieldType, 0 /* None */));
                    }
                }
            }
            catch (Exception) { }
        }

        internal static void Enforce()
        {
            EnforceSlot4Disabled();

            if (GateDisabled) return;
            if (!Plugin.BlockJoinOutsideGameplay.Value) return;

            if (GameplayActive())
            {
                NoteAllowed();
                return;
            }

            if (!Plugin.GateTitleScreen.Value &&
                (_activeSceneName == "scene_title" || _activeSceneName == "scene_start"))
            {
                NoteAllowed();
                return;
            }

            var inMenu = SlotSelectActive || IsMenuScene(_activeSceneName);

            if (!inMenu && !NoGameplayYet())
            {
                NoteAllowed();
                return;
            }

            Block();
        }

        private static bool NoGameplayYet()
        {
            return !GameplayActive();
        }

        private static void Block()
        {
            PlayerManager.SetPlayerCanJoin(PlayerId.PlayerTwo, canJoin: false, promptBeforeJoin: false);

            if (!_blocked)
            {
                _blocked = true;
                _lastAllowedSceneLogged = null;
                Plugin.Log.LogInfo("[3P-Coop] Join blocked in scene: " + _activeSceneName);
            }
        }

        private static void NoteAllowed()
        {
            if (_blocked)
            {
                _blocked = false;
                _lastAllowedSceneLogged = null;
            }

            if (_lastAllowedSceneLogged == _activeSceneName) return;
            _lastAllowedSceneLogged = _activeSceneName;
            Plugin.Log.LogInfo("[3P-Coop] Joining allowed in scene: " + _activeSceneName);
        }

        internal static void RestoreForGameplay(string source)
        {
            if (!Plugin.RestoreJoinOnGameplayLoad.Value) return;

            try
            {
                bool prompt = !Plugin.SinglePressJoin.Value;
                PlayerManager.SetPlayerCanJoin(PlayerId.PlayerTwo, canJoin: true, promptBeforeJoin: prompt);
                
                EnforceSlot4Disabled();

                _blocked = false;
                _lastAllowedSceneLogged = null;
                Plugin.Log.LogInfo("[3P-Coop] Joining re-armed for gameplay from " + source);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] RestoreForGameplay(" + source + ") failed: " + e.Message);
            }
        }

        internal static void Disable(string reason)
        {
            if (GateDisabled) return;
            GateDisabled = true;
            Plugin.Log.LogError("[3P-Coop] Join gate DISABLED: " + reason);
        }
    }

    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.Update))]
    internal static class PlayerManagerUpdatePatch
    {
        private static void Prefix()
        {
            try
            {
                JoinGate.Enforce();
            }
            catch (Exception e)
            {
                JoinGate.Disable(e.ToString());
            }
        }
    }

    [HarmonyPatch(typeof(Level), "Start")]
    internal static class LevelStartJoinPatch
    {
        private static void Postfix(Level __instance)
        {
            if (__instance == null) return;
            JoinGate.RestoreForGameplay("Level.Start");
        }
    }

    [HarmonyPatch(typeof(Map), "Start")]
    internal static class MapStartJoinPatch
    {
        private static void Postfix(Map __instance)
        {
            if (__instance == null) return;
            JoinGate.RestoreForGameplay("Map.Start");
        }
    }

    [HarmonyPatch(typeof(SlotSelectScreen), "Awake")]
    internal static class SlotSelectScreenAwakePatch
    {
        private static void Postfix(SlotSelectScreen __instance)
        {
            if (__instance == null) return;
            JoinGate.SlotSelectActive = true;
        }
    }

    [HarmonyPatch(typeof(SlotSelectScreen), "OnDestroy")]
    internal static class SlotSelectScreenOnDestroyPatch
    {
        private static void Postfix()
        {
            JoinGate.SlotSelectActive = false;
        }
    }

    [HarmonyPatch(typeof(Level), "CreatePlayerFourOnJoin")]
    internal static class PreventPlayerFourLevelJoinPatch
    {
        private static bool Prefix()
        {
            if (Plugin.MaxPlayers.Value < 4)
            {
                Plugin.Log.LogInfo("[3P-Coop] Blocked Player 4 from joining (MaxPlayers = " + Plugin.MaxPlayers.Value + ")");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Level), "CreatePlayerThreeOnJoin")]
    internal static class CreatePlayerThreeOnJoinSafePatch
    {
        private static bool Prefix(Level __instance)
        {
            if (__instance == null) return false;
            if (!PlayerManager.Multiplayer2) return false;

            try
            {
                var field = AccessTools.Field(typeof(Level), "allowMultiplayer");
                if (field != null && !(bool)field.GetValue(__instance)) return false;

                var playersField = AccessTools.Field(typeof(Level), "players");
                var players = playersField?.GetValue(__instance) as AbstractPlayerController[];
                if (players == null || players.Length < 3) return true;

                // If Player 2 exists, vanilla is completely safe!
                if (players[1] != null) return true;

                // Fall back: spawn relative to Player 1 safely!
                var p1 = players[0];
                if (p1 == null) return true;

                UnityEngine.Vector3 spawnPos = p1.center + new UnityEngine.Vector3(80f, 0f, 0f);
                var p3 = AbstractPlayerController.Create(PlayerId.PlayerThree, spawnPos, __instance.playerMode);
                players[2] = p3;
                p3.LevelJoin(spawnPos);
                Plugin.Log.LogInfo("[3P-Coop] Safe CreatePlayerThreeOnJoin spawned P3 relative to P1.");
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] CreatePlayerThreeOnJoinSafePatch: " + e.Message);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Map), "OnPlayerJoined")]
    internal static class MapOnPlayerJoinedSafePatch
    {
        private static bool Prefix(Map __instance, PlayerId playerId)
        {
            if (__instance == null) return true;
            if (playerId != PlayerId.PlayerThree) return true;

            try
            {
                var mapPlayers = __instance.players;
                if (mapPlayers != null && mapPlayers.Length > 1 && mapPlayers[1] == null && mapPlayers[0] != null)
                {
                    // Fall back: spawn relative to Player 1 safely instead of crashing on player 1!
                    UnityEngine.Vector3 pos = mapPlayers[0].transform.position + new UnityEngine.Vector3(0.5f, 0.5f, 0f);
                    var initObj = new MapPlayerController.InitObject(pos, MapPlayerPose.Default);
                    var p3 = MapPlayerController.Create(PlayerId.PlayerThree, initObj);

                    var alt = __instance.altplayers;
                    if (alt != null && alt.Length > 0)
                    {
                        alt[0] = p3;
                        if (p3.animationController != null && p3.animationController.spriteRenderer != null &&
                            mapPlayers[0].animationController != null && mapPlayers[0].animationController.spriteRenderer != null)
                        {
                            p3.animationController.spriteRenderer.sortingOrder = mapPlayers[0].animationController.spriteRenderer.sortingOrder;
                        }
                    }

                    LevelNewPlayerGUI.Current?.Init();
                    AccessTools.Method(typeof(Map), "SetRichPresence")?.Invoke(__instance, null);
                    Plugin.Log.LogInfo("[3P-Coop] Safe Map.OnPlayerJoined spawned P3 relative to P1.");
                    return false;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] MapOnPlayerJoinedSafePatch: " + e.Message);
            }
            return true;
        }
    }
}


