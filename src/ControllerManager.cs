using System;
using System.Reflection;
using HarmonyLib;
using Rewired;
using UnityEngine;

namespace Cuphead3PCoop
{
    /// <summary>
    /// Manages controller routing and auto-assignment for 3-player co-op.
    /// Specifically solves the common PC co-op configuration:
    /// Player 1 on Keyboard, Player 2 on Gamepad 1, Player 3 on Gamepad 2.
    /// 
    /// In vanilla Cuphead / Rewired, Gamepad 1 is auto-captured by Player 1,
    /// leaving only Gamepad 2 for Player 2, and preventing Player 3 from joining.
    /// This manager detaches Gamepad 1 from Player 1 so both gamepads can join,
    /// provides smart join interception, and exposes hotkeys (F1, F2, F3, F4).
    /// </summary>
    internal static class ControllerManager
    {
        internal static bool P1KeyboardOnlyMode = false;
        private static float _lastAutoCheckTime = 0f;

        // On-screen toast notification
        private static string _toastMessage = "";
        private static float _toastExpires = 0f;

        internal static string CurrentToast
        {
            get
            {
                if (Time.unscaledTime < _toastExpires)
                    return _toastMessage;
                return null;
            }
        }

        internal static void ShowToast(string message, float duration = 4.0f)
        {
            _toastMessage = message;
            _toastExpires = Time.unscaledTime + duration;
            Plugin.Log.LogInfo("[3P-Coop] " + message);
        }

        /// <summary>
        /// Called every frame from FixRunner.Update().
        /// </summary>
        internal static void Update()
        {
            try
            {
                EnsureEventsHooked();
                HandleHotkeys();

                // Periodic check for controller auto-assignment detachment (every 0.5s)
                if (Time.unscaledTime - _lastAutoCheckTime > 0.5f)
                {
                    _lastAutoCheckTime = Time.unscaledTime;
                    CheckAutoDetach();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("ControllerManager.Update: " + e.Message);
            }
        }

        private static bool _eventsHooked = false;

        private static void EnsureEventsHooked()
        {
            if (_eventsHooked || !ReInput.isReady) return;
            try
            {
                ReInput.ControllerConnectedEvent += args =>
                {
                    Plugin.Log.LogInfo(string.Format("[3P-Coop] Gamepad CONNECTED: id={0}, name='{1}', type={2}", args.controllerId, args.name, args.controllerType));
                    ShowToast("Gamepad podłączony: " + args.name);
                };
                ReInput.ControllerDisconnectedEvent += args =>
                {
                    Plugin.Log.LogInfo(string.Format("[3P-Coop] Gamepad DISCONNECTED: id={0}, name='{1}', type={2}", args.controllerId, args.name, args.controllerType));
                    ShowToast("Gamepad disconnected: " + args.name);
                };
                _eventsHooked = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Failed to hook Rewired controller events: " + e.Message);
            }
        }

        private static void HandleHotkeys()
        {
            // F1: Toggle Player 1 Keyboard-Only mode
            if (Input.GetKeyDown(KeyCode.F1))
            {
                P1KeyboardOnlyMode = !P1KeyboardOnlyMode;
                if (P1KeyboardOnlyMode)
                {
                    DetachGamepadsFromP1();
                    ShowToast("Cuphead (P1) na KLAWIATURZE. Pady zwolnione dla P2 i P3!");
                }
                else
                {
                    var p1 = PlayerManager.GetPlayerInput(PlayerId.PlayerOne);
                    if (p1 != null)
                    {
                        p1.controllers.excludeFromControllerAutoAssignment = false;
                    }
                    ShowToast("Cuphead na klawiaturze WYŁĄCZONY. Pady mogą łączyć z P1.");
                }
            }

            // F2: Quick-Join Player 2
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (PlayerManager.DoesPlayerExist(PlayerId.PlayerTwo))
                {
                    ShowToast("Mugman (P2) jest już w grze!");
                }
                else
                {
                    TryForceJoin(PlayerId.PlayerTwo);
                }
            }

            // F3: Quick-Join Player 3
            if (Input.GetKeyDown(KeyCode.F3))
            {
                if (PlayerManager.DoesPlayerExist(PlayerId.PlayerThree))
                {
                    ShowToast("Gracz 3 (P3) jest już w grze!");
                }
                else
                {
                    TryForceJoin(PlayerId.PlayerThree);
                }
            }

            // F4: Cleanly re-pair all controllers: P1=KB, P2=Pad1, P3=Pad2
            if (Input.GetKeyDown(KeyCode.F4))
            {
                RepairControllers();
            }

            // F6: Toggle Hardcore Ghost-Lost Restart mode
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Plugin.RestartOnGhostLost.Value = !Plugin.RestartOnGhostLost.Value;
                if (Plugin.RestartOnGhostLost.Value)
                {
                    ShowToast("HARDCORE MODE: WŁĄCZONY (Auto-restart)");
                }
                else
                {
                    ShowToast("HARDCORE MODE: WYŁĄCZONY (Standardowa gra)");
                }
            }

