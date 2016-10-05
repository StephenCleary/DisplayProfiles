using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DisplayProfilesGui
{
    public sealed class ApplicationSettings
    {
        public int Version { get; } = 0;
        public List<HotkeySetting> Hotkeys { get; } = new List<HotkeySetting>();

        /// <summary>
        /// Searches the list of hotkeys for one matching the specified hotkey id (which can be a profile name). If no hotkey is found, this method returns <c>Keys.None</c>.
        /// </summary>
        /// <param name="id">The name of the profile.</param>
        public Keys FindHotkey(string id)
        {
            var result = Hotkeys.FirstOrDefault(x => x.Id == id);
            if (result == null)
                return Keys.None;
            return result.Hotkey;
        }

        /// <summary>
        /// Sets a hotkey for an id (which can be a profile name). If <paramref name="hotkey"/> is <c>Keys.None</c>, then this method removes any hotkey associated with <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The name of the profile.</param>
        /// <param name="hotkey">The hotkey to set, or <c>Keys.None</c> to remove a hotkey.</param>
        public void SetHotkey(string id, Keys hotkey)
        {
            var entry = Hotkeys.FirstOrDefault(x => x.Id == id);
            if (entry == null)
            {
                if (hotkey == Keys.None)
                    return;
                entry = new HotkeySetting
                {
                    Id = id,
                    Hotkey = hotkey,
                };
                Hotkeys.Add(entry);
            }
            else
            {
                if (hotkey == Keys.None)
                    Hotkeys.Remove(entry);
                else
                    entry.Hotkey = hotkey;
            }
        }
    }
}
