using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Configurable Parry Time Stop (Hitstop).
    ///
    /// In vanilla Cuphead, AbstractParryEffect.hit_cr calls PauseManager.Pause() and freezes the entire
    /// game world for 0.185 seconds (or 0.138s if hitEnemy).
    /// In 3-player co-op, frequent parries can stutter and freeze gameplay for teammates.
    /// This patch makes the time freeze optional:
    /// - When ParryTimeFreeze is true (default): vanilla behavior (hitstop pause).
    /// - When ParryTimeFreeze is false: seamless parries without freezing the game world for teammates.
    /// </summary>
    [HarmonyPatch]
    internal static class ParryFreezePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var targets = new List<MethodBase>();
            var outerType = typeof(AbstractParryEffect);

            foreach (var nested in outerType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (nested.Name.Contains("hit_cr"))
                {
                    var moveNext = AccessTools.Method(nested, "MoveNext");
                    if (moveNext != null)
                    {
                        targets.Add(moveNext);
                        Plugin.Log.LogInfo("[3P-Coop] ParryFreezePatch targeting: " + nested.FullName + ".MoveNext");
                    }
                }
            }
            return targets;
        }

        public static void SafeParryPause()
        {
            if (Plugin.ParryTimeFreeze != null && Plugin.ParryTimeFreeze.Value)
            {
                PauseManager.Pause();
            }
        }

        public static void SafeParryUnpause()
        {
            if (Plugin.ParryTimeFreeze != null && Plugin.ParryTimeFreeze.Value)
            {
                PauseManager.Unpause();
            }
        }

        public static float AdjustParryPauseTime(float originalTime)
        {
            if (Plugin.ParryTimeFreeze != null && !Plugin.ParryTimeFreeze.Value)
            {
                return 0f;
            }
            return originalTime;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var pauseMethod = AccessTools.Method(typeof(PauseManager), nameof(PauseManager.Pause));
            var unpauseMethod = AccessTools.Method(typeof(PauseManager), nameof(PauseManager.Unpause));
            var safePause = AccessTools.Method(typeof(ParryFreezePatch), nameof(SafeParryPause));
            var safeUnpause = AccessTools.Method(typeof(ParryFreezePatch), nameof(SafeParryUnpause));
            var adjustTime = AccessTools.Method(typeof(ParryFreezePatch), nameof(AdjustParryPauseTime));

            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                var ins = codes[i];

                if (ins.opcode == OpCodes.Call && pauseMethod != null && pauseMethod.Equals(ins.operand))
                {
                    ins.operand = safePause;
                    continue;
                }

                if (ins.opcode == OpCodes.Call && unpauseMethod != null && unpauseMethod.Equals(ins.operand))
                {
                    ins.operand = safeUnpause;
                    continue;
                }

                if (ins.opcode == OpCodes.Stfld && ins.operand is FieldInfo fi && fi.Name.Contains("pauseTime"))
                {
                    codes.Insert(i, new CodeInstruction(OpCodes.Call, adjustTime));
                    i++;
                    continue;
                }
            }

            return codes;
        }
    }
}
