using System;
using HarmonyLib;
using UnityEngine;

namespace Cuphead3PCoop
{
    public enum DamageDifficultyPreset
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Custom = 3
    }

    /// <summary>
    /// Configurable player damage scaling.
    ///
    /// The game already funnels all player damage through one chokepoint:
    ///
    ///     public static float DamageMultiplier =&gt; 1f / (float)Count;
    ///
    /// Every consumer is a player weapon or super - AbstractProjectile.DamageMultiplier,
    /// PlayerSuperBeam, PlayerSuperGhost, PlayerSuperChaliceVerticalBeam, PlaneSuperBomb,
    /// PlaneSuperChalice, WeaponBoomerang and RetroArcadeWormRocketBrokenPiece. Nothing on the
    /// enemy-to-player path reads it, so overriding this one getter scales exactly player output and
    /// nothing else. That is far safer and more complete than intercepting individual DealDamage
    /// call sites, which would miss supers and the boomerang's maxDamage.
    ///
    /// Note PlayerManager.Count counts LIVING players, so the multiplier naturally rises again as
    /// players go down - the same behaviour vanilla has.
    /// </summary>
    internal static class DamageScaling
    {
        private static float _lastLogged = -1f;
        private static int _lastCount = -1;

        internal static DamageDifficultyPreset CurrentPreset
        {
            get
            {
                var p = Plugin.DamageDifficultyPreset != null ? Plugin.DamageDifficultyPreset.Value : "Normal";
                if (string.Equals(p, "Easy", StringComparison.OrdinalIgnoreCase)) return DamageDifficultyPreset.Easy;
                if (string.Equals(p, "Hard", StringComparison.OrdinalIgnoreCase)) return DamageDifficultyPreset.Hard;
                if (string.Equals(p, "Normal", StringComparison.OrdinalIgnoreCase)) return DamageDifficultyPreset.Normal;
                return DamageDifficultyPreset.Custom;
            }
        }

        internal static string CurrentPresetName
        {
            get
            {
                switch (CurrentPreset)
                {
                    case DamageDifficultyPreset.Easy: return "Easy";
                    case DamageDifficultyPreset.Normal: return "Normal";
                    case DamageDifficultyPreset.Hard: return "Hard";
                    default: return "Custom";
                }
            }
        }

        internal static string CurrentPresetDescription
        {
            get
            {
                switch (CurrentPreset)
                {
                    case DamageDifficultyPreset.Easy: return "Easy (2P: 75% | 3P: 50%)";
                    case DamageDifficultyPreset.Normal: return "Normal (2P: 50% | 3P: 35%)";
                    case DamageDifficultyPreset.Hard: return "Hard (2P: 35% | 3P: 25%)";
                    default: return "Custom (Manual Config)";
                }
            }
        }

        internal static void SetPreset(DamageDifficultyPreset preset)
        {
            switch (preset)
            {
                case DamageDifficultyPreset.Easy:
                    if (Plugin.DamageDifficultyPreset != null) Plugin.DamageDifficultyPreset.Value = "Easy";
                    if (Plugin.DamageMultiplier1P != null) Plugin.DamageMultiplier1P.Value = 1.00f;
                    if (Plugin.DamageMultiplier2P != null) Plugin.DamageMultiplier2P.Value = 0.75f;
                    if (Plugin.DamageMultiplier3P != null) Plugin.DamageMultiplier3P.Value = 0.50f;
                    if (Plugin.DamageMultiplier4P != null) Plugin.DamageMultiplier4P.Value = 0.40f;
                    ControllerManager.ShowToast("Damage Preset: EASY (2P: 75%, 3P: 50%)");
                    break;
                case DamageDifficultyPreset.Normal:
                    if (Plugin.DamageDifficultyPreset != null) Plugin.DamageDifficultyPreset.Value = "Normal";
                    if (Plugin.DamageMultiplier1P != null) Plugin.DamageMultiplier1P.Value = 1.00f;
                    if (Plugin.DamageMultiplier2P != null) Plugin.DamageMultiplier2P.Value = 0.50f;
                    if (Plugin.DamageMultiplier3P != null) Plugin.DamageMultiplier3P.Value = 0.35f;
                    if (Plugin.DamageMultiplier4P != null) Plugin.DamageMultiplier4P.Value = 0.25f;
                    ControllerManager.ShowToast("Damage Preset: NORMAL (Vanilla 2P: 50%, 3P: 35%)");
                    break;
                case DamageDifficultyPreset.Hard:
                    if (Plugin.DamageDifficultyPreset != null) Plugin.DamageDifficultyPreset.Value = "Hard";
                    if (Plugin.DamageMultiplier1P != null) Plugin.DamageMultiplier1P.Value = 0.85f;
                    if (Plugin.DamageMultiplier2P != null) Plugin.DamageMultiplier2P.Value = 0.35f;
                    if (Plugin.DamageMultiplier3P != null) Plugin.DamageMultiplier3P.Value = 0.25f;
                    if (Plugin.DamageMultiplier4P != null) Plugin.DamageMultiplier4P.Value = 0.20f;
                    ControllerManager.ShowToast("Damage Preset: HARD (2P: 35%, 3P: 25%)");
                    break;
            }

            ResetLogState();
            ReportChange("preset set to " + preset);
        }

        internal static void CyclePreset(int delta)
        {
            var cur = CurrentPreset;
            int idx = (cur == DamageDifficultyPreset.Custom) ? 1 : (int)cur;
            idx = (idx + delta) % 3;
            if (idx < 0) idx += 3;
            SetPreset((DamageDifficultyPreset)idx);
        }

        internal static void InitFromConfig()
        {
            try
            {
                var cur = CurrentPreset;
                if (cur == DamageDifficultyPreset.Easy)
                {
                    if (Plugin.DamageMultiplier1P != null) Plugin.DamageMultiplier1P.Value = 1.00f;
                    if (Plugin.DamageMultiplier2P != null) Plugin.DamageMultiplier2P.Value = 0.75f;
                    if (Plugin.DamageMultiplier3P != null) Plugin.DamageMultiplier3P.Value = 0.50f;
                    if (Plugin.DamageMultiplier4P != null) Plugin.DamageMultiplier4P.Value = 0.40f;
                }
                else if (cur == DamageDifficultyPreset.Hard)
                {
                    if (Plugin.DamageMultiplier1P != null) Plugin.DamageMultiplier1P.Value = 0.85f;
                    if (Plugin.DamageMultiplier2P != null) Plugin.DamageMultiplier2P.Value = 0.35f;
                    if (Plugin.DamageMultiplier3P != null) Plugin.DamageMultiplier3P.Value = 0.25f;
                    if (Plugin.DamageMultiplier4P != null) Plugin.DamageMultiplier4P.Value = 0.20f;
                }
                else if (cur == DamageDifficultyPreset.Normal)
                {
                    if (Plugin.DamageMultiplier1P != null) Plugin.DamageMultiplier1P.Value = 1.00f;
                    if (Plugin.DamageMultiplier2P != null) Plugin.DamageMultiplier2P.Value = 0.50f;
                    if (Plugin.DamageMultiplier3P != null) Plugin.DamageMultiplier3P.Value = 0.35f;
                    if (Plugin.DamageMultiplier4P != null) Plugin.DamageMultiplier4P.Value = 0.25f;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] DamageScaling.InitFromConfig error: " + e.Message);
            }
        }

        internal static bool DynamicMode
        {
            get
            {
                try { return Plugin.ScaleByAlivePlayers.Value; }
                catch (Exception) { return true; }
            }
        }

        internal static string ModeName
        {
            get { return DynamicMode ? "Dynamic Alive" : "Static Connected"; }
        }

        /// <summary>
        /// The player count the multiplier is chosen from, clamped to the 1-4 range the config
        /// covers.
        ///
        /// Dynamic mode counts only players present AND alive. Static mode counts everyone present
        /// in the level regardless of death.
        ///
        /// Two things worth knowing:
        ///
        ///   * PlayerStatsManager has no `isDead` member. The correct test is
        ///     AbstractPlayerController.IsDead, which is `_isReviving ? false : stats.Health &lt;= 0` -
        ///     exactly the wanted behaviour, since a downed player in ghost form is not counted and
        ///     the instant a parry starts the revive they count again.
        ///   * `GetAllPlayers().Count` cannot be used for the static count. PlayerManager.Awake
        ///     seeds the dictionary with all four keys set to null, so that Count is ALWAYS 4 -
        ///     using it would pin even a solo game to the 4P multiplier. Non-null entries are
        ///     counted instead.
        /// </summary>
        internal static int ScalingPlayerCount()
        {
            var count = 0;
            var dynamicMode = DynamicMode;

            try
            {
                foreach (var id in Extras.AllIds)
                {
                    var player = Extras.GetPlayer(id);
                    if (player == null) continue;          // slot never joined
                    if (player.stats == null) continue;    // mid-teardown
                    if (dynamicMode && player.IsDead) continue;
                    count++;
                }
            }
            catch (Exception)
            {
                return 1;
            }

            if (count < 1) return 1;
            if (count > 4) return 4;
            return count;
        }

        internal static float MultiplierFor(int count)
        {
            float configured;
            switch (count)
            {
                case 1: configured = Plugin.DamageMultiplier1P.Value; break;
                case 2: configured = Plugin.DamageMultiplier2P.Value; break;
                case 3: configured = Plugin.DamageMultiplier3P.Value; break;
                default: configured = Plugin.DamageMultiplier4P.Value; break;
            }

            // "Unset or invalid" - a non-positive or non-finite entry - falls back to 1/N when
            // dynamic defaulting is on, otherwise to vanilla's own 1/N anyway.
            var usable = configured > 0f && !float.IsNaN(configured) && !float.IsInfinity(configured);
            if (usable) return configured;

            // Unset/invalid. With dynamic defaulting on we compute 1/N; with it off we still fall
            // back to 1/N, because that is vanilla's own formula and the safest thing to apply.
            return 1f / count;
        }

        internal static float Current()
        {
            var count = ScalingPlayerCount();
            var multiplier = MultiplierFor(count);
            NoteChange(count, multiplier);
            return multiplier;
        }

        private static void NoteChange(int count, float multiplier)
        {
            if (!Plugin.LogDamageScaling.Value) return;
            if (count == _lastCount && Mathf.Approximately(multiplier, _lastLogged)) return;

            _lastCount = count;
            _lastLogged = multiplier;

            Plugin.Log.LogInfo("[3P-Coop] Damage scaling active: Mode=" + ModeName +
                               ", Players=" + count +
                               ", Multiplier=" + multiplier.ToString("P0"));
        }

        /// <summary>Forces the next evaluation to log, so each level entry reports afresh.</summary>
        internal static void ResetLogState()
        {
            _lastCount = -1;
            _lastLogged = -1f;
        }

        internal static void ReportForLevel()
        {
            if (!Plugin.LogDamageScaling.Value) return;

            ResetLogState();
            var count = ScalingPlayerCount();
            var multiplier = MultiplierFor(count);
            _lastCount = count;
            _lastLogged = multiplier;

            Plugin.Log.LogInfo("[3P-Coop] Damage scaling active: Mode=" + ModeName +
                               ", Multiplier=" + multiplier.ToString("P0") +
                               " (players counted: " + count + ")");
        }

        /// <summary>
        /// Re-evaluates and logs immediately on a death or revive, rather than waiting for the next
        /// projectile to read the multiplier. The stats manager updates health before these fire.
        /// </summary>
        internal static void ReportChange(string reason)
        {
            if (!Plugin.LogDamageScaling.Value) return;

            var count = ScalingPlayerCount();
            var multiplier = MultiplierFor(count);
            if (count == _lastCount && Mathf.Approximately(multiplier, _lastLogged)) return;

            _lastCount = count;
            _lastLogged = multiplier;

            Plugin.Log.LogInfo("[3P-Coop] Alive player count changed to " + count +
                               ". Damage multiplier updated to " + multiplier.ToString("P0") +
                               ". (" + reason + ", Mode=" + ModeName + ")");
        }
    }

    /// <summary>Replaces vanilla's hardcoded 1/Count with the configured curve.</summary>
    [HarmonyPatch(typeof(PlayerManager), "get_DamageMultiplier")]
    internal static class PlayerManagerDamageMultiplierPatch
    {
        private static bool Prefix(ref float __result)
        {
            if (!Plugin.EnableDamageScaling.Value) return true;

            try
            {
                __result = DamageScaling.Current();
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] Damage scaling failed, using vanilla: " + e.Message);
                return true;
            }
        }
    }

    /// <summary>
    /// Immediate re-evaluation the moment a player goes down or is parried back, so the multiplier
    /// change is logged at the event rather than on the next shot.
    /// </summary>
    [HarmonyPatch(typeof(Level), "OnPlayerDeath")]
    internal static class LevelOnPlayerDeathPatch
    {
        private static void Postfix()
        {
            try { DamageScaling.ReportChange("player died"); }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(Level), "OnPlayerRevive")]
    internal static class LevelOnPlayerRevivePatch
    {
        private static void Postfix()
        {
            try { DamageScaling.ReportChange("player revived"); }
            catch (Exception) { }
        }
    }

    /// <summary>Reports the scaling in force as each level begins.</summary>
    [HarmonyPatch(typeof(Level), "Start")]
    internal static class LevelStartDamageReportPatch
    {
        private static void Postfix(Level __instance)
        {
            if (__instance == null) return;
            try
            {
                DamageScaling.ReportForLevel();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] Damage scaling report failed: " + e.Message);
            }
        }
    }
}
