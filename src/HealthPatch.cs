using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Configures base starting health for each player in range 1-3.
    /// Supports real-time in-game configuration via [F5] menu.
    /// </summary>
    public static class HealthConfig
    {
        private static readonly MethodInfo _calcHealthMaxMethod = AccessTools.Method(typeof(PlayerStatsManager), "CalculateHealthMax");

        public static int GetBaseHealth(PlayerId id)
        {
            switch (id)
            {
                case PlayerId.PlayerOne:
                    return Mathf.Clamp(Plugin.BaseHealthP1.Value, 1, 3);
                case PlayerId.PlayerTwo:
                    return Mathf.Clamp(Plugin.BaseHealthP2.Value, 1, 3);
                case PlayerId.PlayerThree:
                    return Mathf.Clamp(Plugin.BaseHealthP3.Value, 1, 3);
                case PlayerId.PlayerFour:
                    return Mathf.Clamp(Plugin.BaseHealthP4.Value, 1, 3);
                default:
                    return 3;
            }
        }

        public static void SetBaseHealth(PlayerId id, int value)
        {
            value = Mathf.Clamp(value, 1, 3);
            switch (id)
            {
                case PlayerId.PlayerOne:
                    Plugin.BaseHealthP1.Value = value;
                    break;
                case PlayerId.PlayerTwo:
                    Plugin.BaseHealthP2.Value = value;
                    break;
                case PlayerId.PlayerThree:
                    Plugin.BaseHealthP3.Value = value;
                    break;
                case PlayerId.PlayerFour:
                    Plugin.BaseHealthP4.Value = value;
                    break;
            }

            ApplyHealthInGame(id);
        }

        public static void ApplyHealthInGame(PlayerId id)
        {
            try
            {
                if (Level.Current == null) return;
                var player = Extras.GetPlayer(id);
                if (player != null && player.stats != null && !player.IsDead)
                {
                    if (_calcHealthMaxMethod != null)
                    {
                        _calcHealthMaxMethod.Invoke(player.stats, null);
                    }
                    int targetHealth = Mathf.Min(player.stats.Health, player.stats.HealthMax);
                    player.stats.SetHealth(targetHealth);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("ApplyHealthInGame error: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerStatsManager), "CalculateHealthMax")]
    internal static class HealthMaxPatch
    {
        private static readonly FieldInfo _healthMaxField = AccessTools.Field(typeof(PlayerStatsManager), "<HealthMax>k__BackingField");

        private static void Postfix(PlayerStatsManager __instance)
        {
            try
            {
                var player = __instance.basePlayer;
                if (player == null) return;

                int targetBase = HealthConfig.GetBaseHealth(player.id);
                int diff = targetBase - 3; // Vanilla base is 3
                if (diff != 0 && _healthMaxField != null)
                {
                    int currentMax = __instance.HealthMax;
                    int newMax = Mathf.Max(1, currentMax + diff);
                    _healthMaxField.SetValue(__instance, newMax);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("HealthMaxPatch error: " + e.Message);
            }
        }
    }
}
