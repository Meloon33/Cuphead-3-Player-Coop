using System;
using HarmonyLib;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Fixes Player 3 (and extra players) during Super art animations:
    /// 1. Ensures proper base model selection (Mugman vs Cuphead) based on Plugin.P3BaseCharacter.
    /// 2. Retains custom color tint (green) during the entire Super animation across ground and plane levels.
    /// 3. Ensures PlayerSuperGhost and PlaneSuperBomb transformations use the appropriate character model and color.
    /// </summary>
    internal static class SuperFixPatch
    {
        [HarmonyPatch(typeof(AbstractPlayerSuper), nameof(AbstractPlayerSuper.Create))]
        internal static class AbstractPlayerSuperCreatePatch
        {
            private static void Postfix(AbstractPlayerSuper __result, LevelPlayerController player)
            {
                if (__result == null || player == null) return;
                try
                {
                    if (player.id == PlayerId.PlayerThree)
                    {
                        var stats = player.stats;
                        if (stats != null && stats.isChalice) return;

                        bool isMugman = (Plugin.P3BaseCharacter != null && Plugin.P3BaseCharacter.Value == 1);
                        var cuphead = Extras.f_pSuperCuphead?.GetValue(__result) as SpriteRenderer;
                        var mugman = Extras.f_pSuperMugman?.GetValue(__result) as SpriteRenderer;

                        if (isMugman)
                        {
                            if (cuphead != null) cuphead.gameObject.SetActive(false);
                            if (mugman != null)
                            {
                                mugman.gameObject.SetActive(true);
                                Extras.f_pSuperRenderer?.SetValue(__result, mugman);
                            }
                        }
                        else
                        {
                            if (mugman != null) mugman.gameObject.SetActive(false);
                            if (cuphead != null)
                            {
                                cuphead.gameObject.SetActive(true);
                                Extras.f_pSuperRenderer?.SetValue(__result, cuphead);
                            }
                        }

                        var ghost = __result as PlayerSuperGhost;
                        if (ghost != null)
                        {
                            var chBottom = Extras.f_ghostCupheadBottom?.GetValue(ghost) as SpriteRenderer;
                            var mmBottom = Extras.f_ghostMugmanBottom?.GetValue(ghost) as SpriteRenderer;
                            if (isMugman)
                            {
                                if (chBottom != null) chBottom.gameObject.SetActive(false);
                                if (mmBottom != null) mmBottom.gameObject.SetActive(true);
                            }
                            else
                            {
                                if (mmBottom != null) mmBottom.gameObject.SetActive(false);
                                if (chBottom != null) chBottom.gameObject.SetActive(true);
                            }
                        }

                        var color = Extras.ParseColor(Plugin.Player3Color.Value, new Color(0.06f, 1f, 0f));
                        Extras.ApplySuperTintToObject(__result, color);
                    }
                    else if (player.id == PlayerId.PlayerFour)
                    {
                        var stats = player.stats;
                        if (stats != null && stats.isChalice) return;

                        var color = Extras.ParseColor(Plugin.Player4Color.Value, new Color(1f, 1f, 0f));
                        Extras.ApplySuperTintToObject(__result, color);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("AbstractPlayerSuper.Create postfix error: " + e.Message);
                }
            }
        }

        [HarmonyPatch(typeof(AbstractPlaneSuper), nameof(AbstractPlaneSuper.Create))]
        internal static class AbstractPlaneSuperCreatePatch
        {
            private static void Postfix(AbstractPlaneSuper __result, PlanePlayerController player)
            {
                if (__result == null || player == null) return;
                try
                {
                    if (player.id == PlayerId.PlayerThree)
                    {
                        var stats = player.stats;
                        if (stats != null && stats.isChalice) return;

                        bool isMugman = (Plugin.P3BaseCharacter != null && Plugin.P3BaseCharacter.Value == 1);
                        var cuphead = Extras.f_planeSuperCuphead?.GetValue(__result) as SpriteRenderer;
                        var mugman = Extras.f_planeSuperMugman?.GetValue(__result) as SpriteRenderer;

                        if (isMugman)
                        {
                            if (cuphead != null) cuphead.gameObject.SetActive(false);
                            if (mugman != null)
                            {
                                mugman.gameObject.SetActive(true);
                                Extras.f_planeSuperRenderer?.SetValue(__result, mugman);
                            }
                        }
                        else
                        {
                            if (mugman != null) mugman.gameObject.SetActive(false);
                            if (cuphead != null)
                            {
                                cuphead.gameObject.SetActive(true);
                                Extras.f_planeSuperRenderer?.SetValue(__result, cuphead);
                            }
                        }

                        var color = Extras.ParseColor(Plugin.Player3Color.Value, new Color(0.06f, 1f, 0f));
                        Extras.ApplySuperTintToObject(__result, color);
                    }
                    else if (player.id == PlayerId.PlayerFour)
                    {
                        var stats = player.stats;
                        if (stats != null && stats.isChalice) return;

                        var color = Extras.ParseColor(Plugin.Player4Color.Value, new Color(1f, 1f, 0f));
                        Extras.ApplySuperTintToObject(__result, color);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("AbstractPlaneSuper.Create postfix error: " + e.Message);
                }
            }
        }

        [HarmonyPatch(typeof(PlaneSuperBomb), "StartSuper")]
        internal static class PlaneSuperBombStartSuperPatch
        {
            private static void Postfix(PlaneSuperBomb __instance)
            {
                if (__instance == null) return;
                try
                {
                    var player = Extras.f_planeSuperPlayer?.GetValue(__instance) as PlanePlayerController;
                    if (player == null) return;

                    if (player.id == PlayerId.PlayerThree)
                    {
                        var stats = player.stats;
                        if (stats != null && stats.isChalice) return;

                        bool isMugman = (Plugin.P3BaseCharacter != null && Plugin.P3BaseCharacter.Value == 1);
                        var boom = Extras.f_bombBoom?.GetValue(__instance) as Transform;
                        var boomMM = Extras.f_bombBoomMM?.GetValue(__instance) as Transform;

                        if (isMugman)
                        {
                            if (boom != null) boom.gameObject.SetActive(false);
                            if (boomMM != null) boomMM.gameObject.SetActive(true);
                        }
                        else
                        {
                            if (boomMM != null) boomMM.gameObject.SetActive(false);
                            if (boom != null) boom.gameObject.SetActive(true);
                        }

                        var color = Extras.ParseColor(Plugin.Player3Color.Value, new Color(0.06f, 1f, 0f));
                        Extras.ApplySuperTintToObject(__instance, color);
                    }
                    else if (player.id == PlayerId.PlayerFour)
                    {
                        var stats = player.stats;
                        if (stats != null && stats.isChalice) return;

                        var color = Extras.ParseColor(Plugin.Player4Color.Value, new Color(1f, 1f, 0f));
                        Extras.ApplySuperTintToObject(__instance, color);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("PlaneSuperBomb.StartSuper postfix error: " + e.Message);
                }
            }
        }

        [HarmonyPatch(typeof(LevelPlayerAnimationController), nameof(LevelPlayerAnimationController.LevelInit))]
        internal static class LevelPlayerAnimationControllerLevelInitPatch
        {
            private static void Postfix(LevelPlayerAnimationController __instance)
            {
                if (__instance == null || __instance.player == null) return;
                try
                {
                    if (__instance.player.id == PlayerId.PlayerThree)
                    {
                        var stats = __instance.player.stats;
                        if (stats != null && stats.isChalice) return;

                        bool isMugman = (Plugin.P3BaseCharacter != null && Plugin.P3BaseCharacter.Value == 1);
                        __instance.SetSprites(!isMugman);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("LevelPlayerAnimationController.LevelInit postfix error: " + e.Message);
                }
            }
        }

        [HarmonyPatch(typeof(PlanePlayerAnimationController), nameof(PlanePlayerAnimationController.LevelInit))]
        internal static class PlanePlayerAnimationControllerLevelInitPatch
        {
            private static void Postfix(PlanePlayerAnimationController __instance)
            {
                if (__instance == null || __instance.player == null) return;
                try
                {
                    if (__instance.player.id == PlayerId.PlayerThree)
                    {
                        var stats = __instance.player.stats;
                        if (stats != null && stats.isChalice) return;

                        bool isMugman = (Plugin.P3BaseCharacter != null && Plugin.P3BaseCharacter.Value == 1);
                        Extras.SetPlanePlayerSprites(__instance, isMugman);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("PlanePlayerAnimationController.LevelInit postfix error: " + e.Message);
                }
            }
        }
    }
}
