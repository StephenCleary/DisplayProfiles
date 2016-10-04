using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DisplayProfilesGui
{
    public sealed class ApplicationSettings
    {
        public List<HotkeySetting> Hotkeys { get; } = new List<HotkeySetting>();

        /// <summary>
        /// Searches the list of hotkeys for one matching the specified profile name. If no hotkey is found, this method returns <c>Keys.None</c>.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        public Keys FindHotkeyForProfileName(string profileName)
        {
            var result = Hotkeys.FirstOrDefault(x => x.ProfileName == profileName);
            if (result == null)
                return Keys.None;
            return result.Hotkey;
        }

        /// <summary>
        /// Sets a hotkey for a profile. If <paramref name="hotkey"/> is <c>Keys.None</c>, then this method removes any hotkey associated with <paramref name="profileName"/>.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        /// <param name="hotkey">The hotkey to set, or <c>Keys.None</c> to remove a hotkey.</param>
        public void SetHotkeyForProfileName(string profileName, Keys hotkey)
        {
            var profile = Hotkeys.FirstOrDefault(x => x.ProfileName == profileName);
            if (profile == null)
            {
                if (hotkey == Keys.None)
                    return;
                profile = new HotkeySetting
                {
                    ProfileName = profileName,
                    Hotkey = hotkey,
                };
                Hotkeys.Add(profile);
            }
            else
            {
                if (hotkey == Keys.None)
                    Hotkeys.Remove(profile);
                else
                    profile.Hotkey = hotkey;
            }
        }
    }
}
