using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisplayProfilesGui.Hotkeys
{
    public sealed class Hotkey: IDisposable
    {
        private readonly IntPtr _window;

        public Hotkey(IntPtr window, int id, NativeMethods.KeyModifiers modifiers, uint key)
        {
            _window = window;
            Id = id;
            NativeMethods.RegisterHotKey(window, id, modifiers, key);
        }

        public int Id { get; }

        public void Dispose()
        {
            NativeMethods.UnregisterHotKey(_window, Id);
        }

        public Func<IntPtr, int, IntPtr, IntPtr, bool> CreateMessageFilter(Action callback)
        {
            return NativeMethods.CreateMessageFilter(_window, Id, callback);
        }
    }
}
