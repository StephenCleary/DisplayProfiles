using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisplayProfilesGui.Hotkeys
{
    /// <summary>
    /// A singleton that manages hotkeys for an application (ids must be in the range 0x0-0xBFFF).
    /// </summary>
    public sealed class ApplicationHotkeyManager
    {
        private int _nextId = 0;
        private readonly Dictionary<int, Hotkey> _registeredHotkeys = new Dictionary<int, Hotkey>();

        private ApplicationHotkeyManager()
        {
        }

        public static ApplicationHotkeyManager Instance { get; } = new ApplicationHotkeyManager();

        public Hotkey Register(IntPtr window, NativeMethods.KeyModifiers modifiers, uint key)
        {
            var result = new Hotkey(window, _nextId, modifiers, key);
            _registeredHotkeys.Add(_nextId, result);
            IncrementNextId();
            return result;
        }

        public void Unregister(int id)
        {
            Hotkey hotkey;
            if (!_registeredHotkeys.TryGetValue(id, out hotkey))
                return;
            _registeredHotkeys.Remove(id);
            hotkey.Dispose();
        }

        private void IncrementNextId()
        {
            while (true)
            {
                _nextId = (_nextId + 1) % 0xC000;
                if (!_registeredHotkeys.ContainsKey(_nextId))
                    break;
            }
        }
    }
}
