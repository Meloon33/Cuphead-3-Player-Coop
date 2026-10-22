using System;
using UnityEngine;
using Rewired;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Instantly restarts the current level upon holding the Left Stick click (L3 / LS)
    /// on any connected gamepad, or holding [R] on the keyboard.
    /// Features a vintage Fleischer-styled countdown HUD indicator while held.
    /// </summary>
    public static class QuickRestartManager
    {
        private static float _holdTimer = 0f;
        private static bool _hasTriggeredThisPress = false;
        private static bool _isRestarting = false;

        private static GUIStyle _boxStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _subStyle;
        private static Texture2D _progressBgTex;
        private static Texture2D _progressFillTex;

        public static float HoldProgress
        {
            get
            {
                float target = TargetDuration;
                if (target <= 0.05f) return 0f;
                return Mathf.Clamp01(_holdTimer / target);
            }
        }

        public static float TargetDuration
        {
            get
            {
                if (Plugin.QuickRestartHoldDuration == null) return 1.0f;
                return Mathf.Max(0.2f, Plugin.QuickRestartHoldDuration.Value);
            }
        }

        public static bool IsEnabled
        {
            get
            {
                if (Plugin.QuickRestartEnabled == null) return true;
                return Plugin.QuickRestartEnabled.Value;
            }
        }

        public static void ResetOnLevelStart()
        {
            _holdTimer = 0f;
            _hasTriggeredThisPress = false;
            _isRestarting = false;
        }

        public static void Update()
        {
            if (!IsEnabled)
            {
                _holdTimer = 0f;
                return;
            }

            // Only allow quick restart during active Level gameplay (not on World Map, menus, or ending)
            if (Level.Current == null || Level.Current.Ending || _isRestarting)
            {
                _holdTimer = 0f;
                return;
            }

            // Block if custom in-game UI menus are open
            if (P3LoadoutUI.IsOpen || BossVictorySummaryUI.IsOpen)
            {
                _holdTimer = 0f;
                return;
            }

            // Check if left stick (or R key) is held
            bool isHeld = CheckInput();

            if (!isHeld)
            {
                _hasTriggeredThisPress = false;
                _holdTimer = 0f;
                return;
            }

            // If already triggered on this same press, wait until released
            if (_hasTriggeredThisPress) return;

            _holdTimer += Time.unscaledDeltaTime;

            if (_holdTimer >= TargetDuration)
            {
                _hasTriggeredThisPress = true;
                _holdTimer = TargetDuration;
                ExecuteRestart();
            }
        }

        public static bool CheckInput()
        {
            try
            {
                // 1. Unity legacy joystick button 8 (standard Left Stick / L3 across all joysticks)
                if (Input.GetKey(KeyCode.JoystickButton8) ||
                    Input.GetKey(KeyCode.Joystick1Button8) ||
                    Input.GetKey(KeyCode.Joystick2Button8) ||
                    Input.GetKey(KeyCode.Joystick3Button8) ||
                    Input.GetKey(KeyCode.Joystick4Button8))
                {
                    return true;
                }

                // 2. Keyboard shortcut [R]
                if (Input.GetKey(KeyCode.R))
                {
                    return true;
                }

                // 3. Rewired Joysticks
                if (ReInput.isReady && ReInput.controllers != null)
                {
                    var joysticks = ReInput.controllers.Joysticks;
                    if (joysticks != null)
                    {
                        int count = joysticks.Count;
                        for (int i = 0; i < count; i++)
                        {
                            var joy = joysticks[i];
                            if (joy == null) continue;

                            // Standard button 8 (LS / L3)
                            if (joy.buttonCount > 8 && joy.GetButton(8))
                            {
                                return true;
                            }

                            // Check element identifier names
                            var ids = joy.ButtonElementIdentifiers;
                            if (ids != null)
                            {
                                int idCount = ids.Count;
                                for (int b = 0; b < idCount; b++)
                                {
                                    var elem = ids[b];
                                    if (elem != null && !string.IsNullOrEmpty(elem.name))
                                    {
                                        string n = elem.name;
                                        if (n.IndexOf("Left Stick", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            n.IndexOf("L3", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            n.IndexOf("LeftStick", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            if (joy.GetButton(b)) return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore input query errors
            }

            return false;
        }

        public static void ExecuteRestart()
        {
            if (_isRestarting) return;
            _isRestarting = true;

            try
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogInfo("[3P-Coop] Quick Restart: Reloading level on Left Stick hold...");
                }

                ControllerManager.ShowToast("Restartowanie poziomu...", 2.0f);

                // Unpause if paused
                try
                {
                    PauseManager.Unpause();
                }
                catch { }
                Time.timeScale = 1f;

                // Safe player state
                try
                {
                    PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerOne, false);
                    PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerTwo, false);
                    PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerThree, false);
                    PlayerManager.SetPlayerCanSwitch(PlayerId.PlayerFour, false);
                }
                catch { }

                try
                {
                    Dialoguer.EndDialogue();
                }
                catch { }

                try
                {
                    if (Level.IsDicePalaceMain || Level.IsDicePalace)
                    {
                        DicePalaceMainLevelGameInfo.CleanUpRetry();
                    }
                }
                catch { }

                SceneLoader.ReloadLevel();
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogError("[3P-Coop] Quick Restart error: " + e);
                }
                SceneLoader.ReloadLevel();
            }
        }

        public static void DrawGUI()
        {
            // Only draw progress when holding stick for at least 0.12s
            if (!IsEnabled || _holdTimer < 0.12f) return;
            if (Level.Current == null || Level.Current.Ending) return;

            EnsureStyles();

            float width = 360f;
            float height = 52f;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height - 110f;

            // Vintage outer box
            GUI.Box(new Rect(x, y, width, height), GUIContent.none, _boxStyle);

            float cx = x + 12f;
            float cy = y + 6f;
            float cw = width - 24f;

            float pct = HoldProgress;
            string titleText = (_holdTimer >= TargetDuration || _isRestarting)
                ? "<color=#5CD864><b>RESTARTOWANIE POZIOMU...</b></color>"
                : string.Format("<color=#FFDD88><b>RESTART POZIOMU ({0:P0})</b></color>", pct);

            GUI.Label(new Rect(cx, cy, cw, 18f), titleText, _titleStyle);
            cy += 18f;

            // Progress bar
            float barW = cw;
            float barH = 8f;
            GUI.DrawTexture(new Rect(cx, cy, barW, barH), _progressBgTex);

            float fillW = barW * pct;
            if (fillW > 0f)
            {
                GUI.DrawTexture(new Rect(cx, cy, fillW, barH), _progressFillTex);
            }
            cy += barH + 3f;

            GUI.Label(new Rect(cx, cy, cw, 14f), "<color=#C4B49E>Przytrzymaj lewą gałkę [L3] lub klawisz [R]</color>", _subStyle);
        }

        private static void EnsureStyles()
        {
            if (_boxStyle != null && _boxStyle.normal.background != null) return;

            var titleFont = CupheadUITheme.TitleFont;
            var bodyFont = CupheadUITheme.BodyFont;

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = CupheadUITheme.CreateVintageCardTexture(16, CupheadUITheme.InkBlack, CupheadUITheme.VintageGold, CupheadUITheme.CocoaDark, true);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _titleStyle.normal.textColor = CupheadUITheme.VintageGoldBright;

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _subStyle.normal.textColor = CupheadUITheme.ParchmentCream;

            _progressBgTex = MakeSolidTex(new Color(0.12f, 0.08f, 0.06f, 0.9f));
            _progressFillTex = MakeSolidTex(new Color(1f, 0.88f, 0.38f, 1f));
        }

        private static Texture2D MakeSolidTex(Color color)
        {
            var t = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            t.SetPixel(0, 0, color);
            t.Apply();
            return t;
        }
    }
}
