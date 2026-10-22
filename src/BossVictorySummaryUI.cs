using System;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Renders the 1930s vintage Boss Victory Scorecard.
    /// Shows two distinct cards after victory:
    /// 1. The decisive winning attempt stats (single successful fight).
    /// 2. Cumulative all-attempts stats across the whole boss fight session.
    /// Supports switching via tabs, [Enter], [Space], [Tab], or arrows.
    /// </summary>
    public static class BossVictorySummaryUI
    {
        public static bool IsOpen { get; set; } = false;
        private static BossRunSummary _winningSummary = null;
        private static BossRunSummary _cumulativeSummary = null;
        private static int _activeCard = 0; // 0 = Winning Attempt, 1 = Cumulative All Attempts

        private static GUIStyle _windowStyle;
        private static GUIStyle _slotBoxStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _subHeaderStyle;
        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _trophyTitleStyle;
        private static GUIStyle _trophyDescStyle;
        private static GUIStyle _tabActiveStyle;
        private static GUIStyle _tabInactiveStyle;

        private static Vector2 _trophyScrollPos = Vector2.zero;
        private static int _lastFontMode = -1;
        private static float _lastFontAspect = -1f;

        public static void InvalidateStyles()
        {
            _windowStyle = null;
        }

        private static void SetCursor(bool visible)
        {
            try
            {
                ApplyCursor(visible);
            }
            catch { }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ApplyCursor(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Confined;
        }

        public static void Open(BossRunSummary winningSummary, BossRunSummary cumulativeSummary)
        {
            _winningSummary = winningSummary;
            _cumulativeSummary = cumulativeSummary;
            _activeCard = 0; // Start on winning fight
            IsOpen = true;
            _trophyScrollPos = Vector2.zero;
            SetCursor(true);
        }

        public static void Open(BossRunSummary summary)
        {
            if (summary == null) return;
            if (summary.isWinningAttemptOnly)
            {
                Open(summary, BossSessionTracker.LastCumulativeSummary ?? summary);
            }
            else
            {
                Open(BossSessionTracker.LastWinningSummary ?? summary, summary);
            }
        }

        public static void Toggle()
        {
            if (IsOpen)
            {
                IsOpen = false;
                SetCursor(false);
            }
            else if (BossSessionTracker.HasLastSummary)
            {
                Open(BossSessionTracker.LastWinningSummary, BossSessionTracker.LastCumulativeSummary);
            }
            else
            {
                ControllerManager.ShowToast("Brak zapisanej ostatniej walki z bossem w tej sesji!");
            }
        }

        public static void UpdateInput()
        {
            if (!IsOpen) return;

            SetCursor(true);

            if (Input.GetKeyDown(KeyCode.Escape) ||
                Input.GetKeyDown(KeyCode.JoystickButton1) ||
                Input.GetKeyDown(KeyCode.JoystickButton7) ||
                Input.GetKeyDown(KeyCode.V))
            {
                IsOpen = false;
                SetCursor(false);
                return;
            }

            // Tab switching with Q / E / Tab / Left / Right / A / D / Gamepad LB / RB
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                _activeCard = 0;
                _trophyScrollPos = Vector2.zero;
                return;
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.JoystickButton5))
            {
                _activeCard = 1;
                _trophyScrollPos = Vector2.zero;
                return;
            }

            // Progression button with Enter / Space / Gamepad A
            if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.JoystickButton0))
            {
                if (_activeCard == 0 && _cumulativeSummary != null && _cumulativeSummary.totalAttempts > 1)
                {
                    _activeCard = 1;
                    _trophyScrollPos = Vector2.zero;
                }
                else
                {
                    IsOpen = false;
                    SetCursor(false);
                }
            }
        }

        private static void EnsureStyles()
        {
            int curMode = Plugin.UIFontMode != null ? Plugin.UIFontMode.Value : 0;
            float curAspect = Plugin.UIFontAspectScale != null ? Plugin.UIFontAspectScale.Value : 0.88f;

            if (_windowStyle != null && _windowStyle.normal.background != null && _tabActiveStyle != null && _lastFontMode == curMode && Mathf.Approximately(_lastFontAspect, curAspect)) return;

            _lastFontMode = curMode;
            _lastFontAspect = curAspect;

            CupheadUITheme.ResolveFonts();
            var titleFont = CupheadUITheme.TitleFont;
            var bodyFont = CupheadUITheme.BodyFont;

            _windowStyle = new GUIStyle(GUI.skin.box);
            _windowStyle.normal.background = CupheadUITheme.CreateVintageCardTexture(24, CupheadUITheme.InkBlack, CupheadUITheme.VintageGold, CupheadUITheme.CocoaDark, true);
            _windowStyle.border = new RectOffset(7, 7, 7, 7);

            _slotBoxStyle = new GUIStyle(GUI.skin.box);
            _slotBoxStyle.normal.background = CupheadUITheme.CreateInsetSlotTexture(16, CupheadUITheme.InkBlack, CupheadUITheme.CocoaSlot);
            _slotBoxStyle.border = new RectOffset(4, 4, 4, 4);

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont,
                fontSize = 30,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _headerStyle.normal.textColor = CupheadUITheme.VintageGoldBright;

            _subHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _subHeaderStyle.normal.textColor = CupheadUITheme.ParchmentCream;

            _sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont,
                fontSize = 22,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _sectionHeaderStyle.normal.textColor = CupheadUITheme.VintageGold;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
            _labelStyle.normal.textColor = CupheadUITheme.ParchmentCream;

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 19,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleRight,
                richText = true
            };
            _valueStyle.normal.textColor = new Color(1f, 0.88f, 0.38f, 1f);

            _trophyTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont,
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
            _trophyTitleStyle.normal.textColor = CupheadUITheme.VintageGoldBright;

            _trophyDescStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
            _trophyDescStyle.normal.textColor = CupheadUITheme.ParchmentCream;

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = titleFont,
                fontSize = 19,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _buttonStyle.normal.background = CupheadUITheme.CreateButtonTexture(16, CupheadUITheme.InkBlack, CupheadUITheme.VintageGold, new Color(0.25f, 0.17f, 0.11f, 1f));
            _buttonStyle.normal.textColor = CupheadUITheme.ParchmentCream;
            _buttonStyle.hover.background = CupheadUITheme.CreateButtonTexture(16, CupheadUITheme.InkBlack, new Color(1f, 0.88f, 0.40f, 1f), CupheadUITheme.CupheadRedHover);
            _buttonStyle.hover.textColor = Color.white;
            _buttonStyle.active.background = CupheadUITheme.CreateButtonTexture(16, CupheadUITheme.InkBlack, new Color(1f, 0.88f, 0.40f, 1f), CupheadUITheme.CupheadRed);
            _buttonStyle.active.textColor = Color.white;
            _buttonStyle.border = new RectOffset(3, 3, 3, 3);

            _tabActiveStyle = new GUIStyle(GUI.skin.button)
            {
                font = titleFont,
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _tabActiveStyle.normal.background = CupheadUITheme.CreateButtonTexture(16, CupheadUITheme.InkBlack, new Color(1f, 0.94f, 0.55f, 1f), new Color(0.88f, 0.66f, 0.22f, 1f));
            _tabActiveStyle.normal.textColor = new Color(0.12f, 0.07f, 0.04f, 1f);
            _tabActiveStyle.border = new RectOffset(3, 3, 3, 3);

            _tabInactiveStyle = new GUIStyle(GUI.skin.button)
            {
                font = titleFont,
                fontSize = 17,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _tabInactiveStyle.normal.background = CupheadUITheme.CreateButtonTexture(16, CupheadUITheme.InkBlack, new Color(0.32f, 0.22f, 0.15f, 1f), new Color(0.18f, 0.12f, 0.08f, 1f));
            _tabInactiveStyle.normal.textColor = new Color(0.78f, 0.70f, 0.58f, 1f);
            _tabInactiveStyle.border = new RectOffset(3, 3, 3, 3);
        }

        public static void DrawGUI()
        {
            if (!IsOpen) return;

            BossRunSummary currentSummary = (_activeCard == 0 ? _winningSummary : _cumulativeSummary) ?? _winningSummary ?? _cumulativeSummary;
            if (currentSummary == null) return;

            UpdateInput();
            EnsureStyles();

            float width = Mathf.Clamp(Screen.width * 0.72f, 980f, 1380f);
            float height = Mathf.Clamp(Screen.height * 0.82f, 760f, 960f);
            if (width > Screen.width - 40f) width = Screen.width - 40f;
            if (height > Screen.height - 40f) height = Screen.height - 40f;
            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, _windowStyle);

            float cx = x + 24f;
            float cy = y + 16f;
            float cw = width - 48f;

            int totalAttempts = _cumulativeSummary != null ? _cumulativeSummary.totalAttempts : (_winningSummary != null ? _winningSummary.attemptNumber : 1);
            int winningRound = _winningSummary != null ? _winningSummary.attemptNumber : totalAttempts;

            // 1. Dual Card Tabs Switcher at top
            float tabH = 38f;
            float tabW = (cw - 12f) / 2f;
            GUIStyle tab0Style = (_activeCard == 0) ? _tabActiveStyle : _tabInactiveStyle;
            GUIStyle tab1Style = (_activeCard == 1) ? _tabActiveStyle : _tabInactiveStyle;

            string tab0Text = string.Format("1. ZWYCIĘSKA WALKA (Runda #{0})", winningRound);
            string tab1Text = string.Format("2. WSZYSTKIE PODEJŚCIA ({0} prób)", totalAttempts);

            if (CupheadUITheme.DrawButton(new Rect(cx, cy, tabW, tabH), tab0Text, tab0Style))
            {
                _activeCard = 0;
                _trophyScrollPos = Vector2.zero;
            }
            if (CupheadUITheme.DrawButton(new Rect(cx + tabW + 12f, cy, tabW, tabH), tab1Text, tab1Style))
            {
                _activeCard = 1;
                _trophyScrollPos = Vector2.zero;
            }
            cy += tabH + 10f;

            // 2. Header Banner
            string headerTitle = (_activeCard == 0) ? "WIELKI NOKAUT – ZWYCIĘSKA WALKA" : "WIELKI NOKAUT – CAŁA SESJA WALKI";
            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 32f), headerTitle, _headerStyle);
            cy += 32f;

            string subText = (_activeCard == 0)
                ? string.Format("<color=#FFDD88>{0}</color>  •  Decydująca runda: <color=#5CD864>Podejście #{1}</color> (Zwycięstwo!)", currentSummary.bossName, currentSummary.attemptNumber)
                : string.Format("<color=#FFDD88>{0}</color>  •  Wspólna sesja: <color=#5CD864>{1} podejść</color> łącznie do sukcesu", currentSummary.bossName, currentSummary.totalAttempts);
            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 22f), subText, _subHeaderStyle);
            cy += 26f;

            // 3. Session Stats Ribbon
            float ribbonH = 46f;
            GUI.Box(new Rect(cx, cy, cw, ribbonH), GUIContent.none, _slotBoxStyle);
            float colW = cw / 4f;

            DrawRibbonCol(cx, cy, colW, ribbonH, "ŁĄCZNE OBRAŻENIA", string.Format("{0:N0}", currentSummary.totalTeamDamage));
            string attemptsLabel = (_activeCard == 0) ? string.Format("#{0} (Wygrana)", currentSummary.attemptNumber) : currentSummary.totalAttempts.ToString();
            DrawRibbonCol(cx + colW, cy, colW, ribbonH, (_activeCard == 0 ? "RUNDA" : "PODEJŚCIA"), attemptsLabel);
            DrawRibbonCol(cx + colW * 2f, cy, colW, ribbonH, "PAROWANIA", currentSummary.totalTeamParries.ToString());
            DrawRibbonCol(cx + colW * 3f, cy, colW, ribbonH, "WSKRZESZENIA", currentSummary.totalTeamRevives.ToString());
            cy += ribbonH + 10f;

            // 4. Player Breakdown Columns
            float pColW = (cw - 24f) / 3f;
            float pColH = 250f;

            DrawPlayerColumn(cx, cy, pColW, pColH, "Cuphead (Czerwony)", "#FF6666", currentSummary.p1, currentSummary.totalTeamDamage, currentSummary.totalAttempts, currentSummary.isWinningAttemptOnly);
            DrawPlayerColumn(cx + pColW + 12f, cy, pColW, pColH, "Mugman (Niebieski)", "#55A0FF", currentSummary.p2, currentSummary.totalTeamDamage, currentSummary.totalAttempts, currentSummary.isWinningAttemptOnly);
            DrawPlayerColumn(cx + (pColW + 12f) * 2f, cy, pColW, pColH, "Gracz 3 (Zielony)", "#5CD864", currentSummary.p3, currentSummary.totalTeamDamage, currentSummary.totalAttempts, currentSummary.isWinningAttemptOnly);
            cy += pColH + 10f;

            // 5. Trophies / Honors Section
            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 24f), "LAURY I ODZNACZENIA DRUŻYNOWE", _sectionHeaderStyle);
            cy += 26f;

            float footerReservedH = 78f;
            float trophyBoxH = Mathf.Max(120f, (y + height - footerReservedH) - cy);
            GUI.Box(new Rect(cx, cy, cw, trophyBoxH), GUIContent.none, _slotBoxStyle);

            Rect trophyViewRect = new Rect(0, 0, cw - 24f, Mathf.Max(trophyBoxH, currentSummary.trophies.Count * 54f + 8f));
            _trophyScrollPos = GUI.BeginScrollView(new Rect(cx + 6f, cy + 6f, cw - 12f, trophyBoxH - 12f), _trophyScrollPos, trophyViewRect);

            float ty = 4f;
            for (int i = 0; i < currentSummary.trophies.Count; i++)
            {
                var t = currentSummary.trophies[i];
                string tag = !string.IsNullOrEmpty(t.icon) ? string.Format("<color=#D4AF37><b>{0}</b></color> ", t.icon) : "";
                string tHeader = string.Format("{0}<color=#FFDD88><b>{1}</b></color>  —  <color={2}><b>{3}</b></color>", tag, t.title, t.colorHex, t.playerName);
                CupheadUITheme.DrawLabel(new Rect(10f, ty, cw - 44f, 24f), tHeader, _trophyTitleStyle);
                ty += 22f;

                CupheadUITheme.DrawLabel(new Rect(16f, ty, cw - 52f, 24f), t.description, _trophyDescStyle);
                ty += 28f;
            }

            GUI.EndScrollView();
            cy += trophyBoxH + 10f;

            // 6. Footer Action Buttons
            float btnH = 40f;
            if (_activeCard == 0 && _cumulativeSummary != null && _cumulativeSummary.totalAttempts > 1)
            {
                float closeW = 240f;
                float nextW = cw - closeW - 12f;

                if (CupheadUITheme.DrawButton(new Rect(cx, cy, closeW, btnH), "ZAMKNIJ [Esc]", _buttonStyle))
                {
                    IsOpen = false;
                    SetCursor(false);
                }

                string nextText = string.Format("WSZYSTKIE PODEJŚCIA ({0})  [Enter / Spacja / D]", _cumulativeSummary.totalAttempts);
                if (CupheadUITheme.DrawButton(new Rect(cx + closeW + 12f, cy, nextW, btnH), nextText, _buttonStyle))
                {
                    _activeCard = 1;
                    _trophyScrollPos = Vector2.zero;
                }
            }
            else
            {
                if (_activeCard == 1 && _winningSummary != null && _cumulativeSummary != null && _cumulativeSummary.totalAttempts > 1)
                {
                    float prevW = 300f;
                    float closeW = cw - prevW - 12f;

                    string prevText = string.Format("WYGRANA WALKA (#{0})  [A]", _winningSummary.attemptNumber);
                    if (CupheadUITheme.DrawButton(new Rect(cx, cy, prevW, btnH), prevText, _buttonStyle))
                    {
                        _activeCard = 0;
                        _trophyScrollPos = Vector2.zero;
                    }

                    if (CupheadUITheme.DrawButton(new Rect(cx + prevW + 12f, cy, closeW, btnH), "ZAMKNIJ KARTĘ  [Enter / Spacja / Esc]", _buttonStyle))
                    {
                        IsOpen = false;
                        SetCursor(false);
                    }
                }
                else
                {
                    float btnW = 440f;
                    float btnX = cx + (cw - btnW) / 2f;
                    if (CupheadUITheme.DrawButton(new Rect(btnX, cy, btnW, btnH), "DALEJ  [Enter / Spacja / A / Esc]", _buttonStyle))
                    {
                        IsOpen = false;
                        SetCursor(false);
                    }
                }
            }
            cy += btnH + 4f;

            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 20f), "<color=#AAA090>[Q / E / Tab / ← / →] Przełącz kartę   •   [V] Otwórz ponownie tę kartę w dowolnym momencie na mapie</color>", _subHeaderStyle);
        }

        private static void DrawRibbonCol(float rx, float ry, float rw, float rh, string label, string val)
        {
            CupheadUITheme.DrawLabel(new Rect(rx, ry + 4f, rw, 18f), string.Format("<color=#C4B49E>{0}</color>", label), _subHeaderStyle);
            CupheadUITheme.DrawLabel(new Rect(rx, ry + 22f, rw, 22f), string.Format("<color=#FFDD88><b>{0}</b></color>", val), _subHeaderStyle);
        }

        private static void DrawPlayerColumn(float px, float py, float pw, float ph, string title, string colorHex, PlayerBossRunStats st, float totalTeamDmg, int attempts, bool isWinningAttemptOnly)
        {
            GUI.Box(new Rect(px, py, pw, ph), GUIContent.none, _slotBoxStyle);

            float ix = px + 12f;
            float iy = py + 8f;
            float iw = pw - 24f;

            // Player Title
            CupheadUITheme.DrawLabel(new Rect(ix, iy, iw, 24f), string.Format("<color={0}><b>{1}</b></color>", colorHex, title), _sectionHeaderStyle);
            iy += 32f;

            // Damage Dealt
            float pct = (totalTeamDmg > 0f) ? (st.damageDealt / totalTeamDmg) : 0f;
            DrawStatLine(ix, iy, iw, "Obrażenia:", string.Format("{0:N0} ({1:P0})", st.damageDealt, pct));
            iy += 24f;

            // Parries
            DrawStatLine(ix, iy, iw, "Parowania:", st.parries.ToString());
            iy += 24f;

            // Revives Given
            DrawStatLine(ix, iy, iw, "Uratowani koledzy:", st.revivesGiven.ToString());
            iy += 24f;

            // Supers Used
            DrawStatLine(ix, iy, iw, "Superciosy:", st.supersUsed.ToString());
            iy += 24f;

            // Survival Rate
            string survText;
            if (isWinningAttemptOnly)
            {
                survText = (st.deaths == 0) ? "<color=#5CD864>Przeżył walkę!</color>" : "<color=#FF8866>Poległ w boju</color>";
            }
            else
            {
                int survived = Mathf.Max(0, attempts - st.deaths);
                survText = (st.deaths == 0) ? "<color=#5CD864>Nienaruszony!</color>" : string.Format("{0} / {1} prób", survived, attempts);
            }
            DrawStatLine(ix, iy, iw, "Przetrwanie:", survText);
        }

        private static void DrawStatLine(float sx, float sy, float sw, string label, string val)
        {
            float labelW = Mathf.Max(140f, sw * 0.48f);
            CupheadUITheme.DrawLabel(new Rect(sx, sy, labelW, 22f), label, _labelStyle);
            CupheadUITheme.DrawLabel(new Rect(sx + labelW, sy, sw - labelW - 6f, 22f), val, _valueStyle);
        }
    }
}
