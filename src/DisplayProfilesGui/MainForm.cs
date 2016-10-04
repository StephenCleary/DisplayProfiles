using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DisplayProfiles;
using DisplayProfilesGui.Hotkeys;
using DisplayProfilesGui.Properties;

namespace DisplayProfilesGui
{
    public partial class MainForm : Form
    {
        private readonly List<WinFormsHotkey> _hotkeys = new List<WinFormsHotkey>();

        public MainForm()
        {
            InitializeComponent();
            notifyIcon.Icon = Resources.MainIcon;
            BuildTrayMenu();
            RefreshHotkeys();
            DeviceChangeNotification.NativeMethods.RegisterForDeviceNotification(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == DeviceChangeNotification.NativeMethods.WM_DEVICECHANGE)
            {
                Debug.WriteLine("Saw msg.");
            }
        }

        private void RefreshHotkeys()
        {
            foreach (var hotkey in _hotkeys)
                hotkey.Dispose();
            _hotkeys.Clear();
            foreach (var profileHotkey in SettingsFiles.ApplicationSettings.Hotkeys)
            {
                var name = profileHotkey.ProfileName;
                try
                {
                    _hotkeys.Add(new WinFormsHotkey(profileHotkey.Hotkey, true, () => LoadProfile(name)));
                }
                catch (Exception ex)
                {
                    notifyIcon.BalloonTipTitle = "Could not bind hotkey " + WinFormsHotkey.HotkeyString(profileHotkey.Hotkey) + " to " + name;
                    notifyIcon.BalloonTipText = ex.Message;
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                    notifyIcon.ShowBalloonTip(5000);
                }
            }
        }

        private void BuildTrayMenu()
        {
            contextMenuStrip.Items.Clear();

            // Find all profile files
            var profiles = SettingsFiles.GetProfileNames();

            // Add to load menu
            foreach (var profile in profiles)
                contextMenuStrip.Items.Add(new ToolStripMenuItem(profile, Resources.Profile.ToBitmap(), (_, __) => LoadProfile(profile)));

            // Menu for saving items
            var newProfileMenuItem = new ToolStripMenuItem("New Profile...", Resources.NewProfile.ToBitmap(), OnMenuSaveAs);
            if (profiles.Count != 0)
            {
                contextMenuStrip.Items.Add(new ToolStripSeparator());
                var saveMenu = new ToolStripMenuItem("Save Profile", Resources.SaveProfile.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
                saveMenu.DropDownItems.Add(newProfileMenuItem);
                saveMenu.DropDownItems.Add(new ToolStripSeparator());
                profiles.ForEach(x => saveMenu.DropDownItems.Add(new ToolStripMenuItem(x, Resources.SaveProfile.ToBitmap(), (_, __) => SaveProfile(x))));
                contextMenuStrip.Items.Add(saveMenu);
            }
            else
            {
                contextMenuStrip.Items.Add(newProfileMenuItem);
            }

            // Menu for deleting items
            if (profiles.Count != 0)
            {
                var deleteMenu = new ToolStripMenuItem("Delete Profile", Resources.DeleteProfile.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
                profiles.ForEach(x => deleteMenu.DropDownItems.Add(new ToolStripMenuItem(x, Resources.DeleteProfile.ToBitmap(), (_, __) => DeleteProfile(x))));
                contextMenuStrip.Items.Add(deleteMenu);
            }

            // Menu for hotkeys
            if (profiles.Count != 0)
            {
                var hotkeyMenu = new ToolStripMenuItem("Hotkeys", Resources.Hotkey.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
                profiles.ForEach(x =>
                {
                    var hotkey = SettingsFiles.ApplicationSettings.FindHotkeyForProfileName(x);
                    hotkeyMenu.DropDownItems.Add(new ToolStripMenuItem(x, Resources.Profile.ToBitmap(), (_, __) => SetHotkey(x))
                    {
                        ShortcutKeys = hotkey,
                        ShowShortcutKeys = hotkey != Keys.None,
                    });
                });
                contextMenuStrip.Items.Add(hotkeyMenu);
            }

            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(new ToolStripMenuItem("About", Resources.About.ToBitmap(), (_, __) =>
            {
                using (var dialog = new AboutDialog())
                    dialog.ShowDialog();
            }));
            contextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", Resources.Exit.ToBitmap(), (_, __) => Application.Exit()));
        }

        private void OnMenuSaveAs(object sender, EventArgs e)
        {
            using (var dialog = new ProfileNameDialog())
            {
                var result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                    return;
                SaveProfile(dialog.ProfileName);
            }
        }

        private void SetHotkey(string name)
        {
            ExecuteUiAction(() =>
            {
                var result = SetHotkeyDialog.ExecuteDialog(name, SettingsFiles.ApplicationSettings.FindHotkeyForProfileName(name));
                if (result == null)
                    return;
                SettingsFiles.ApplicationSettings.SetHotkeyForProfileName(name, result.Value);
                SettingsFiles.SaveApplicationSettings();
            }, "Could not set hotkey for profile " + name);
        }

        private void LoadProfile(string name)
        {
            ExecuteUiAction(() =>
            {
                var profile = Profile.LoadDisplaySettings(SettingsFiles.ProfileNameToFileName(name));
                try
                {
                    profile.SetCurrent();
                }
                catch (Exception ex)
                {
                    if (profile.MissingAdapters.Count == 0)
                        throw;
                    throw new Exception(ex.Message + "\nThese adapters are missing:\n" + string.Join("\n", profile.MissingAdapters), ex);
                }
            }, "Could not load display profile " + name);
        }

        private void SaveProfile(string name)
        {
            ExecuteUiAction(() => Profile.SaveCurrentDisplaySettings(SettingsFiles.ProfileNameToFileName(name)),
                "Could not save display profile " + name);
        }

        private void DeleteProfile(string name)
        {
            ExecuteUiAction(() =>
            {
                SettingsFiles.ApplicationSettings.SetHotkeyForProfileName(name, Keys.None);
                File.Delete(SettingsFiles.ProfileNameToFileName(name));
            }, "Could not delete display profile " + name);
        }

        private void ExecuteUiAction(Action action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                notifyIcon.BalloonTipTitle = errorMessage;
                notifyIcon.BalloonTipText = ex.Message;
                notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
                notifyIcon.ShowBalloonTip(5000);
            }
            finally
            {
                BuildTrayMenu();
                RefreshHotkeys();
            }
        }
    }
}
