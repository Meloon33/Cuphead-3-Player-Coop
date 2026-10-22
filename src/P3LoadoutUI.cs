using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Safe, non-invasive Loadout and Co-op Settings menu.
    /// Rendered via Unity OnGUI centered on screen with visible mouse pointer.
    /// Supports full keyboard, gamepad, and mouse navigation.
    /// Toggleable with [F5].
    /// </summary>
    internal static class P3LoadoutUI
    {
        public static bool IsOpen { get; set; } = false;

        private static readonly WeaponInfo[] AllWeapons = new[]
        {
            new WeaponInfo(Weapon.level_weapon_peashot, "Peashooter"),
            new WeaponInfo(Weapon.level_weapon_spreadshot, "Spread"),
            new WeaponInfo(Weapon.level_weapon_homing, "Chaser"),
            new WeaponInfo(Weapon.level_weapon_boomerang, "Roundabout"),
            new WeaponInfo(Weapon.level_weapon_charge, "Charge"),
            new WeaponInfo(Weapon.level_weapon_arc, "Lobber"),
            new WeaponInfo(Weapon.level_weapon_crackshot, "Crackshot (DLC)"),
            new WeaponInfo(Weapon.level_weapon_wide_shot, "Converge (DLC)"),
            new WeaponInfo(Weapon.level_weapon_upshot, "Twist-Up (DLC)"),
            new WeaponInfo(Weapon.None, "None")
        };

        private static readonly SuperInfo[] AllSupers = new[]
        {
            new SuperInfo(Super.level_super_beam, "Super I (Energy Beam)"),
            new SuperInfo(Super.level_super_invincible, "Super II (Invincibility)"),
            new SuperInfo(Super.level_super_ghost, "Super III (Giant Ghost)"),
            new SuperInfo(Super.None, "None")
        };

        private static readonly CharmInfo[] AllCharms = new[]
        {
            new CharmInfo(Charm.None, "None"),
            new CharmInfo(Charm.charm_chalice, "Astral Cookie (Ms. Chalice)"),
            new CharmInfo(Charm.charm_smoke_dash, "Smoke Bomb"),
            new CharmInfo(Charm.charm_health_up_1, "Heart (+1 HP)"),
            new CharmInfo(Charm.charm_health_up_2, "Twin Heart (+2 HP)"),
            new CharmInfo(Charm.charm_super_builder, "Coffee (Super Builder)"),
            new CharmInfo(Charm.charm_parry_plus, "Sugar (Auto-Parry)"),
            new CharmInfo(Charm.charm_pit_saver, "Whetstone (Parry Axe)"),
            new CharmInfo(Charm.charm_healer, "Heart Ring (Parry HP)"),
            new CharmInfo(Charm.charm_curse, "Relic / Curse"),
        };

        private class WeaponInfo
        {
            public Weapon Value;
            public string Name;
            public WeaponInfo(Weapon v, string n) { Value = v; Name = n; }
        }

        private class SuperInfo
        {
            public Super Value;
            public string Name;
            public SuperInfo(Super v, string n) { Value = v; Name = n; }
        }

        private class CharmInfo
        {
            public Charm Value;
            public string Name;
            public CharmInfo(Charm v, string n) { Value = v; Name = n; }
        }

        private static int _primaryIndex = 0;
        private static int _secondaryIndex = 1;
        private static int _superIndex = 0;
        private static int _charmIndex = 1;

        // Active tab: 0 = Loadout & Health, 1 = Gameplay & Hardcore, 2 = Stats & Achievements
        private static int _activeTab = 0;
        private static int _selectedRow = 0;
        private static Vector2 _statsScrollPos = Vector2.zero;

        private static GUIStyle _windowStyle;
        private static GUIStyle _slotBoxStyle;
        private static GUIStyle _headerStyle;
        private static GUIStyle _sectionHeaderStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _selectedLabelStyle;
        private static GUIStyle _valueStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _actionButtonStyle;
        private static GUIStyle _selectedActionButtonStyle;
        private static GUIStyle _tabActiveStyle;
        private static GUIStyle _tabInactiveStyle;
        private static GUIStyle _footerStyle;
        private static GUIStyle _highlightStyle;
        private static GUIStyle _tableHeaderStyle;
        private static GUIStyle _tableRowStyle;
        private static GUIStyle _tableColStyle;
        private static GUIStyle _achTitleStyle;
        private static GUIStyle _achDescStyle;
        private static Texture2D _cursorTexture;
        private static int _lastFontMode = -1;
        private static float _lastFontAspect = -1f;

        private static void EnsureStyles()
        {
            int curMode = Plugin.UIFontMode != null ? Plugin.UIFontMode.Value : 0;
            float curAspect = Plugin.UIFontAspectScale != null ? Plugin.UIFontAspectScale.Value : 0.88f;

            if (_windowStyle != null && _headerStyle != null && _lastFontMode == curMode && Mathf.Approximately(_lastFontAspect, curAspect)) return;

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

            _highlightStyle = new GUIStyle(GUI.skin.box);
            _highlightStyle.normal.background = CupheadUITheme.CreateInsetSlotTexture(16, new Color(1f, 0.86f, 0.38f, 0.70f), new Color(0.96f, 0.76f, 0.22f, 0.28f));
            _highlightStyle.border = new RectOffset(3, 3, 3, 3);

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont,
                fontSize = 28,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _headerStyle.normal.textColor = CupheadUITheme.VintageGoldBright;

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

            _selectedLabelStyle = new GUIStyle(_labelStyle);
            _selectedLabelStyle.normal.textColor = CupheadUITheme.VintageGoldBright;

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 19,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _valueStyle.normal.textColor = new Color(1f, 0.88f, 0.38f, 1f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = titleFont,
                fontSize = 18,
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

            _actionButtonStyle = new GUIStyle(_buttonStyle);
            _actionButtonStyle.normal.background = CupheadUITheme.CreateButtonTexture(16, CupheadUITheme.InkBlack, CupheadUITheme.VintageGold, new Color(0.28f, 0.18f, 0.12f, 1f));
            _actionButtonStyle.normal.textColor = CupheadUITheme.ParchmentCream;

            _selectedActionButtonStyle = new GUIStyle(_actionButtonStyle);
            _selectedActionButtonStyle.normal.background = CupheadUITheme.CreateButtonTexture(16, CupheadUITheme.InkBlack, new Color(1f, 0.88f, 0.40f, 1f), CupheadUITheme.CupheadRed);
            _selectedActionButtonStyle.normal.textColor = Color.white;

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

            _footerStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _footerStyle.normal.textColor = CupheadUITheme.SepiaMuted;

            _tableHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont ?? bodyFont,
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _tableHeaderStyle.normal.textColor = CupheadUITheme.VintageGoldBright;

            _tableRowStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 17,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
            _tableRowStyle.normal.textColor = CupheadUITheme.ParchmentCream;

            _tableColStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 17,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            _tableColStyle.normal.textColor = new Color(1f, 0.90f, 0.48f, 1f);

            _achTitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = titleFont ?? bodyFont,
                fontSize = 17,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };

            _achDescStyle = new GUIStyle(GUI.skin.label)
            {
                font = bodyFont,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
        }

        private static Texture2D CreateCursorTexture()
        {
            return CupheadUITheme.CreateCartoonGloveCursor();
        }

        public static void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                IsOpen = !IsOpen;
                if (IsOpen)
                {
                    ReadFromSave();
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    ControllerManager.ShowToast("Menu ustawień Co-op OTWARTE [F5 lub Esc]");
                }
                else
                {
                    Cursor.visible = false;
                }
            }

            if (IsOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                HandleKeyboardNavigation();
            }
        }

        private static void HandleKeyboardNavigation()
        {
            int totalRows = (_activeTab == 0) ? 13 : ((_activeTab == 1) ? 13 : 4);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                IsOpen = false;
                Cursor.visible = false;
                return;
            }

            // Quick tab switch with Q / E or Tab or Gamepad LB / RB
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton4))
            {
                _activeTab = (_activeTab - 1 + 3) % 3;
                _selectedRow = 0;
                return;
            }
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.JoystickButton5))
            {
                _activeTab = (_activeTab + 1) % 3;
                _selectedRow = 0;
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                _selectedRow = (_selectedRow - 1 + totalRows) % totalRows;
                if (_activeTab == 2)
                {
                    _statsScrollPos.y = Mathf.Max(0f, _statsScrollPos.y - 30f);
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                _selectedRow = (_selectedRow + 1) % totalRows;
                if (_activeTab == 2)
                {
                    _statsScrollPos.y += 30f;
                }
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                if (_selectedRow == 0)
                {
                    _activeTab = (_activeTab - 1 + 3) % 3;
                }
                else
                {
                    StepRow(-1);
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                if (_selectedRow == 0)
                {
                    _activeTab = (_activeTab + 1) % 3;
                }
                else
                {
                    StepRow(1);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                ActivateRow();
            }
        }

        private static void StepRow(int delta)
        {
            if (_activeTab == 0)
            {
                switch (_selectedRow)
                {
                    case 1: // Shot-A
                        _primaryIndex = (_primaryIndex + delta + AllWeapons.Length) % AllWeapons.Length;
                        SaveToData();
                        break;
                    case 2: // Shot-B
                        _secondaryIndex = (_secondaryIndex + delta + AllWeapons.Length) % AllWeapons.Length;
                        SaveToData();
                        break;
                    case 3: // Super
                        _superIndex = (_superIndex + delta + AllSupers.Length) % AllSupers.Length;
                        SaveToData();
                        break;
                    case 4: // Charm
                        _charmIndex = (_charmIndex + delta + AllCharms.Length) % AllCharms.Length;
                        SaveToData();
                        break;
                    case 5: // Chalice Toggle
                        ToggleChalice();
                        break;
                    case 6: // Base Model
                        ToggleP3BaseCharacter();
                        break;
                    case 7: // P1 HP
                        AdjustHp(PlayerId.PlayerOne, delta);
                        break;
                    case 8: // P2 HP
                        AdjustHp(PlayerId.PlayerTwo, delta);
                        break;
                    case 9: // P3 HP
                        AdjustHp(PlayerId.PlayerThree, delta);
                        break;
                    case 10: // Quick HP
                        ApplyAllHp(delta > 0 ? 3 : 1);
                        break;
                }
            }
            else if (_activeTab == 1) // Tab 1: Gameplay & Hardcore
            {
                switch (_selectedRow)
                {
                    case 1: // Damage Preset
                        DamageScaling.CyclePreset(delta);
                        break;
                    case 2: // Parry Time Stop
                        ToggleParryFreeze();
                        break;
                    case 3: // Hardcore Mode
                        ToggleHardcoreMode();
                        break;
                    case 4: // Hardcore Scope
                        ToggleRestartOnlyOnBosses();
                        break;
                    case 5: // Progress Card
                        ToggleShowProgressOnGhostLost();
                        break;
                    case 6: // Progress Duration
                        AdjustGameOverProgressSeconds(delta * 0.5f);
                        break;
                    case 7: // Ghost Lost Grace
                        AdjustGhostLostGraceSeconds(delta * 0.5f);
                        break;
                    case 8: // Rescue Cooldown
                        AdjustRescueCooldown(delta * 0.5f);
                        break;
                    case 9: // Quick Restart Hold Duration
                        AdjustQuickRestartDuration(delta * 0.2f);
                        break;
                    case 10: // UI Font Mode
                        CycleFontMode();
                        break;
                    case 11: // UI Font Aspect
                        AdjustFontAspect(delta * 0.05f);
                        break;
                }
            }
            else // Tab 2: Stats & Achievements
            {
                _statsScrollPos.y = Mathf.Max(0f, _statsScrollPos.y + delta * 35f);
            }
        }

        private static void ActivateRow()
        {
            if (_activeTab == 0)
            {
                switch (_selectedRow)
                {
                    case 0:
                        _activeTab = (_activeTab + 1) % 3;
                        break;
                    case 5: // Chalice Toggle
                        ToggleChalice();
                        break;
                    case 6: // Base Model
                        ToggleP3BaseCharacter();
                        break;
                    case 10: // All 3 HP
                        ApplyAllHp(3);
                        break;
                    case 11: // Copy P1
                        CopyFromPlayerOne();
                        break;
                    case 12: // Close
                        IsOpen = false;
                        Cursor.visible = false;
                        break;
                }
            }
            else if (_activeTab == 1) // Tab 1: Gameplay & Hardcore
            {
                switch (_selectedRow)
                {
                    case 0:
                        _activeTab = (_activeTab + 1) % 3;
                        break;
                    case 1: // Damage Preset
                        DamageScaling.CyclePreset(1);
                        break;
                    case 2: // Parry Time Stop
                        ToggleParryFreeze();
                        break;
                    case 3: // Hardcore Mode
                        ToggleHardcoreMode();
                        break;
                    case 4: // Hardcore Scope
                        ToggleRestartOnlyOnBosses();
                        break;
                    case 5: // Progress Card
                        ToggleShowProgressOnGhostLost();
                        break;
                    case 6: // Progress Duration
                        AdjustGameOverProgressSeconds(0.5f);
                        break;
                    case 7: // Ghost Lost Grace
                        AdjustGhostLostGraceSeconds(0.5f);
                        break;
                    case 8: // Rescue Cooldown
                        AdjustRescueCooldown(0.5f);
                        break;
                    case 9: // Quick Restart Hold Duration
                        AdjustQuickRestartDuration(0.2f);
                        break;
                    case 10: // UI Font Mode
                        CycleFontMode();
                        break;
                    case 11: // UI Font Aspect
                        AdjustFontAspect(0.05f);
                        break;
                    case 12: // Close
                        IsOpen = false;
                        Cursor.visible = false;
                        break;
                }
            }
            else // Tab 2: Stats & Achievements
            {
                switch (_selectedRow)
                {
                    case 0:
                        _activeTab = (_activeTab + 1) % 3;
                        break;
                    case 1: // Last Boss Summary
                        BossVictorySummaryUI.Toggle();
                        break;
                    case 2: // Reset All Stats
                        CoopStatsManager.ResetAllStats();
                        break;
                    case 3: // Close
                        IsOpen = false;
                        Cursor.visible = false;
                        break;
                }
            }
        }

        private static void ToggleChalice()
        {
            bool isChaliceNow = AllCharms[_charmIndex].Value == Charm.charm_chalice;
            if (isChaliceNow)
            {
                _charmIndex = 0; // None
            }
            else
            {
                for (int i = 0; i < AllCharms.Length; i++)
                {
                    if (AllCharms[i].Value == Charm.charm_chalice) { _charmIndex = i; break; }
                }
            }
            SaveToData();
        }

        private static void ToggleParryFreeze()
        {
            if (Plugin.ParryTimeFreeze == null) return;
            Plugin.ParryTimeFreeze.Value = !Plugin.ParryTimeFreeze.Value;
            if (Plugin.ParryTimeFreeze.Value)
            {
                ControllerManager.ShowToast("Parry Time Stop: WŁĄCZONE (Zatrzymanie)");
            }
            else
            {
                ControllerManager.ShowToast("Parry Time Stop: WYŁĄCZONE (Płynna gra)");
            }
        }

        private static void ToggleHardcoreMode()
        {
            if (Plugin.RestartOnGhostLost == null) return;
            Plugin.RestartOnGhostLost.Value = !Plugin.RestartOnGhostLost.Value;
            if (Plugin.RestartOnGhostLost.Value)
            {
                ControllerManager.ShowToast("HARDCORE MODE: WŁĄCZONY (Restart po utracie duszka)");
            }
            else
            {
                ControllerManager.ShowToast("HARDCORE MODE: WYŁĄCZONY (Standardowa gra)");
            }
        }

        private static void ToggleRestartOnlyOnBosses()
        {
            if (Plugin.RestartOnlyOnBosses == null) return;
            Plugin.RestartOnlyOnBosses.Value = !Plugin.RestartOnlyOnBosses.Value;
            if (Plugin.RestartOnlyOnBosses.Value)
            {
                ControllerManager.ShowToast("Zakres Hardcore: TYLKO BOSSOWIE");
            }
            else
            {
                ControllerManager.ShowToast("Zakres Hardcore: WSZYSTKIE POZIOMY (R&G)");
            }
        }

        private static void ToggleShowProgressOnGhostLost()
        {
            if (Plugin.ShowProgressOnGhostLost == null) return;
            Plugin.ShowProgressOnGhostLost.Value = !Plugin.ShowProgressOnGhostLost.Value;
            if (Plugin.ShowProgressOnGhostLost.Value)
            {
                ControllerManager.ShowToast("Karta postępu bossa: WŁĄCZONA");
            }
            else
            {
                ControllerManager.ShowToast("Karta postępu bossa: WYŁĄCZONA (Natychmiastowy restart)");
            }
        }

        private static void AdjustGameOverProgressSeconds(float delta)
        {
            if (Plugin.GameOverProgressSeconds == null) return;
            float val = Mathf.Round((Plugin.GameOverProgressSeconds.Value + delta) * 10f) / 10f;
            val = Mathf.Clamp(val, 1.0f, 10.0f);
            Plugin.GameOverProgressSeconds.Value = val;
            ControllerManager.ShowToast(string.Format("Czas karty postępu: {0:0.0}s", val));
        }

        private static void AdjustGhostLostGraceSeconds(float delta)
        {
            if (Plugin.GhostLostGraceSeconds == null) return;
            float val = Mathf.Round((Plugin.GhostLostGraceSeconds.Value + delta) * 10f) / 10f;
            val = Mathf.Clamp(val, 0.2f, 5.0f);
            Plugin.GhostLostGraceSeconds.Value = val;
            ControllerManager.ShowToast(string.Format("Okno ratunku: {0:0.0}s", val));
        }

        private static void AdjustRescueCooldown(float delta)
        {
            if (Plugin.RescueCooldown == null) return;
            float val = Mathf.Round((Plugin.RescueCooldown.Value + delta) * 10f) / 10f;
            val = Mathf.Clamp(val, 0.5f, 5.0f);
            Plugin.RescueCooldown.Value = val;
            ControllerManager.ShowToast(string.Format("Odnowienie ratunku: {0:0.0}s", val));
        }

        private static void AdjustQuickRestartDuration(float delta)
        {
            if (Plugin.QuickRestartHoldDuration == null) return;
            float val = Mathf.Round((Plugin.QuickRestartHoldDuration.Value + delta) * 10f) / 10f;
            val = Mathf.Clamp(val, 0.4f, 3.0f);
            Plugin.QuickRestartHoldDuration.Value = val;
            ControllerManager.ShowToast(string.Format("Szybki restart (L3): {0:0.0}s", val));
        }

        private static void CycleFontMode()
        {
            if (Plugin.UIFontMode == null) return;
            Plugin.UIFontMode.Value = (Plugin.UIFontMode.Value == 0) ? 1 : 0;
            CupheadUITheme.ReloadFonts();
            _windowStyle = null;
            BossVictorySummaryUI.InvalidateStyles();
            string modeName = Plugin.UIFontMode.Value == 1 ? "Czysta (Arial / Sans)" : "Cuphead (Vogue & Memphis) [Oryginalna]";
            ControllerManager.ShowToast("Krój czcionki UI: " + modeName);
        }

        private static void AdjustFontAspect(float delta)
        {
            if (Plugin.UIFontAspectScale == null) return;
            float val = Mathf.Round((Plugin.UIFontAspectScale.Value + delta) * 20f) / 20f;
            val = Mathf.Clamp(val, 0.70f, 1.15f);
            Plugin.UIFontAspectScale.Value = val;
            _windowStyle = null;
            BossVictorySummaryUI.InvalidateStyles();
            int pct = Mathf.RoundToInt(val * 100f);
            string desc = pct <= 88 ? "Smukła / Wyższa" : "Szeroka";
            ControllerManager.ShowToast(string.Format("Proporcje czcionki UI: {0}% ({1})", pct, desc));
        }

        private static void ToggleP3BaseCharacter()
        {
            if (Plugin.P3BaseCharacter == null) return;
            int cur = Plugin.P3BaseCharacter.Value;
            int next = (cur == 1) ? 0 : 1;
            Plugin.P3BaseCharacter.Value = next;

            if (Level.Current != null)
            {
                var p3 = Extras.GetPlayer(PlayerId.PlayerThree);
                if (p3 != null)
                {
                    var stats = p3.stats;
                    if (stats == null || !stats.isChalice)
                    {
                        var levelP = p3 as LevelPlayerController;
                        if (levelP != null && levelP.animationController != null)
                        {
                            levelP.animationController.SetSprites(next == 0);
                            var col = Extras.ParseColor(Plugin.Player3Color.Value, new Color(0.06f, 1f, 0f));
                            levelP.animationController.SetColor(col);
                        }

                        var planeP = p3 as PlanePlayerController;
                        if (planeP != null && planeP.animationController != null)
                        {
                            var ac = planeP.animationController;
                            Extras.SetPlanePlayerSprites(ac, next == 1);
                            var col = Extras.ParseColor(Plugin.Player3Color.Value, new Color(0.06f, 1f, 0f));
                            ac.SetColor(col);
                        }
                    }
                }
            }

            string name = (next == 1) ? "Mugman (Okrągły nos)" : "Cuphead (Spiczasty nos)";
            ControllerManager.ShowToast("Model Bazowy Stempla: " + name);
        }

        private static void AdjustHp(PlayerId id, int delta)
        {
            int current = HealthConfig.GetBaseHealth(id);
            int updated = Mathf.Clamp(current + delta, 1, 3);
            HealthConfig.SetBaseHealth(id, updated);
        }

        private static void ApplyAllHp(int hp)
        {
            hp = Mathf.Clamp(hp, 1, 3);
            HealthConfig.SetBaseHealth(PlayerId.PlayerOne, hp);
            HealthConfig.SetBaseHealth(PlayerId.PlayerTwo, hp);
            HealthConfig.SetBaseHealth(PlayerId.PlayerThree, hp);
            ControllerManager.ShowToast(string.Format("Punkty życia graczy ustawione na {0} HP!", hp));
        }

        private static void ReadFromSave()
        {
            try
            {
                var data = PlayerData.Data;
                if (data == null || data.Loadouts == null) return;
                var p3 = data.Loadouts.playerThree;
                if (p3 == null) return;

                for (int i = 0; i < AllWeapons.Length; i++)
                {
                    if (AllWeapons[i].Value == p3.primaryWeapon) { _primaryIndex = i; break; }
                }

                for (int i = 0; i < AllWeapons.Length; i++)
                {
                    if (AllWeapons[i].Value == p3.secondaryWeapon) { _secondaryIndex = i; break; }
                }

                for (int i = 0; i < AllSupers.Length; i++)
                {
                    if (AllSupers[i].Value == p3.super) { _superIndex = i; break; }
                }

                for (int i = 0; i < AllCharms.Length; i++)
                {
                    if (AllCharms[i].Value == p3.charm) { _charmIndex = i; break; }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] ReadFromSave error: " + e.Message);
            }
        }

        private static void SaveToData()
        {
            try
            {
                var data = PlayerData.Data;
                if (data == null || data.Loadouts == null) return;

                var p3 = data.Loadouts.playerThree;
                if (p3 == null)
                {
                    p3 = new PlayerData.PlayerLoadouts.PlayerLoadout();
                    data.Loadouts.playerThree = p3;
                }

                p3.primaryWeapon = AllWeapons[_primaryIndex].Value;
                p3.secondaryWeapon = AllWeapons[_secondaryIndex].Value;
                p3.super = AllSupers[_superIndex].Value;
                p3.charm = AllCharms[_charmIndex].Value;

                PlayerData.SaveCurrentFile();

                string isChalice = (p3.charm == Charm.charm_chalice) ? " (Bajeczna Czarka)" : "";
                ControllerManager.ShowToast("Ekwipunek Stempla: " + AllWeapons[_primaryIndex].Name + " + " + AllWeapons[_secondaryIndex].Name + isChalice);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] SaveToData error: " + e.Message);
            }
        }

        public static void DrawGUI()
        {
            if (!IsOpen) return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EnsureStyles();

            float width = Mathf.Clamp(Screen.width * 0.72f, 960f, 1380f);
            if (width > Screen.width - 40f) width = Screen.width - 40f;

            float height = Mathf.Clamp(Screen.height * 0.82f, 760f, 960f);
            if (height > Screen.height - 40f) height = Screen.height - 40f;

            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none, _windowStyle);

            float cx = x + 28f;
            float cy = y + 18f;
            float cw = width - 56f;

            // Header
            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 32f), "CO-OP SETTINGS & STATS", _headerStyle);
            cy += 38f;

            // Row 0: Tab Bar
            DrawTabBar(cx, ref cy, cw);

            if (_activeTab == 0)
            {
                // Tab 0: P3 LOADOUT & HEALTH
                // Row 1: Primary Weapon
                DrawRow(1, "Shot-A (Główna):", AllWeapons[_primaryIndex].Name, cx, ref cy, cw, () =>
                {
                    _primaryIndex = (_primaryIndex - 1 + AllWeapons.Length) % AllWeapons.Length;
                    SaveToData();
                }, () =>
                {
                    _primaryIndex = (_primaryIndex + 1) % AllWeapons.Length;
                    SaveToData();
                });

                // Row 2: Secondary Weapon
                DrawRow(2, "Shot-B (Alternatywna):", AllWeapons[_secondaryIndex].Name, cx, ref cy, cw, () =>
                {
                    _secondaryIndex = (_secondaryIndex - 1 + AllWeapons.Length) % AllWeapons.Length;
                    SaveToData();
                }, () =>
                {
                    _secondaryIndex = (_secondaryIndex + 1) % AllWeapons.Length;
                    SaveToData();
                });

                // Row 3: Super Art
                DrawRow(3, "Supercios (Super):", AllSupers[_superIndex].Name, cx, ref cy, cw, () =>
                {
                    _superIndex = (_superIndex - 1 + AllSupers.Length) % AllSupers.Length;
                    SaveToData();
                }, () =>
                {
                    _superIndex = (_superIndex + 1) % AllSupers.Length;
                    SaveToData();
                });

                // Row 4: Charm / Perk
                DrawRow(4, "Amulet (Perk):", AllCharms[_charmIndex].Name, cx, ref cy, cw, () =>
                {
                    _charmIndex = (_charmIndex - 1 + AllCharms.Length) % AllCharms.Length;
                    SaveToData();
                }, () =>
                {
                    _charmIndex = (_charmIndex + 1) % AllCharms.Length;
                    SaveToData();
                });

                cy += 6f;

                // Row 5: Chalice Toggle Button
                bool isChaliceNow = AllCharms[_charmIndex].Value == Charm.charm_chalice;
                string chaliceBtnText = isChaliceNow ? "Postać: Ms. Chalice [Włączona]" : "Wybierz postać: Ms. Chalice (Astral Cookie)";
                GUIStyle chaliceStyle = (_selectedRow == 5) ? _selectedActionButtonStyle : _actionButtonStyle;

                if (_selectedRow == 5)
                {
                    GUI.Box(new Rect(cx - 3f, cy - 3f, cw + 6f, 44f), GUIContent.none, _highlightStyle);
                }

                if (GUI.Button(new Rect(cx, cy, cw, 38f), chaliceBtnText, chaliceStyle))
                {
                    _selectedRow = 5;
                    ToggleChalice();
                }
                cy += 46f;

                // Row 6: Base Character Model (when not Chalice)
                string bCharText = (Plugin.P3BaseCharacter != null && Plugin.P3BaseCharacter.Value == 1)
                    ? "MUGMAN (Okrągły nos / Słomka)"
                    : "CUPHEAD (Spiczasty nos / Słomka)";
                DrawRow(6, "Model Bazowy P3:", bCharText, cx, ref cy, cw,
                    () => ToggleP3BaseCharacter(), () => ToggleP3BaseCharacter());
                cy += 6f;

                // Health Rows (Rows 7, 8, 9)
                int p1Hp = HealthConfig.GetBaseHealth(PlayerId.PlayerOne);
                int p2Hp = HealthConfig.GetBaseHealth(PlayerId.PlayerTwo);
                int p3Hp = HealthConfig.GetBaseHealth(PlayerId.PlayerThree);

                DrawHealthRow(7, "Cuphead (Czerwony):", p1Hp, cx, ref cy, cw,
                    () => { HealthConfig.SetBaseHealth(PlayerId.PlayerOne, p1Hp - 1); },
                    () => { HealthConfig.SetBaseHealth(PlayerId.PlayerOne, p1Hp + 1); });

                DrawHealthRow(8, "Mugman (Niebieski):", p2Hp, cx, ref cy, cw,
                    () => { HealthConfig.SetBaseHealth(PlayerId.PlayerTwo, p2Hp - 1); },
                    () => { HealthConfig.SetBaseHealth(PlayerId.PlayerTwo, p2Hp + 1); });

                DrawHealthRow(9, "Gracz 3 (Zielony):", p3Hp, cx, ref cy, cw,
                    () => { HealthConfig.SetBaseHealth(PlayerId.PlayerThree, p3Hp - 1); },
                    () => { HealthConfig.SetBaseHealth(PlayerId.PlayerThree, p3Hp + 1); });

                cy += 6f;

                // Row 10: Quick preset buttons
                float thirdW = (cw - 16f) / 3f;
                float presetH = 38f;
                if (_selectedRow == 10)
                {
                    GUI.Box(new Rect(cx - 3f, cy - 3f, cw + 6f, presetH + 6f), GUIContent.none, _highlightStyle);
                }
                if (CupheadUITheme.DrawButton(new Rect(cx, cy, thirdW, presetH), "Wszyscy 1 HP", _buttonStyle))
                {
                    _selectedRow = 10;
                    ApplyAllHp(1);
                }
                if (CupheadUITheme.DrawButton(new Rect(cx + thirdW + 8f, cy, thirdW, presetH), "Wszyscy 2 HP", _buttonStyle))
                {
                    _selectedRow = 10;
                    ApplyAllHp(2);
                }
                if (CupheadUITheme.DrawButton(new Rect(cx + (thirdW + 8f) * 2f, cy, thirdW, presetH), "Wszyscy 3 HP", _buttonStyle))
                {
                    _selectedRow = 10;
                    ApplyAllHp(3);
                }
                cy += presetH + 14f;

                // Row 11: Copy Loadout & Row 12: Close
                float halfW = (cw - 16f) / 2f;
                float btmH = 40f;
                if (_selectedRow == 11)
                {
                    GUI.Box(new Rect(cx - 3f, cy - 3f, halfW + 6f, btmH + 6f), GUIContent.none, _highlightStyle);
                }
                if (CupheadUITheme.DrawButton(new Rect(cx, cy, halfW, btmH), "Kopiuj z P1 (Cuphead)", (_selectedRow == 11 ? _selectedActionButtonStyle : _actionButtonStyle)))
                {
                    _selectedRow = 11;
                    CopyFromPlayerOne();
                }

                if (_selectedRow == 12)
                {
                    GUI.Box(new Rect(cx + halfW + 16f - 3f, cy - 3f, halfW + 6f, btmH + 6f), GUIContent.none, _highlightStyle);
                }
                if (CupheadUITheme.DrawButton(new Rect(cx + halfW + 16f, cy, halfW, btmH), "Zamknij menu [F5]", (_selectedRow == 12 ? _selectedActionButtonStyle : _actionButtonStyle)))
                {
                    IsOpen = false;
                    Cursor.visible = false;
                }
                cy += btmH + 14f;
            }
            else if (_activeTab == 1)
            {
                // Tab 1: GAME RULES & HARDCORE
                // Row 1: Damage Preset
                DrawDamagePresetRow(1, cx, ref cy, cw);

                // Row 2: Parry Time Stop
                string parryVal = (Plugin.ParryTimeFreeze == null || Plugin.ParryTimeFreeze.Value) ? "WŁĄCZONE (Zatrzymanie)" : "WYŁĄCZONE (Płynna gra)";
                DrawToggleRow(2, "Parry Time Stop:", parryVal, cx, ref cy, cw, ToggleParryFreeze);

                // Row 3: Hardcore Mode
                bool hcOn = Plugin.RestartOnGhostLost != null && Plugin.RestartOnGhostLost.Value;
                DrawToggleRow(3, "Hardcore Mode:", hcOn ? "WŁĄCZONY (Auto-Restart)" : "WYŁĄCZONY (Normalna gra)", cx, ref cy, cw, ToggleHardcoreMode);

                // Row 4: Hardcore Scope
                bool bossOnly = Plugin.RestartOnlyOnBosses == null || Plugin.RestartOnlyOnBosses.Value;
                DrawToggleRow(4, "Zakres Hardcore:", bossOnly ? "Tylko walki z bossami" : "Wszystkie poziomy (w tym R&G)", cx, ref cy, cw, ToggleRestartOnlyOnBosses);

                // Row 5: Boss Progress Card
                bool showProg = Plugin.ShowProgressOnGhostLost == null || Plugin.ShowProgressOnGhostLost.Value;
                DrawToggleRow(5, "Karta postępu bossa:", showProg ? "WŁĄCZONA (Oś czasu)" : "WYŁĄCZONA (Błyskawiczny restart)", cx, ref cy, cw, ToggleShowProgressOnGhostLost);

                // Row 6: Progress Card Duration
                float progSec = Plugin.GameOverProgressSeconds != null ? Plugin.GameOverProgressSeconds.Value : 3.0f;
                DrawFloatRow(6, "Czas karty postępu:", progSec, "s (Oś czasu bossa)", cx, ref cy, cw, () => AdjustGameOverProgressSeconds(-0.5f), () => AdjustGameOverProgressSeconds(0.5f));

                // Row 7: Ghost Lost Grace Delay
                float graceSec = Plugin.GhostLostGraceSeconds != null ? Plugin.GhostLostGraceSeconds.Value : 1.0f;
                DrawFloatRow(7, "Okno ratunku duszka:", graceSec, "s (Czas przed restartem)", cx, ref cy, cw, () => AdjustGhostLostGraceSeconds(-0.5f), () => AdjustGhostLostGraceSeconds(0.5f));

                // Row 8: Rescue Cooldown
                float rescueSec = Plugin.RescueCooldown != null ? Plugin.RescueCooldown.Value : 1.0f;
                DrawFloatRow(8, "Odnowienie ratunku:", rescueSec, "s (Teleport kamery)", cx, ref cy, cw, () => AdjustRescueCooldown(-0.5f), () => AdjustRescueCooldown(0.5f));

                // Row 9: Quick Restart Hold Duration
                float qrSec = Plugin.QuickRestartHoldDuration != null ? Plugin.QuickRestartHoldDuration.Value : 1.0f;
                DrawFloatRow(9, "Szybki restart (L3):", qrSec, "s (Przytrzymaj LS / [R])", cx, ref cy, cw, () => AdjustQuickRestartDuration(-0.2f), () => AdjustQuickRestartDuration(0.2f));

                // Row 10: UI Font Style Mode
                int fontMode = Plugin.UIFontMode != null ? Plugin.UIFontMode.Value : 0;
                string fontModeName = fontMode == 1 ? "Czysta (Arial / Sans)" : "Cuphead (Vogue & Memphis) [Oryginalna]";
                DrawToggleRow(10, "Krój czcionki UI:", fontModeName, cx, ref cy, cw, CycleFontMode);

                // Row 11: UI Font Aspect Ratio Scale
                float curAspect = Plugin.UIFontAspectScale != null ? Plugin.UIFontAspectScale.Value : 0.88f;
                int aspectPct = Mathf.RoundToInt(curAspect * 100f);
                string aspectDesc = aspectPct <= 88 ? "% (Smukła / Wyższa)" : "% (Szeroka)";
                DrawFloatRow(11, "Proporcje czcionki (Aspect):", aspectPct, aspectDesc, cx, ref cy, cw,
                    () => AdjustFontAspect(-0.05f), () => AdjustFontAspect(0.05f));

                cy += 8f;

                // Row 12: Close Menu
                if (_selectedRow == 12)
                {
                    GUI.Box(new Rect(cx - 3f, cy - 3f, cw + 6f, 46f), GUIContent.none, _highlightStyle);
                }
                if (CupheadUITheme.DrawButton(new Rect(cx, cy, cw, 40f), "Zamknij menu [F5]", (_selectedRow == 12 ? _selectedActionButtonStyle : _actionButtonStyle)))
                {
                    IsOpen = false;
                    Cursor.visible = false;
                }
                cy += 48f;
            }
            else
            {
                // Tab 2: STATS & ACHIEVEMENTS
                DrawStatsTab(cx, ref cy, cw, height - 36f);
            }

            // Keyboard navigation hint at bottom
            CupheadUITheme.DrawLabel(new Rect(cx, y + height - 32f, cw, 22f), "[Q/E or Tab] Switch Tab   •   [W/S or ↑/↓] Select   •   [A/D or ←/→] Adjust/Scroll   •   [Enter] Action", _footerStyle);

            // Draw software cursor
            if (_cursorTexture == null) _cursorTexture = CreateCursorTexture();
            if (Event.current != null)
            {
                Vector2 mPos = Event.current.mousePosition;
                GUI.DrawTexture(new Rect(mPos.x, mPos.y, 22f, 22f), _cursorTexture);
            }
        }

        private static void DrawTabBar(float cx, ref float cy, float cw)
        {
            float tabH = 38f;
            float tabW = (cw - 12f) / 3f;
            bool isSelected = (_selectedRow == 0);

            if (isSelected)
            {
                GUI.Box(new Rect(cx - 3f, cy - 3f, cw + 6f, tabH + 6f), GUIContent.none, _highlightStyle);
            }

            GUIStyle tab0Style = (_activeTab == 0) ? _tabActiveStyle : _tabInactiveStyle;
            GUIStyle tab1Style = (_activeTab == 1) ? _tabActiveStyle : _tabInactiveStyle;
            GUIStyle tab2Style = (_activeTab == 2) ? _tabActiveStyle : _tabInactiveStyle;

            string t0Text = "1. EKWIPUNEK P3";
            string t1Text = "2. ROZGRYWKA & HC";
            string t2Text = "3. STATYSTYKI & OSIĄGNIĘCIA";

            if (CupheadUITheme.DrawButton(new Rect(cx, cy, tabW, tabH), t0Text, tab0Style))
            {
                _activeTab = 0;
                _selectedRow = 0;
            }

            if (CupheadUITheme.DrawButton(new Rect(cx + tabW + 6f, cy, tabW, tabH), t1Text, tab1Style))
            {
                _activeTab = 1;
                _selectedRow = 0;
            }

            if (CupheadUITheme.DrawButton(new Rect(cx + (tabW + 6f) * 2f, cy, tabW, tabH), t2Text, tab2Style))
            {
                _activeTab = 2;
                _selectedRow = 0;
            }

            cy += tabH + 12f;
        }

        private static void DrawStatsTab(float cx, ref float cy, float cw, float maxY)
        {
            var data = CoopStatsManager.Data;
            if (data == null) return;

            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 24f), "STATYSTYKI DRUŻYNY (TRIO)", _sectionHeaderStyle);
            cy += 28f;

            float colNameW = 280f;
            float colW = (cw - colNameW) / 4f;

            // Inset slot background for stats table
            float tableHeaderH = 26f;
            float tableRowH = 24f;
            float tableH = tableHeaderH + 7 * tableRowH + 10f;
            GUI.Box(new Rect(cx, cy, cw, tableH), GUIContent.none, _slotBoxStyle);

            // Table Header Row
            CupheadUITheme.DrawLabel(new Rect(cx + 6f, cy + 2f, colNameW - 6f, 22f), "Statystyka / Stat", _tableHeaderStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW, cy + 2f, colW, 22f), "<color=#FF6B66>Cuphead (P1)</color>", _tableHeaderStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW + colW, cy + 2f, colW, 22f), "<color=#5AA5FF>Mugman (P2)</color>", _tableHeaderStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW + colW * 2f, cy + 2f, colW, 22f), "<color=#68D960>Gracz 3 (P3)</color>", _tableHeaderStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW + colW * 3f, cy + 2f, colW, 22f), "<color=#FFDF40>Drużyna (Trio)</color>", _tableHeaderStyle);
            cy += tableHeaderH;

            DrawTableRow("Obrażenia (Damage):", string.Format("{0:N0}", data.p1.damageDealt), string.Format("{0:N0}", data.p2.damageDealt), string.Format("{0:N0}", data.p3.damageDealt), string.Format("{0:N0}", data.totalTeamDamage), cx, ref cy, colNameW, colW);
            DrawTableRow("Parowania (Parries):", data.p1.parries.ToString(), data.p2.parries.ToString(), data.p3.parries.ToString(), data.totalTeamParries.ToString(), cx, ref cy, colNameW, colW);
            DrawTableRow("Uratowania (Revives):", data.p1.revivesGiven.ToString(), data.p2.revivesGiven.ToString(), data.p3.revivesGiven.ToString(), data.totalTeamRevives.ToString(), cx, ref cy, colNameW, colW);
            DrawTableRow("Otrzymane (Revived):", data.p1.revivesReceived.ToString(), data.p2.revivesReceived.ToString(), data.p3.revivesReceived.ToString(), data.totalTeamRevives.ToString(), cx, ref cy, colNameW, colW);
            DrawTableRow("Śmierci (Deaths):", data.p1.deaths.ToString(), data.p2.deaths.ToString(), data.p3.deaths.ToString(), data.totalTeamDeaths.ToString(), cx, ref cy, colNameW, colW);
            DrawTableRow("Superciosy (Supers):", data.p1.supersUsed.ToString(), data.p2.supersUsed.ToString(), data.p3.supersUsed.ToString(), data.totalTeamSupers.ToString(), cx, ref cy, colNameW, colW);
            DrawTableRow("Bossowie (Boss Wins):", data.p1.bossVictories.ToString(), data.p2.bossVictories.ToString(), data.p3.bossVictories.ToString(), data.totalBossVictories.ToString(), cx, ref cy, colNameW, colW);

            cy += 10f;

            // Hardcore bar
            string hcBar = string.Format("<color=#FF7777>Zwycięstwa Hardcore: {0}</color>   |   <color=#FFAA66>Restarty Hardcore: {1}</color>", data.hardcoreBossVictories, data.hardcoreRestarts);
            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 22f), hcBar, _tableHeaderStyle);
            cy += 26f;

            // Achievements count
            int unlockedCount = 0;
            if (data.achievements != null)
            {
                for (int i = 0; i < data.achievements.Count; i++)
                {
                    if (data.achievements[i].unlocked) unlockedCount++;
                }
            }
            int totalAch = (data.achievements != null) ? data.achievements.Count : 15;

            CupheadUITheme.DrawLabel(new Rect(cx, cy, cw, 24f), string.Format("OSIĄGNIĘCIA 3-GRACZY ({0} / {1} ODBLOKOWANE)", unlockedCount, totalAch), _sectionHeaderStyle);
            cy += 26f;

            // Scrollable achievements list with recessed slot background
            float scrollH = Mathf.Max(200f, maxY - cy - 70f);
            float itemH = 48f;
            float contentH = (data.achievements != null ? data.achievements.Count : 0) * itemH + 8f;

            GUI.Box(new Rect(cx, cy, cw, scrollH), GUIContent.none, _slotBoxStyle);

            _statsScrollPos = GUI.BeginScrollView(new Rect(cx + 3f, cy + 3f, cw - 6f, scrollH - 6f), _statsScrollPos, new Rect(0f, 0f, cw - 26f, contentH));
            float ay = 3f;

            if (data.achievements != null)
            {
                for (int i = 0; i < data.achievements.Count; i++)
                {
                    var ach = data.achievements[i];
                    string titleText;
                    if (ach.unlocked)
                    {
                        titleText = string.Format("<color=#68D860>[ODBLOKOWANE]</color> <color=#FFDF48><b>{0}</b></color> <color=#A89882>({1})</color>", ach.title, ach.unlockDate);
                    }
                    else
                    {
                        titleText = string.Format("<color=#A89886>[ZABLOKOWANE]</color> <color=#B6A694><b>{0}</b></color>", ach.title);
                    }

                    CupheadUITheme.DrawLabel(new Rect(6f, ay, cw - 36f, 22f), titleText, _achTitleStyle);
                    CupheadUITheme.DrawLabel(new Rect(16f, ay + 20f, cw - 44f, 20f), (ach.unlocked ? "<color=#EADCC5>" : "<color=#B6A694>") + ach.description + "</color>", _achDescStyle);

                    ay += itemH;
                }
            }
            GUI.EndScrollView();
            cy += scrollH + 12f;

            // Buttons: Last Boss Summary [V], Reset Stats & Close
            float thirdW = (cw - 16f) / 3f;
            float btnH = 38f;
            if (_selectedRow == 1)
            {
                GUI.Box(new Rect(cx - 3f, cy - 3f, thirdW + 6f, btnH + 6f), GUIContent.none, _highlightStyle);
            }
            if (CupheadUITheme.DrawButton(new Rect(cx, cy, thirdW, btnH), "Karta walki bossa [V]", _buttonStyle))
            {
                _selectedRow = 1;
                BossVictorySummaryUI.Toggle();
            }

            if (_selectedRow == 2)
            {
                GUI.Box(new Rect(cx + thirdW + 8f - 3f, cy - 3f, thirdW + 6f, btnH + 6f), GUIContent.none, _highlightStyle);
            }
            if (CupheadUITheme.DrawButton(new Rect(cx + thirdW + 8f, cy, thirdW, btnH), "Zresetuj statystyki", _buttonStyle))
            {
                _selectedRow = 2;
                CoopStatsManager.ResetAllStats();
            }

            if (_selectedRow == 3)
            {
                GUI.Box(new Rect(cx + (thirdW + 8f) * 2f - 3f, cy - 3f, thirdW + 6f, btnH + 6f), GUIContent.none, _highlightStyle);
            }
            if (CupheadUITheme.DrawButton(new Rect(cx + (thirdW + 8f) * 2f, cy, thirdW, btnH), "Zamknij [F5]", _buttonStyle))
            {
                IsOpen = false;
                Cursor.visible = false;
            }
            cy += btnH + 12f;
        }

        private static void DrawTableRow(string name, string p1, string p2, string p3, string total, float cx, ref float cy, float colNameW, float colW)
        {
            CupheadUITheme.DrawLabel(new Rect(cx + 6f, cy, colNameW - 6f, 22f), name, _tableRowStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW, cy, colW, 22f), p1, _tableColStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW + colW, cy, colW, 22f), p2, _tableColStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW + colW * 2f, cy, colW, 22f), p3, _tableColStyle);
            CupheadUITheme.DrawLabel(new Rect(cx + colNameW + colW * 3f, cy, colW, 22f), total, _tableColStyle);
            cy += 24f;
        }

        private static void DrawToggleRow(int rowIndex, string title, string value, float cx, ref float cy, float cw, Action onToggle)
        {
            bool isSelected = (_selectedRow == rowIndex);
            float controlW = 420f;
            float labelW = cw - controlW - 16f;
            float btnW = 38f;
            float btnH = 34f;
            float valW = controlW - (btnW * 2f + 8f);

            if (isSelected)
            {
                GUI.Box(new Rect(cx - 4f, cy - 2f, cw + 8f, btnH + 4f), GUIContent.none, _highlightStyle);
            }

            CupheadUITheme.DrawLabel(new Rect(cx + 8f, cy, labelW, btnH), title, isSelected ? _selectedLabelStyle : _labelStyle);

            float controlX = cx + cw - controlW;

            if (CupheadUITheme.DrawButton(new Rect(controlX, cy, btnW, btnH), "<", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onToggle();
            }

            float valX = controlX + btnW + 4f;
            GUI.Box(new Rect(valX, cy, valW, btnH), GUIContent.none, _slotBoxStyle);
            CupheadUITheme.DrawLabel(new Rect(valX + 4f, cy, valW - 8f, btnH), value, _valueStyle);

            if (GUI.Button(new Rect(valX, cy, valW, btnH), GUIContent.none, GUIStyle.none))
            {
                _selectedRow = rowIndex;
                onToggle();
            }

            if (CupheadUITheme.DrawButton(new Rect(controlX + controlW - btnW, cy, btnW, btnH), ">", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onToggle();
            }

            cy += 40f;
        }

        private static void DrawFloatRow(int rowIndex, string title, float value, string unit, float cx, ref float cy, float cw, Action onDec, Action onInc)
        {
            bool isSelected = (_selectedRow == rowIndex);
            float controlW = 420f;
            float labelW = cw - controlW - 16f;
            float btnW = 38f;
            float btnH = 34f;
            float valW = controlW - (btnW * 2f + 8f);

            if (isSelected)
            {
                GUI.Box(new Rect(cx - 4f, cy - 2f, cw + 8f, btnH + 4f), GUIContent.none, _highlightStyle);
            }

            CupheadUITheme.DrawLabel(new Rect(cx + 8f, cy, labelW, btnH), title, isSelected ? _selectedLabelStyle : _labelStyle);

            float controlX = cx + cw - controlW;

            if (CupheadUITheme.DrawButton(new Rect(controlX, cy, btnW, btnH), "<", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onDec();
            }

            float valX = controlX + btnW + 4f;
            GUI.Box(new Rect(valX, cy, valW, btnH), GUIContent.none, _slotBoxStyle);

            string valText = string.Format("{0:0.0} {1}", value, unit).TrimEnd();
            CupheadUITheme.DrawLabel(new Rect(valX + 4f, cy, valW - 8f, btnH), valText, _valueStyle);

            if (GUI.Button(new Rect(valX, cy, valW, btnH), GUIContent.none, GUIStyle.none))
            {
                _selectedRow = rowIndex;
                onInc();
            }

            if (CupheadUITheme.DrawButton(new Rect(controlX + controlW - btnW, cy, btnW, btnH), ">", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onInc();
            }

            cy += 40f;
        }

        private static void DrawRow(int rowIndex, string title, string value, float cx, ref float cy, float cw, Action onPrev, Action onNext)
        {
            bool isSelected = (_selectedRow == rowIndex);
            float controlW = 420f;
            float labelW = cw - controlW - 16f;
            float btnW = 38f;
            float btnH = 34f;
            float valW = controlW - (btnW * 2f + 8f);

            if (isSelected)
            {
                GUI.Box(new Rect(cx - 4f, cy - 2f, cw + 8f, btnH + 4f), GUIContent.none, _highlightStyle);
            }

            CupheadUITheme.DrawLabel(new Rect(cx + 8f, cy, labelW, btnH), title, isSelected ? _selectedLabelStyle : _labelStyle);

            float controlX = cx + cw - controlW;

            if (CupheadUITheme.DrawButton(new Rect(controlX, cy, btnW, btnH), "<", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onPrev();
            }

            float valX = controlX + btnW + 4f;
            GUI.Box(new Rect(valX, cy, valW, btnH), GUIContent.none, _slotBoxStyle);
            CupheadUITheme.DrawLabel(new Rect(valX + 4f, cy, valW - 8f, btnH), value, _valueStyle);

            if (GUI.Button(new Rect(valX, cy, valW, btnH), GUIContent.none, GUIStyle.none))
            {
                _selectedRow = rowIndex;
                onNext();
            }

            if (CupheadUITheme.DrawButton(new Rect(controlX + controlW - btnW, cy, btnW, btnH), ">", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onNext();
            }

            cy += 40f;
        }

        private static void DrawDamagePresetRow(int rowIndex, float cx, ref float cy, float cw)
        {
            bool isSelected = (_selectedRow == rowIndex);
            float controlW = 420f;
            float labelW = cw - controlW - 16f;
            float btnW = 38f;
            float btnH = 34f;
            float valW = controlW - (btnW * 2f + 8f);

            if (isSelected)
            {
                GUI.Box(new Rect(cx - 4f, cy - 2f, cw + 8f, btnH + 4f), GUIContent.none, _highlightStyle);
            }

            CupheadUITheme.DrawLabel(new Rect(cx + 8f, cy, labelW, btnH), "Tryb skalowania obrażeń:", isSelected ? _selectedLabelStyle : _labelStyle);

            float controlX = cx + cw - controlW;

            if (CupheadUITheme.DrawButton(new Rect(controlX, cy, btnW, btnH), "<", _buttonStyle))
            {
                _selectedRow = rowIndex;
                DamageScaling.CyclePreset(-1);
            }

            float valX = controlX + btnW + 4f;
            GUI.Box(new Rect(valX, cy, valW, btnH), GUIContent.none, _slotBoxStyle);
            string valText = DamageScaling.CurrentPresetDescription;
            CupheadUITheme.DrawLabel(new Rect(valX + 4f, cy, valW - 8f, btnH), valText, _valueStyle);

            if (GUI.Button(new Rect(valX, cy, valW, btnH), GUIContent.none, GUIStyle.none))
            {
                _selectedRow = rowIndex;
                DamageScaling.CyclePreset(1);
            }

            if (CupheadUITheme.DrawButton(new Rect(controlX + controlW - btnW, cy, btnW, btnH), ">", _buttonStyle))
            {
                _selectedRow = rowIndex;
                DamageScaling.CyclePreset(1);
            }

            cy += 40f;
        }

        private static void DrawHealthRow(int rowIndex, string title, int value, float cx, ref float cy, float cw, Action onDec, Action onInc)
        {
            bool isSelected = (_selectedRow == rowIndex);
            float controlW = 420f;
            float labelW = cw - controlW - 16f;
            float btnW = 38f;
            float btnH = 34f;
            float valW = controlW - (btnW * 2f + 8f);

            if (isSelected)
            {
                GUI.Box(new Rect(cx - 4f, cy - 2f, cw + 8f, btnH + 4f), GUIContent.none, _highlightStyle);
            }

            CupheadUITheme.DrawLabel(new Rect(cx + 8f, cy, labelW, btnH), title, isSelected ? _selectedLabelStyle : _labelStyle);

            float controlX = cx + cw - controlW;

            if (CupheadUITheme.DrawButton(new Rect(controlX, cy, btnW, btnH), "<", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onDec();
            }

            float valX = controlX + btnW + 4f;
            GUI.Box(new Rect(valX, cy, valW, btnH), GUIContent.none, _slotBoxStyle);
            string hpText = string.Format("{0} HP", value);
            CupheadUITheme.DrawLabel(new Rect(valX + 4f, cy, valW - 8f, btnH), hpText, _valueStyle);

            if (GUI.Button(new Rect(valX, cy, valW, btnH), GUIContent.none, GUIStyle.none))
            {
                _selectedRow = rowIndex;
                onInc();
            }

            if (CupheadUITheme.DrawButton(new Rect(controlX + controlW - btnW, cy, btnW, btnH), ">", _buttonStyle))
            {
                _selectedRow = rowIndex;
                onInc();
            }

            cy += 40f;
        }

        private static void CopyFromPlayerOne()
        {
            try
            {
                var data = PlayerData.Data;
                if (data == null || data.Loadouts == null) return;
                var p1 = data.Loadouts.playerOne;
                if (p1 == null) return;

                for (int i = 0; i < AllWeapons.Length; i++)
                {
                    if (AllWeapons[i].Value == p1.primaryWeapon) _primaryIndex = i;
                    if (AllWeapons[i].Value == p1.secondaryWeapon) _secondaryIndex = i;
                }

                for (int i = 0; i < AllSupers.Length; i++)
                {
                    if (AllSupers[i].Value == p1.super) _superIndex = i;
                }

                for (int i = 0; i < AllCharms.Length; i++)
                {
                    if (AllCharms[i].Value == p1.charm) _charmIndex = i;
                }

                SaveToData();
                ControllerManager.ShowToast("Skopiowano ekwipunek P1 (Cuphead) do Gracza 3!");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[3P-Coop] CopyFromPlayerOne error: " + e.Message);
            }
        }
    }
}
