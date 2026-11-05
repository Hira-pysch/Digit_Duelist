using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DigitDuelist
{
    /// <summary>
    /// Unified input wrapper that cleanly handles Unity's New Input System (UnityEngine.InputSystem)
    /// without throwing InvalidOperationException when legacy UnityEngine.Input is disabled in Player Settings.
    /// </summary>
    public static class GameInput
    {
        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                switch (key)
                {
                    case KeyCode.Escape:
                        return Keyboard.current.escapeKey.wasPressedThisFrame;

                    case KeyCode.Alpha1:
                        return Keyboard.current[Key.Digit1].wasPressedThisFrame;
                    case KeyCode.Keypad1:
                        return Keyboard.current[Key.Numpad1].wasPressedThisFrame;

                    case KeyCode.Alpha2:
                        return Keyboard.current[Key.Digit2].wasPressedThisFrame;
                    case KeyCode.Keypad2:
                        return Keyboard.current[Key.Numpad2].wasPressedThisFrame;

                    case KeyCode.Alpha3:
                        return Keyboard.current[Key.Digit3].wasPressedThisFrame;
                    case KeyCode.Keypad3:
                        return Keyboard.current[Key.Numpad3].wasPressedThisFrame;

                    case KeyCode.Alpha4:
                        return Keyboard.current[Key.Digit4].wasPressedThisFrame;
                    case KeyCode.Keypad4:
                        return Keyboard.current[Key.Numpad4].wasPressedThisFrame;

                    case KeyCode.Q:
                        return Keyboard.current.qKey.wasPressedThisFrame;

                    case KeyCode.W:
                        return Keyboard.current.wKey.wasPressedThisFrame;

                    case KeyCode.E:
                        return Keyboard.current.eKey.wasPressedThisFrame;

                    case KeyCode.B:
                        return Keyboard.current.bKey.wasPressedThisFrame;

                    case KeyCode.L:
                        return Keyboard.current.lKey.wasPressedThisFrame;

                    case KeyCode.U:
                        return Keyboard.current.uKey.wasPressedThisFrame;
                }
            }
#endif
            try
            {
                return Input.GetKeyDown(key);
            }
            catch
            {
                return false;
            }
        }
    }
}
