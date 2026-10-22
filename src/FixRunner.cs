using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Executes per-frame logic:
    /// - Re-applies Player 3 custom color tint so lighting shaders do not reset it to plain white.
    /// - Rescues players who fall off-screen during scrolling Run & Gun stages.
    /// - MapWatchdog recovery.
    /// - OnGUI status watermark.
    /// </summary>
    internal class FixRunner : MonoBehaviour
    {
        private readonly Dictionary<PlayerId, float> _lastRescue = new Dictionary<PlayerId, float>();
        private UnityEngine.Camera _cachedCamera;
        private CupheadLevelCamera _cachedOwner;

        private GUIStyle _hudBoxStyle;
        private GUIStyle _hudTextStyle;
        private GUIStyle _hudTitleStyle;
        private GUIStyle _toastBoxStyle;
        private GUIStyle _toastTextStyle;

        private void Update()
        {
            ControllerManager.Update();
            P3LoadoutUI.Update();
            QuickRestartManager.Update();
            if (Input.GetKeyDown(KeyCode.V))
            {
                BossVictorySummaryUI.Toggle();
            }
        }

        private void LateUpdate()
        {
            try
            {
                if (Plugin.PersistExtraTints.Value) ApplyTints();
                if (Plugin.RescueOffscreenPlayers.Value) RescueOffscreen();
                MapWatchdog.Tick();
                HardcoreGhostWatcher.Tick();
                CoopStatsManager.Tick();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("FixRunner.LateUpdate: " + e.Message);
            }
        }

        private void ApplyTints()
        {
            if (Level.Current == null) return;

            var p3 = Extras.GetPlayer(PlayerId.PlayerThree);
            if (p3 != null && !IsChalice(p3))
            {
                var color3 = Extras.ParseColor(Plugin.Player3Color.Value, new Color(0.06f, 1f, 0f));
                Extras.ApplyTint(p3, color3);
                Extras.ApplySuperTint(p3, color3);
            }

            if (Plugin.MaxPlayers.Value >= 4)
            {
                var p4 = Extras.GetPlayer(PlayerId.PlayerFour);
                if (p4 != null && !IsChalice(p4))
                {
                    var color4 = Extras.ParseColor(Plugin.Player4Color.Value, new Color(1f, 1f, 0f));
                    Extras.ApplyTint(p4, color4);
                    Extras.ApplySuperTint(p4, color4);
                }
            }
        }

        private static bool IsChalice(AbstractPlayerController player)
        {
            try
            {
                var stats = player.stats;
                return stats != null && stats.isChalice;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private UnityEngine.Camera ResolveCamera()
        {
            var owner = CupheadLevelCamera.Current;
            if (owner == null) return null;

            if (_cachedOwner != owner || _cachedCamera == null)
            {
                _cachedOwner = owner;
                _cachedCamera = owner.GetComponent<UnityEngine.Camera>();
                if (_cachedCamera == null)
                {
                    _cachedCamera = owner.GetComponentInChildren<UnityEngine.Camera>();
                }
                if (_cachedCamera == null)
                {
                    _cachedCamera = UnityEngine.Camera.main;
                }
            }
            return _cachedCamera;
        }

        private void RescueOffscreen()
        {
            var level = Level.Current;
            if (level == null) return;

            var cam = ResolveCamera();
            if (cam == null) return;

            var anchor = Extras.GetAnchor();
            if (anchor == null) return;

            var margin = Mathf.Max(0f, Plugin.ViewportMargin.Value);
            var cooldown = Mathf.Max(0.1f, Plugin.RescueCooldown.Value);
            var now = Time.unscaledTime;

            foreach (var id in Extras.AllIds)
            {
                var player = Extras.GetLivePlayer(id);
                if (player == null) continue;
                if (ReferenceEquals(player, anchor)) continue;

                if (!player.levelStarted || player.levelEnded) continue;

                float last;
                if (_lastRescue.TryGetValue(id, out last) && now - last < cooldown) continue;

                Vector3 viewport;
                try
                {
                    viewport = cam.WorldToViewportPoint(player.center);
                }
                catch (Exception)
                {
                    continue;
                }

                var lost = viewport.z < 0f
                           || viewport.x < -margin
                           || viewport.y < -margin;

                if (!lost) continue;

                var target = anchor.transform.position;
                target.x += (id == PlayerId.PlayerThree) ? -40f : 40f;

                player.transform.position = target;
                _lastRescue[id] = now;

                Plugin.Log.LogInfo("[3P-Coop] Offscreen rescue: pulled " + id + " to anchor (" + anchor.id + ")");
            }
        }

        private void EnsureStyles()
        {
            if (_hudBoxStyle == null || _hudTitleStyle == null || _hudTitleStyle.font == null)
            {
                CupheadUITheme.ResolveFonts();
                var titleFont = CupheadUITheme.TitleFont;
                var bodyFont = CupheadUITheme.BodyFont;

                if (_hudBoxStyle == null)
                {
                    _hudBoxStyle = new GUIStyle(GUI.skin.box);
                    _hudBoxStyle.normal.background = CupheadUITheme.CreateVintageCardTexture(20, CupheadUITheme.InkBlack, CupheadUITheme.VintageGold, new Color(0.12f, 0.08f, 0.05f, 0.92f), true);
                    _hudBoxStyle.border = new RectOffset(6, 6, 6, 6);
                }

                if (_hudTitleStyle == null)
                {
                    _hudTitleStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.UpperLeft,
                        richText = true
                    };
                }
                _hudTitleStyle.font = titleFont;
                _hudTitleStyle.fontSize = 13;
                _hudTitleStyle.fontStyle = FontStyle.Normal;
                _hudTitleStyle.normal.textColor = CupheadUITheme.VintageGoldBright;

                if (_hudTextStyle == null)
                {
                    _hudTextStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.UpperLeft,
                        richText = true
                    };
                }
                _hudTextStyle.font = bodyFont;
                _hudTextStyle.fontSize = 12;
                _hudTextStyle.fontStyle = FontStyle.Normal;
                _hudTextStyle.normal.textColor = CupheadUITheme.ParchmentCream;

                if (_toastBoxStyle == null)
                {
                    _toastBoxStyle = new GUIStyle(GUI.skin.box);
                    _toastBoxStyle.normal.background = CupheadUITheme.CreateVintageCardTexture(20, CupheadUITheme.InkBlack, CupheadUITheme.VintageGold, new Color(0.14f, 0.09f, 0.06f, 0.96f), true);
                    _toastBoxStyle.border = new RectOffset(6, 6, 6, 6);
                }

                if (_toastTextStyle == null)
                {
                    _toastTextStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        richText = true
                    };
                }
                _toastTextStyle.font = titleFont;
                _toastTextStyle.fontSize = 17;
                _toastTextStyle.fontStyle = FontStyle.Normal;
                _toastTextStyle.normal.textColor = new Color(1f, 0.88f, 0.36f, 1f);
            }
        }

        private static string _lastSceneLogged = null;

        private void Start()
        {
            Application.runInBackground = true;
            Plugin.Log.LogInfo("[3P-Coop] FixRunner service started successfully (runInBackground enabled).");
        }

        private void OnGUI()
        {
            try
            {
                EnsureStyles();

                // 1. Toast Notification (can display on any screen when a hotkey is pressed)
                string toast = ControllerManager.CurrentToast;
                if (!string.IsNullOrEmpty(toast))
                {
                    float tw = 640f;
                    float th = 48f;
                    float tx = (Screen.width - tw) / 2f;
                    float ty = 20f;
                    GUI.Box(new Rect(tx, ty, tw, th), GUIContent.none, _toastBoxStyle);
                    GUI.Label(new Rect(tx, ty, tw, th), toast, _toastTextStyle);
                }

                // 2. Gameplay Watermark HUD
                if (Plugin.ShowCoopWatermark.Value && (Level.Current != null || Map.Current != null))
                {
                    var activeScene = JoinGate.ActiveScene;
                    if (activeScene != _lastSceneLogged)
                    {
                        _lastSceneLogged = activeScene;
                        Plugin.Log.LogInfo("[3P-Coop] Gameplay HUD active in scene: " + activeScene);
                    }

                    bool p1Active = PlayerManager.DoesPlayerExist(PlayerId.PlayerOne) || (Map.Current != null && Map.Current.players != null && Map.Current.players.Length > 0 && Map.Current.players[0] != null);
                    bool p2Active = PlayerManager.DoesPlayerExist(PlayerId.PlayerTwo) || (Map.Current != null && Map.Current.players != null && Map.Current.players.Length > 1 && Map.Current.players[1] != null);
                    bool p3Active = PlayerManager.DoesPlayerExist(PlayerId.PlayerThree) || (Map.Current != null && Map.Current.players != null && Map.Current.players.Length > 2 && Map.Current.players[2] != null);

                    float width = 390f;
                    float height = 94f;
                    float x = Screen.width - width - 15f;
                    float y = 15f;

                    GUI.Box(new Rect(x, y, width, height), GUIContent.none, _hudBoxStyle);

                    GUI.Label(new Rect(x + 10f, y + 6f, width - 20f, 18f), "3-PLAYER CO-OP", _hudTitleStyle);

                    string p1Text = p1Active ? "<color=#FF6666>P1 (Cuphead): Ready</color>" : "<color=#9E8E7C>P1: Off</color>";
                    string p2Text = p2Active ? "<color=#55A0FF>P2 (Mugman): Ready</color>" : "<color=#9E8E7C>P2: Join</color>";
                    string p3Text = p3Active ? "<color=#5CD864>P3: Ready</color>" : "<color=#9E8E7C>P3: Join</color>";

                    string statusText = string.Format("{0} | {1} | {2}", p1Text, p2Text, p3Text);
                    GUI.Label(new Rect(x + 10f, y + 25f, width - 20f, 18f), statusText, _hudTextStyle);

                    string scaleText = string.Format("Damage ({0}): {1:P0}", DamageScaling.CurrentPresetName, DamageScaling.Current());
                    if (Plugin.RestartOnGhostLost.Value)
                    {
                        bool isBoss = Level.Current != null && HardcoreGhostWatcher.IsBossLevel(Level.Current);
                        if (!Plugin.RestartOnlyOnBosses.Value || isBoss)
                        {
                            scaleText += " | <color=#FF5555>Hardcore</color>";
                        }
                    }
                    GUI.Label(new Rect(x + 10f, y + 45f, width - 20f, 18f), scaleText, _hudTextStyle);

                    // Hotkey hints
                    GUI.Label(new Rect(x + 10f, y + 65f, width - 20f, 18f), "<color=#C4B49E>[F1] Klaw  [F3] Dołącz  [F5] Menu  [F6] HC  [F7] Parry  [F8] Dmg  [V] Podsum  [L3] Reset</color>", _hudTextStyle);
                }

                // Draw Player 3 Loadout Selector if opened
                P3LoadoutUI.DrawGUI();

                // Draw Boss Victory Summary Scorecard if active
                BossVictorySummaryUI.DrawGUI();

                // Draw Quick Restart Progress HUD if active
                QuickRestartManager.DrawGUI();
            }
            catch (Exception)
            {
                // Silently ignore GUI frame evaluation errors during scene transitions
            }
        }

        private void OnApplicationQuit()
        {
            CoopStatsManager.Flush();
        }

        private void OnDestroy()
        {
            CoopStatsManager.Flush();
        }
    }
}
