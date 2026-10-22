using System;
using System.Reflection;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>Shared, defensive helpers for reaching the extra players.</summary>
    internal static class Extras
    {
        internal static readonly PlayerId[] ExtraIds = { PlayerId.PlayerThree, PlayerId.PlayerFour };
        internal static readonly PlayerId[] AllIds =
        {
            PlayerId.PlayerOne, PlayerId.PlayerTwo, PlayerId.PlayerThree, PlayerId.PlayerFour
        };

        internal static readonly FieldInfo f_pSuperCuphead = typeof(AbstractPlayerSuper).GetField("cuphead", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_pSuperMugman = typeof(AbstractPlayerSuper).GetField("mugman", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_pSuperRenderer = typeof(AbstractPlayerSuper).GetField("spriteRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_pSuperPlayer = typeof(AbstractPlayerSuper).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static readonly FieldInfo f_ghostCupheadBottom = typeof(PlayerSuperGhost).GetField("cupheadBottom", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_ghostMugmanBottom = typeof(PlayerSuperGhost).GetField("mugmanBottom", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static readonly FieldInfo f_planeSuperCuphead = typeof(AbstractPlaneSuper).GetField("cuphead", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_planeSuperMugman = typeof(AbstractPlaneSuper).GetField("mugman", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_planeSuperRenderer = typeof(AbstractPlaneSuper).GetField("spriteRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_planeSuperPlayer = typeof(AbstractPlaneSuper).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static readonly FieldInfo f_bombBoom = typeof(PlaneSuperBomb).GetField("boom", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_bombBoomMM = typeof(PlaneSuperBomb).GetField("boomMM", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static readonly FieldInfo f_pacCuphead = typeof(PlanePlayerAnimationController).GetField("cuphead", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_pacMugman = typeof(PlanePlayerAnimationController).GetField("mugman", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_pacPlayerSprite = typeof(PlanePlayerAnimationController).GetField("playerSprite", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static readonly FieldInfo f_pacSr = typeof(PlanePlayerAnimationController).GetField("<spriteRenderer>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// PlayerManager.GetPlayer indexes a dictionary that is only populated after
        /// PlayerManager.Awake, and blows up for PlayerId.Any/None. Never let that reach a patch.
        /// </summary>
        internal static AbstractPlayerController GetPlayer(PlayerId id)
        {
            try
            {
                var p = PlayerManager.GetPlayer(id);
                // Unity's fake-null: a destroyed controller compares equal to null.
                return (p == null) ? null : p;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>A player that exists, is not dead and whose GameObject is live.</summary>
        internal static AbstractPlayerController GetLivePlayer(PlayerId id)
        {
            var p = GetPlayer(id);
            if (p == null) return null;
            try
            {
                if (p.IsDead) return null;
                if (!p.gameObject.activeInHierarchy) return null;
            }
            catch (Exception)
            {
                return null;
            }
            return p;
        }

        /// <summary>Player 1 if usable, otherwise the first other living player.</summary>
        internal static AbstractPlayerController GetAnchor()
        {
            var anchor = GetLivePlayer(PlayerId.PlayerOne);
            if (anchor != null) return anchor;
            foreach (var id in AllIds)
            {
                var p = GetLivePlayer(id);
                if (p != null) return p;
            }
            return null;
        }

        /// <summary>
        /// Applies a sprite tint to whichever concrete player controller this is.
        /// Level and Plane controllers expose separate animation controllers.
        /// </summary>
        internal static void ApplyTint(AbstractPlayerController player, Color color)
        {
            if (player == null) return;
            try
            {
                var level = player as LevelPlayerController;
                if (level != null)
                {
                    var ac = level.animationController;
                    if (ac != null) ac.SetColor(color);
                    return;
                }

                var plane = player as PlanePlayerController;
                if (plane != null)
                {
                    var ac = plane.animationController;
                    if (ac != null) ac.SetColor(color);
                }
            }
            catch (Exception)
            {
                // A controller mid-destruction is not worth a log spam every frame.
            }
        }

        /// <summary>Parses "#RRGGBB"/"RRGGBB"; returns the fallback when the string is unusable.</summary>
        internal static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            var s = hex.Trim();
            if (!s.StartsWith("#")) s = "#" + s;
            Color parsed;
            return ColorUtility.TryParseHtmlString(s, out parsed) ? parsed : fallback;
        }

        /// <summary>
        /// Sets PlanePlayerAnimationController to Mugman or Cuphead model safely via reflection.
        /// </summary>
        internal static void SetPlanePlayerSprites(PlanePlayerAnimationController ac, bool isMugman)
        {
            if (ac == null) return;
            try
            {
                var cuphead = f_pacCuphead?.GetValue(ac) as Transform;
                var mugman = f_pacMugman?.GetValue(ac) as Transform;

                if (isMugman)
                {
                    if (cuphead != null) cuphead.gameObject.SetActive(false);
                    if (mugman != null)
                    {
                        mugman.gameObject.SetActive(true);
                        f_pacPlayerSprite?.SetValue(ac, mugman);
                        var sr = mugman.GetComponent<SpriteRenderer>();
                        if (sr != null) f_pacSr?.SetValue(ac, sr);
                    }
                }
                else
                {
                    if (mugman != null) mugman.gameObject.SetActive(false);
                    if (cuphead != null)
                    {
                        cuphead.gameObject.SetActive(true);
                        f_pacPlayerSprite?.SetValue(ac, cuphead);
                        var sr = cuphead.GetComponent<SpriteRenderer>();
                        if (sr != null) f_pacSr?.SetValue(ac, sr);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Retrieves the currently active Super animation's SpriteRenderer for a player if casting a Super.
        /// </summary>
        internal static SpriteRenderer GetActiveSuperRenderer(AbstractPlayerController player)
        {
            if (player == null) return null;
            try
            {
                var level = player as LevelPlayerController;
                if (level != null && level.weaponManager != null)
                {
                    var activeSuper = level.weaponManager.activeSuper;
                    if (activeSuper != null && activeSuper.gameObject != null && activeSuper.gameObject.activeInHierarchy)
                    {
                        var sRenderer = f_pSuperRenderer?.GetValue(activeSuper) as SpriteRenderer;
                        if (sRenderer != null && sRenderer.enabled && sRenderer.gameObject.activeInHierarchy)
                        {
                            return sRenderer;
                        }
                        var srs = activeSuper.GetComponentsInChildren<SpriteRenderer>();
                        for (int i = 0; i < srs.Length; i++)
                        {
                            if (srs[i] != null && srs[i].enabled && srs[i].gameObject.activeInHierarchy)
                            {
                                return srs[i];
                            }
                        }
                    }
                }

                var plane = player as PlanePlayerController;
                if (plane != null)
                {
                    var allPlaneSupers = UnityEngine.Object.FindObjectsOfType<AbstractPlaneSuper>();
                    for (int i = 0; i < allPlaneSupers.Length; i++)
                    {
                        var s = allPlaneSupers[i];
                        if (s == null || !s.gameObject.activeInHierarchy) continue;
                        var sPlayer = f_planeSuperPlayer?.GetValue(s) as PlanePlayerController;
                        if (sPlayer != plane) continue;

                        var bomb = s as PlaneSuperBomb;
                        if (bomb != null)
                        {
                            var boomMM = f_bombBoomMM?.GetValue(bomb) as Transform;
                            if (boomMM != null && boomMM.gameObject.activeInHierarchy)
                            {
                                var sr = boomMM.GetComponentInChildren<SpriteRenderer>();
                                if (sr != null && sr.enabled && sr.gameObject.activeInHierarchy) return sr;
                            }
                            var boom = f_bombBoom?.GetValue(bomb) as Transform;
                            if (boom != null && boom.gameObject.activeInHierarchy)
                            {
                                var sr = boom.GetComponentInChildren<SpriteRenderer>();
                                if (sr != null && sr.enabled && sr.gameObject.activeInHierarchy) return sr;
                            }
                        }

                        var sRenderer = f_planeSuperRenderer?.GetValue(s) as SpriteRenderer;
                        if (sRenderer != null && sRenderer.enabled && sRenderer.gameObject.activeInHierarchy)
                        {
                            return sRenderer;
                        }
                        var srs = s.GetComponentsInChildren<SpriteRenderer>();
                        for (int j = 0; j < srs.Length; j++)
                        {
                            if (srs[j] != null && srs[j].enabled && srs[j].gameObject.activeInHierarchy)
                            {
                                return srs[j];
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>
        /// Tints the active Super art sprites for a player.
        /// </summary>
        internal static void ApplySuperTint(AbstractPlayerController player, Color color)
        {
            if (player == null) return;
            try
            {
                var level = player as LevelPlayerController;
                if (level != null && level.weaponManager != null)
                {
                    var activeSuper = level.weaponManager.activeSuper;
                    if (activeSuper != null && activeSuper.gameObject != null && activeSuper.gameObject.activeInHierarchy)
                    {
                        ApplySuperTintToObject(activeSuper, color);
                    }
                    return;
                }

                var plane = player as PlanePlayerController;
                if (plane != null)
                {
                    var allPlaneSupers = UnityEngine.Object.FindObjectsOfType<AbstractPlaneSuper>();
                    for (int i = 0; i < allPlaneSupers.Length; i++)
                    {
                        var s = allPlaneSupers[i];
                        if (s == null || !s.gameObject.activeInHierarchy) continue;
                        var sPlayer = f_planeSuperPlayer?.GetValue(s) as PlanePlayerController;
                        if (sPlayer == plane)
                        {
                            ApplySuperTintToObject(s, color);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Tints all character-related renderers on a ground Super instance.
        /// </summary>
        internal static void ApplySuperTintToObject(AbstractPlayerSuper s, Color color)
        {
            if (s == null) return;
            try
            {
                var cuphead = f_pSuperCuphead?.GetValue(s) as SpriteRenderer;
                if (cuphead != null) cuphead.color = color;

                var mugman = f_pSuperMugman?.GetValue(s) as SpriteRenderer;
                if (mugman != null) mugman.color = color;

                var srMain = f_pSuperRenderer?.GetValue(s) as SpriteRenderer;
                if (srMain != null) srMain.color = color;

                var ghost = s as PlayerSuperGhost;
                if (ghost != null)
                {
                    var chBottom = f_ghostCupheadBottom?.GetValue(ghost) as SpriteRenderer;
                    if (chBottom != null) chBottom.color = color;

                    var mmBottom = f_ghostMugmanBottom?.GetValue(ghost) as SpriteRenderer;
                    if (mmBottom != null) mmBottom.color = color;
                }

                var srs = s.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < srs.Length; i++)
                {
                    var sr = srs[i];
                    if (sr == null) continue;
                    if (sr.name != null && sr.name.IndexOf("heart", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    sr.color = color;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Tints all character-related renderers on a plane Super instance.
        /// </summary>
        internal static void ApplySuperTintToObject(AbstractPlaneSuper s, Color color)
        {
            if (s == null) return;
            try
            {
                var cuphead = f_planeSuperCuphead?.GetValue(s) as SpriteRenderer;
                if (cuphead != null) cuphead.color = color;

                var mugman = f_planeSuperMugman?.GetValue(s) as SpriteRenderer;
                if (mugman != null) mugman.color = color;

                var srMain = f_planeSuperRenderer?.GetValue(s) as SpriteRenderer;
                if (srMain != null) srMain.color = color;

                var bomb = s as PlaneSuperBomb;
                if (bomb != null)
                {
                    var boom = f_bombBoom?.GetValue(bomb) as Transform;
                    if (boom != null)
                    {
                        var srs = boom.GetComponentsInChildren<SpriteRenderer>(true);
                        for (int i = 0; i < srs.Length; i++)
                            if (srs[i] != null) srs[i].color = color;
                    }
                    var boomMM = f_bombBoomMM?.GetValue(bomb) as Transform;
                    if (boomMM != null)
                    {
                        var srs = boomMM.GetComponentsInChildren<SpriteRenderer>(true);
                        for (int i = 0; i < srs.Length; i++)
                            if (srs[i] != null) srs[i].color = color;
                    }
                }

                var allSrs = s.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < allSrs.Length; i++)
                {
                    if (allSrs[i] != null) allSrs[i].color = color;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
