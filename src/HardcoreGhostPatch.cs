using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Hardcore Co-op Mode:
    /// Automatically restarts the level if any player dies and their parry ghost floats
    /// off-screen / can no longer be revived.
    /// In Boss fights, shows the boss progress timeline card for a moment before auto-restarting.
    /// Ensures 3-player co-op groups beat stages with all players surviving.
    /// </summary>
    public static class HardcoreGhostWatcher
    {
        private static readonly HashSet<PlayerDeathEffect> _activeGhosts = new HashSet<PlayerDeathEffect>();
        private static readonly Dictionary<PlayerDeathEffect, float> _lostTimers = new Dictionary<PlayerDeathEffect, float>();
        private static bool _isRestarting = false;
        private static bool _autoRetryPending = false;
        private static float _autoRetryTime = 0f;

        private static readonly FieldInfo _playerIdField = AccessTools.Field(typeof(PlayerDeathEffect), "playerId");
        private static readonly FieldInfo _exitingField = AccessTools.Field(typeof(PlayerDeathEffect), "exiting");
        private static readonly FieldInfo _guiStateField = AccessTools.Field(typeof(LevelGameOverGUI), "state");
        private static readonly MethodInfo _retryMethod = AccessTools.Method(typeof(LevelGameOverGUI), "Retry");

        internal static PlayerId GetPlayerId(PlayerDeathEffect ghost)
        {
            if (ghost == null || _playerIdField == null) return PlayerId.None;
            try
            {
                return (PlayerId)_playerIdField.GetValue(ghost);
            }
            catch
            {
                return PlayerId.None;
            }
        }

        internal static bool IsExiting(PlayerDeathEffect ghost)
        {
            if (ghost == null || _exitingField == null) return false;
            try
            {
                return (bool)_exitingField.GetValue(ghost);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsGameOverExiting()
        {
            var gui = LevelGameOverGUI.Current;
            if (gui == null || _guiStateField == null) return false;
            try
            {
                // State enum: 0 = Init, 1 = Ready, 2 = Exiting
                int val = Convert.ToInt32(_guiStateField.GetValue(gui));
                return val == 2;
            }
            catch
            {
                return false;
            }
        }

        internal static void RegisterGhost(PlayerDeathEffect ghost)
        {
            if (ghost != null && !_activeGhosts.Contains(ghost))
            {
                _activeGhosts.Add(ghost);
                Plugin.Log.LogInfo("[3P-Coop] Hardcore: Registered death ghost for " + GetPlayerId(ghost));
            }
        }

        internal static void UnregisterGhost(PlayerDeathEffect ghost)
        {
            if (ghost != null)
            {
                _activeGhosts.Remove(ghost);
                _lostTimers.Remove(ghost);
            }
        }

        internal static void ResetState()
        {
            _activeGhosts.Clear();
            _lostTimers.Clear();
            _isRestarting = false;
            _autoRetryPending = false;
            _autoRetryTime = 0f;
        }

        public static bool IsBossLevel(Level level)
        {
            if (level == null) return false;
            if (level is PlatformingLevel || PlatformingLevel.Current != null) return false;
            if (level.LevelType == Level.Type.Platforming || level.LevelType == Level.Type.Tutorial) return false;
            return level.LevelType == Level.Type.Battle || Level.IsDicePalace || Level.IsChessBoss;
        }

        public static bool IsGhostInClutchWindow(PlayerDeathEffect ghost)
        {
            if (ghost == null) return false;
            float firstLostTime;
            if (_lostTimers.TryGetValue(ghost, out firstLostTime))
            {
                float grace = Plugin.GhostLostGraceSeconds != null ? Plugin.GhostLostGraceSeconds.Value : 1.0f;
                float remaining = grace - (Time.unscaledTime - firstLostTime);
                return remaining <= 0.6f && remaining >= -0.2f;
            }
            return false;
        }

        /// <summary>
        /// Called every frame from FixRunner.LateUpdate().
        /// </summary>
        public static void Tick()
        {
            if (!Plugin.RestartOnGhostLost.Value) return;

            // 1. Handle pending auto-retry after showing boss progress view
            if (_autoRetryPending)
            {
                if (IsGameOverExiting())
                {
                    // Player pressed Retry manually
                    _autoRetryPending = false;
                    return;
                }

                if (Time.unscaledTime >= _autoRetryTime)
                {
                    _autoRetryPending = false;
                    Plugin.Log.LogInfo("[3P-Coop] Hardcore: Boss progress time elapsed. Auto-restarting level...");
                    InvokeGameOverRetry();
                }
                return;
            }

            if (_isRestarting) return;

            var level = Level.Current;
            if (level == null || level.Ending || !level.PlayersCreated)
            {
                _lostTimers.Clear();
                return;
            }

            // User setting: only active on Boss levels, NOT in Run & Gun (Platforming)
            if (Plugin.RestartOnlyOnBosses.Value && !IsBossLevel(level))
            {
                _lostTimers.Clear();
                return;
            }

            var cam = CupheadLevelCamera.Current;
            if (cam == null) return;

            if (_activeGhosts.Count == 0)
            {
                _lostTimers.Clear();
                return;
            }

            float now = Time.unscaledTime;
            float grace = Mathf.Max(0.2f, Plugin.GhostLostGraceSeconds.Value);

            var ghostsToRemove = new List<PlayerDeathEffect>();
            PlayerDeathEffect lostGhost = null;

            foreach (var ghost in _activeGhosts)
            {
                if (ghost == null || !ghost.gameObject.activeInHierarchy)
                {
                    ghostsToRemove.Add(ghost);
                    continue;
                }

                // If ghost was parried or revived, exiting is true
                if (IsExiting(ghost))
                {
                    _lostTimers.Remove(ghost);
                    continue;
                }

                // Vanilla MDHR out-of-frame check: padding (1000f, 10f)
                // When ContainsPoint returns false, ghost has floated past top of camera viewport!
                bool inFrame = cam.ContainsPoint((Vector2)ghost.transform.position, new Vector2(1000f, 10f));

                if (!inFrame)
                {
                    float firstLostTime;
                    if (!_lostTimers.TryGetValue(ghost, out firstLostTime))
                    {
                        _lostTimers[ghost] = now;
                        string pName = GetPlayerDisplayName(GetPlayerId(ghost));
                        Plugin.Log.LogInfo("[3P-Coop] Hardcore: Ghost for " + pName + " floated off-screen. Grace timer started (" + grace + "s)...");
                    }
                    else if (now - firstLostTime >= grace)
                    {
                        lostGhost = ghost;
                        break;
                    }
                }
                else
                {
                    // Ghost is still in bounds
                    if (_lostTimers.ContainsKey(ghost))
                    {
                        _lostTimers.Remove(ghost);
                    }
                }
            }

            foreach (var g in ghostsToRemove)
            {
                _activeGhosts.Remove(g);
                _lostTimers.Remove(g);
            }

            if (lostGhost != null)
            {
                string pName = GetPlayerDisplayName(GetPlayerId(lostGhost));
                TriggerRestart(pName);
            }
        }

        private static string GetPlayerDisplayName(PlayerId id)
        {
            return CupheadUITheme.GetPlayerFullName(id);
        }

        private static void TriggerRestart(string playerName)
        {
            if (_isRestarting) return;
            _isRestarting = true;

            CoopStatsManager.RecordHardcoreRestart();

            var level = Level.Current;
            bool isBoss = IsBossLevel(level);

            Plugin.Log.LogInfo("[3P-Coop] Hardcore: Ghost lost (" + playerName + ")! Level=" + (level != null ? level.name : "null") + ", isBoss=" + isBoss);

            // If in a boss level and ShowProgressOnGhostLost is enabled, trigger Game Over progress card
            if (isBoss && Plugin.ShowProgressOnGhostLost.Value && level != null && !level.Ending)
            {
                try
                {
                    Plugin.Log.LogInfo("[3P-Coop] Hardcore: Triggering boss progress screen (_OnLose)...");
                    ControllerManager.ShowToast("Utrata duszka (" + playerName + ")! Postęp walki...", Plugin.GameOverProgressSeconds.Value);

                    var onLose = AccessTools.Method(typeof(Level), "_OnLose");
                    if (onLose != null)
                    {
                        onLose.Invoke(level, null);
                        _autoRetryPending = true;
                        _autoRetryTime = Time.unscaledTime + Mathf.Max(1.0f, Plugin.GameOverProgressSeconds.Value);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("[3P-Coop] Failed to trigger _OnLose progress screen: " + e);
                }
            }

            // Fallback or direct restart (e.g. if not boss or progress card disabled)
            TriggerDirectRestart(playerName);
        }

        internal static void InvokeGameOverRetry()
        {
            try
            {
                var gui = LevelGameOverGUI.Current;
                if (gui != null && _retryMethod != null)
                {
                    _retryMethod.Invoke(gui, null);
                    return;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[3P-Coop] InvokeGameOverRetry error: " + e);
            }

            TriggerDirectRestart("Auto-Retry");
        }

        private static void TriggerDirectRestart(string reason)
        {
            Plugin.Log.LogInfo("[3P-Coop] Hardcore: Direct restart (" + reason + ")...");
            ControllerManager.ShowToast("Restart poziomu (" + reason + ")...", 3.0f);

            try
            {
                PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerOne, false);
                PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerTwo, false);
                PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerThree, false);
                PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerFour, false);

                Dialoguer.EndDialogue();

                if (Level.IsDicePalaceMain || Level.IsDicePalace)
                {
                    DicePalaceMainLevelGameInfo.CleanUpRetry();
                }

                SceneLoader.ReloadLevel();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[3P-Coop] Direct restart error: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerDeathEffect), "Start")]
    internal static class HardcoreGhostStartPatch
    {
        private static void Postfix(PlayerDeathEffect __instance)
        {
            HardcoreGhostWatcher.RegisterGhost(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerDeathEffect), "OnDestroy")]
    internal static class HardcoreGhostDestroyPatch
    {
        private static void Postfix(PlayerDeathEffect __instance)
        {
            HardcoreGhostWatcher.UnregisterGhost(__instance);
        }
    }

    [HarmonyPatch(typeof(Level), "Awake")]
    internal static class HardcoreLevelAwakePatch
    {
        private static void Postfix()
        {
            HardcoreGhostWatcher.ResetState();
        }
    }
}
