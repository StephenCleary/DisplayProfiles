using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DisplayProfilesGui.Hotkeys
{
    public sealed class WinFormsHotkey : IDisposable
    {
        private bool _disposed;
        private readonly Hotkey _hotkey;
        private readonly WinFormsHotkeyMessageFilter _filter;

        public WinFormsHotkey(Keys keys, bool noRepeat, Action callback)
        {
            var split = SplitModifiers(keys);
            var modifiers = split.Item1;
            if (noRepeat)
                modifiers |= NativeMethods.KeyModifiers.NoRepeat;
            _hotkey = ApplicationHotkeyManager.Instance.Register(IntPtr.Zero, modifiers, unchecked((uint) split.Item2));
            _filter = new WinFormsHotkeyMessageFilter(_hotkey, callback);
            Application.AddMessageFilter(_filter);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Application.RemoveMessageFilter(_filter);
            ApplicationHotkeyManager.Instance.Unregister(_hotkey.Id);
        }

        public static Tuple<NativeMethods.KeyModifiers, Keys> SplitModifiers(Keys keys)
        {
            var modifiers = NativeMethods.KeyModifiers.None;
            if ((keys & Keys.Control) == Keys.Control)
            {
                modifiers |= NativeMethods.KeyModifiers.Control;
                keys ^= Keys.Control;
            }
            if ((keys & Keys.Shift) == Keys.Shift)
            {
                modifiers |= NativeMethods.KeyModifiers.Shift;
                keys ^= Keys.Shift;
            }
            if ((keys & Keys.Alt) == Keys.Alt)
            {
                modifiers |= NativeMethods.KeyModifiers.Alt;
                keys ^= Keys.Alt;
            }
            if (keys == Keys.ControlKey || keys == Keys.LControlKey || keys == Keys.RControlKey ||
                keys == Keys.ShiftKey || keys == Keys.LShiftKey || keys == Keys.RShiftKey ||
                keys == Keys.Menu)
                keys = Keys.None;
            return Tuple.Create(modifiers, keys);
        }

        public static KeyEventHandler CreateKeyDownHandler(Action<Keys> hotkeyCallback)
        {
            return (sender, e) =>
            {
                var control = sender as Control;
                if (control == null)
                    return;

                // Prevent the text box from getting the key press.
                e.SuppressKeyPress = true;

                // Ensure there is at least one modifier key.
                if (e.Modifiers == Keys.None)
                {
                    control.Text = "";
                    hotkeyCallback(Keys.None);
                    return;
                }

                // Show the hotkey (or at least the current modifiers)
                var split = SplitModifiers(e.KeyData);
                control.Text = HotkeyString(split.Item1, split.Item2);

                if (split.Item2 == Keys.None)
                {
                    hotkeyCallback(Keys.None);
                    return;
                }

                hotkeyCallback(e.KeyData);
            };
        }

        public static string HotkeyString(Keys hotkey)
        {
            var split = SplitModifiers(hotkey);
            return HotkeyString(split.Item1, split.Item2);
        }

        public static string HotkeyString(NativeMethods.KeyModifiers modifiers, Keys key)
        {
            var modifier = string.Join(" + ", HotkeyModifierStrings(modifiers));
            if (modifier == "")
                return modifier;
            return modifier + " + " + KeyToString(key);
        }

        private static IEnumerable<string> HotkeyModifierStrings(NativeMethods.KeyModifiers modifiers)
        {
            if ((modifiers & NativeMethods.KeyModifiers.Control) == NativeMethods.KeyModifiers.Control)
                yield return "Ctrl";
            if ((modifiers & NativeMethods.KeyModifiers.Alt) == NativeMethods.KeyModifiers.Alt)
                yield return "Alt";
            if ((modifiers & NativeMethods.KeyModifiers.Shift) == NativeMethods.KeyModifiers.Shift)
                yield return "Shift";
        }

        private static string KeyToString(Keys key)
        {
            switch (key)
            {
                // Numeric keys
                case Keys.D0:
                    return "0";
                case Keys.D1:
                    return "1";
                case Keys.D2:
                    return "2";
                case Keys.D3:
                    return "3";
                case Keys.D4:
                    return "4";
                case Keys.D5:
                    return "5";
                case Keys.D6:
                    return "6";
                case Keys.D7:
                    return "7";
                case Keys.D8:
                    return "8";
                case Keys.D9:
                    return "9";

                // Multiple definitions of values when we want a specific value
                case Keys.CapsLock:
                    return "CapsLock";
                case Keys.HanguelMode:
                    return "HangulMode";
                case Keys.IMEAceept:
                    return "Accept";
                case Keys.OemSemicolon: // Oem1
                    return "Semicolon";
                case Keys.OemQuestion: // Oem2
                    return "Question";
                case Keys.Oemtilde: // Oem3
                    return "Tilde";
                case Keys.OemOpenBrackets: // Oem4
                    return "OpenBrackets";
                case Keys.OemPipe: // Oem5
                    return "Pipe";
                case Keys.OemCloseBrackets: // Oem6
                    return "CloseBrackets";
                case Keys.OemQuotes: // Oem7
                    return "Quotes";
                case Keys.OemBackslash: // Oem102
                    return "Backslash";

                // Capitalization fixes
                case Keys.Oemcomma:
                    return "Comma";
                case Keys.Oemplus:
                    return "Plus";

                // Special logic
                case Keys.Oem8:
                    return "Oem8"; // Prevent from showing up as "8"
                case Keys.None:
                    return "";

                default:
                    return key.ToString().Replace("Oem", "").Replace("IME", "");
            }
        }
    }
}