            // F7: Toggle Parry Time Stop (Hitstop)
            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (Plugin.ParryTimeFreeze != null)
                {
                    Plugin.ParryTimeFreeze.Value = !Plugin.ParryTimeFreeze.Value;
                    if (Plugin.ParryTimeFreeze.Value)
                    {
                        ShowToast("PARRY TIME STOP: WŁĄCZONE (Zatrzymanie)");
                    }
                    else
                    {
                        ShowToast("PARRY TIME STOP: WYŁĄCZONE (Płynna gra)");
                    }
                }
            }

            // F8: Cycle Damage Scaling Preset (Easy -> Normal -> Hard)
            if (Input.GetKeyDown(KeyCode.F8))
            {
                DamageScaling.CyclePreset(1);
            }
        }

        /// <summary>
        /// Detaches any joysticks/gamepads from Player 1 and prevents Rewired from auto-stealing them.
        /// </summary>
        internal static void DetachGamepadsFromP1()
        {
            try
            {
                var p1 = PlayerManager.GetPlayerInput(PlayerId.PlayerOne);
                if (p1 != null)
                {
                    p1.controllers.excludeFromControllerAutoAssignment = true;
                    if (p1.controllers.joystickCount > 0)
                    {
                        p1.controllers.ClearControllersOfType<Joystick>();
                        Plugin.Log.LogInfo("[3P-Coop] Detached all Joysticks from Player 1.");
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("DetachGamepadsFromP1: " + e.Message);
            }
        }

        /// <summary>
        /// Evaluates whether gamepads should be detached from Player 1 based on configuration
        /// and connected hardware.
        /// </summary>
        private static void CheckAutoDetach()
        {
            if (!Plugin.AssignGamepadsToExtras.Value && !P1KeyboardOnlyMode) return;

            try
            {
                if (!ReInput.isReady) return;

                int joystickCount = ReInput.controllers.joystickCount;
                var p1 = PlayerManager.GetPlayerInput(PlayerId.PlayerOne);
                if (p1 == null) return;

                // If P1 has gamepads assigned:
                if (p1.controllers.joystickCount > 0)
                {
                    // Only auto-detach if explicitly set to Keyboard mode, OR if there are exactly 2 gamepads
                    // (which requires P1 on Keyboard so P2 & P3 can each have a gamepad).
                    // If there are 3 or more gamepads, P1 can freely play on Gamepad 0!
                    if (P1KeyboardOnlyMode || (Plugin.AssignGamepadsToExtras.Value && joystickCount == 2))
                    {
                        p1.controllers.excludeFromControllerAutoAssignment = true;
                        p1.controllers.ClearControllersOfType<Joystick>();
                        Plugin.Log.LogInfo("[3P-Coop] Auto-detached gamepads from Player 1 for 2-gamepad co-op.");
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Programmatically forces a player slot (P2 or P3) to join with an available gamepad.
        /// </summary>
        internal static bool TryForceJoin(PlayerId playerId)
        {
            if (Level.Current == null && Map.Current == null)
            {
                ShowToast("Cannot join yet: Please enter a Map or Level first!");
                return false;
            }

            int slotIndex = (int)playerId;
            if (slotIndex != 1 && slotIndex != 2)
            {
                ShowToast("Invalid player slot for join: " + playerId);
                return false;
            }

            if (!ReInput.isReady) return false;

            // 1. Look for an unassigned joystick
            Joystick targetJoystick = null;
            foreach (var j in ReInput.controllers.Joysticks)
            {
                if (!ReInput.controllers.IsJoystickAssigned(j))
                {
                    targetJoystick = j;
                    break;
                }
            }

            // 2. If no unassigned joystick is found, check if Player 1 is holding one
            if (targetJoystick == null)
            {
                var p1Input = PlayerManager.GetPlayerInput(PlayerId.PlayerOne);
                if (p1Input != null && p1Input.controllers.joystickCount > 0)
                {
                    targetJoystick = p1Input.controllers.Joysticks[0];
                    p1Input.controllers.RemoveController(targetJoystick);
                    p1Input.controllers.excludeFromControllerAutoAssignment = true;
                    Plugin.Log.LogInfo("[3P-Coop] ForceJoin grabbed joystick from Player 1: " + targetJoystick.name);
                }
            }

            if (targetJoystick == null)
            {
                ShowToast("No available Gamepad found! Plug in a controller or press F1.");
                return false;
            }

            try
            {
                var targetPlayerInput = PlayerManager.GetPlayerInput(playerId);
                if (targetPlayerInput == null) return false;

                // Assign controller to target player
                targetPlayerInput.controllers.AddController(targetJoystick, true);

                // Set PlayerSlot internals
                var slotsField = AccessTools.Field(typeof(PlayerManager), "playerSlots");
                var slots = slotsField?.GetValue(null) as Array;
                if (slots != null && slotIndex < slots.Length)
                {
                    var slot = slots.GetValue(slotIndex);
                    if (slot != null)
                    {
                        var controllerStateType = AccessTools.TypeByName("PlayerManager+PlayerSlot+ControllerState");
                        var joinStateType = AccessTools.TypeByName("PlayerManager+PlayerSlot+JoinState");

                        AccessTools.Field(slot.GetType(), "controllerState")?.SetValue(slot, Enum.ToObject(controllerStateType, 1 /* UsingController */));
                        AccessTools.Field(slot.GetType(), "controllerId")?.SetValue(slot, targetJoystick.id);
                        AccessTools.Field(slot.GetType(), "joinState")?.SetValue(slot, Enum.ToObject(joinStateType, 3 /* Joined */));
                    }
                }

                // Update PlayerManager multiplayer flags
                if (playerId == PlayerId.PlayerTwo)
                {
                    PlayerManager.Multiplayer = true;
                }
                else if (playerId == PlayerId.PlayerThree)
                {
                    PlayerManager.Multiplayer2 = true;
                }

                // Fire OnPlayerJoinedEvent
                var joinEvent = AccessTools.Field(typeof(PlayerManager), "OnPlayerJoinedEvent")?.GetValue(null) as MulticastDelegate;
                if (joinEvent != null)
                {
                    joinEvent.DynamicInvoke(playerId);
                }

                try
                {
                    AudioManager.Play("player_spawn");
                }
                catch (Exception) { }

                ShowToast(CupheadUITheme.GetPlayerFullName(playerId) + " DOŁĄCZYŁ na Padzie (" + targetJoystick.name + ")!");
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[3P-Coop] TryForceJoin(" + playerId + ") failed: " + e);
                ShowToast("Błąd dołączenia " + CupheadUITheme.GetPlayerName(playerId) + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Automatically re-assigns controllers cleanly:
        /// If 3+ gamepads: P1=Pad 1, P2=Pad 2, P3=Pad 3.
        /// If 2 gamepads: P1=Keyboard, P2=Pad 1, P3=Pad 2.
        /// </summary>
        internal static void RepairControllers()
        {
            if (!ReInput.isReady) return;

            try
            {
                try
                {
                    ReInput.controllers.AutoAssignJoysticks();
                }
                catch (Exception) { }

                var joysticks = ReInput.controllers.Joysticks;
                int totalPads = joysticks.Count;

                Plugin.Log.LogInfo(string.Format("[3P-Coop] RepairControllers: found {0} joystick(s).", totalPads));
                for (int i = 0; i < totalPads; i++)
                {
                    Plugin.Log.LogInfo(string.Format("[3P-Coop]   Pad {0}: id={1}, name='{2}'", i, joysticks[i].id, joysticks[i].name));
                }

                if (totalPads == 0)
                {
                    ShowToast("No Gamepads found by Cuphead! Check Steam Input / Parsec.", 5f);
                    return;
                }

                if (totalPads >= 3 && !P1KeyboardOnlyMode)
                {
                    // All 3 players on separate gamepads!
                    var p1 = PlayerManager.GetPlayerInput(PlayerId.PlayerOne);
                    var p2 = PlayerManager.GetPlayerInput(PlayerId.PlayerTwo);
                    var p3 = PlayerManager.GetPlayerInput(PlayerId.PlayerThree);

                    if (p1 != null)
                    {
                        p1.controllers.ClearControllersOfType<Joystick>();
                        p1.controllers.AddController(joysticks[0], true);
                    }
                    if (p2 != null)
                    {
                        p2.controllers.ClearControllersOfType<Joystick>();
                        p2.controllers.AddController(joysticks[1], true);
                    }
                    if (p3 != null)
                    {
                        p3.controllers.ClearControllersOfType<Joystick>();
                        p3.controllers.AddController(joysticks[2], true);
                    }

                    ShowToast("Skonfigurowano: P1=Pad 1, P2=Pad 2, P3=Pad 3 (3 Pady)");
                }
                else
                {
                    // Player 1 on Keyboard, Player 2 on Pad 1, Player 3 on Pad 2
                    P1KeyboardOnlyMode = true;
                    DetachGamepadsFromP1();

                    var p2 = PlayerManager.GetPlayerInput(PlayerId.PlayerTwo);
                    var p3 = PlayerManager.GetPlayerInput(PlayerId.PlayerThree);

                    if (totalPads >= 1 && p2 != null)
                    {
                        p2.controllers.ClearControllersOfType<Joystick>();
                        p2.controllers.AddController(joysticks[0], true);
                    }

                    if (totalPads >= 2 && p3 != null)
                    {
                        p3.controllers.ClearControllersOfType<Joystick>();
                        p3.controllers.AddController(joysticks[1], true);
                    }

                    ShowToast(string.Format("Skonfigurowano: P1=Klawiatura, P2=Pad 1, P3=Pad 2 ({0} Pad(y))", totalPads));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[3P-Coop] RepairControllers failed: " + e);
                ShowToast("Re-pair failed: " + e.Message);
            }
        }
    }

    /// <summary>
    /// Harmony patch for CupheadInput.CheckForUnconnectedControllerPress.
    /// If no unassigned joystick is found, intercepts button presses on joysticks that were
    /// stuck to Player 1, detaches them from Player 1, and lets them join!
    /// </summary>
    [HarmonyPatch(typeof(CupheadInput), nameof(CupheadInput.CheckForUnconnectedControllerPress))]
    internal static class CheckForUnconnectedControllerPressPatch
    {
        private static void Postfix(ref Joystick __result)
        {
            if (__result != null) return;
            if (!ReInput.isReady) return;

            try
            {
                // Only intercept button presses from P1's gamepad if P1 is supposed to be Keyboard-only,
                // or if there are exactly 2 gamepads (which requires P1 on Keyboard so P2/P3 can join).
                bool shouldIntercept = ControllerManager.P1KeyboardOnlyMode || (Plugin.AssignGamepadsToExtras.Value && ReInput.controllers.joystickCount == 2);
                if (!shouldIntercept) return;

                var p1 = PlayerManager.GetPlayerInput(PlayerId.PlayerOne);
                if (p1 != null && p1.controllers.joystickCount > 0)
                {
                    foreach (var j in p1.controllers.Joysticks)
                    {
                        if (j != null && j.GetAnyButtonDown())
                        {
                            // User pressed a button on a gamepad that was bound to P1!
                            // Detach it so it becomes the joining joystick!
                            p1.controllers.RemoveController(j);
                            p1.controllers.excludeFromControllerAutoAssignment = true;
                            Plugin.Log.LogInfo("[3P-Coop] Intercepted press on Gamepad (" + j.name + ") -> Detached from P1 for join!");
                            __result = j;
                            return;
                        }
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
