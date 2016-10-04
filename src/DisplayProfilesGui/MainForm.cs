﻿using System;
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
        private readonly SortedDictionary<string, DisplaySettings> _profiles = new SortedDictionary<string, DisplaySettings>(StringComparer.InvariantCultureIgnoreCase);
        private readonly List<WinFormsHotkey> _hotkeys = new List<WinFormsHotkey>();

        public MainForm()
        {
            InitializeComponent();
            notifyIcon.Icon = Resources.MainIcon;
            Rebuild();
            DeviceChangeNotification.NativeMethods.RegisterForDeviceNotification(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg != DeviceChangeNotification.NativeMethods.WM_DEVICECHANGE)
                return;
            if (m.WParam == DeviceChangeNotification.NativeMethods.DBT_DEVICEQUERYREMOVE ||
                m.WParam == DeviceChangeNotification.NativeMethods.DBT_DEVICEREMOVEPENDING ||
                m.WParam == DeviceChangeNotification.NativeMethods.DBT_QUERYCHANGECONFIG)
                return;
            Debug.WriteLine("Saw msg.");
        }

        private void Rebuild()
        {
            // Load all profile files
            _profiles.Clear();
            var profiles = SettingsFiles.GetProfileNames();
            foreach (var name in profiles)
            {
                try
                {
                    _profiles.Add(name, Profile.LoadDisplaySettings(SettingsFiles.ProfileNameToFileName(name)));
                }
                catch (Exception ex)
                {
                    HandleError(ex, "Could not read display profile " + name);
                }
            }

            BuildTrayMenu();
            RefreshHotkeys();
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

            // Add to load menu
            foreach (var profile in _profiles)
            {
                var item = new ToolStripMenuItem(profile.Key, Resources.Profile.ToBitmap(), (_, __) => LoadProfile(profile.Key));
                var ex = profile.Value.Validate();
                if (ex != null)
                {
                    item.Image = Resources.Warning.ToBitmap();
                    item.ToolTipText = AugmentException(ex, profile.Value).Message;
                }
                contextMenuStrip.Items.Add(item);
            }

            // Menu for saving items
            var newProfileMenuItem = new ToolStripMenuItem("New Profile...", Resources.NewProfile.ToBitmap(), OnMenuSaveAs);
            if (_profiles.Count != 0)
            {
                contextMenuStrip.Items.Add(new ToolStripSeparator());
                var saveMenu = new ToolStripMenuItem("Save Profile", Resources.SaveProfile.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
                saveMenu.DropDownItems.Add(newProfileMenuItem);
                saveMenu.DropDownItems.Add(new ToolStripSeparator());
                foreach (var profile in _profiles.Keys)
                    saveMenu.DropDownItems.Add(new ToolStripMenuItem(profile, Resources.SaveProfile.ToBitmap(), (_, __) => SaveProfile(profile)));
                contextMenuStrip.Items.Add(saveMenu);
            }
            else
            {
                contextMenuStrip.Items.Add(newProfileMenuItem);
            }

            // Menu for deleting items
            if (_profiles.Count != 0)
            {
                var deleteMenu = new ToolStripMenuItem("Delete Profile", Resources.DeleteProfile.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
                foreach (var profile in _profiles.Keys)
                    deleteMenu.DropDownItems.Add(new ToolStripMenuItem(profile, Resources.DeleteProfile.ToBitmap(), (_, __) => DeleteProfile(profile)));
                contextMenuStrip.Items.Add(deleteMenu);
            }

            // Menu for hotkeys
            if (_profiles.Count != 0)
            {
                var hotkeyMenu = new ToolStripMenuItem("Hotkeys", Resources.Hotkey.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
                foreach (var profile in _profiles.Keys)
                {
                    var hotkey = SettingsFiles.ApplicationSettings.FindHotkeyForProfileName(profile);
                    hotkeyMenu.DropDownItems.Add(new ToolStripMenuItem(profile, Resources.Profile.ToBitmap(), (_, __) => SetHotkey(profile))
                    {
                        ShortcutKeys = hotkey,
                        ShowShortcutKeys = hotkey != Keys.None,
                    });
                }
                contextMenuStrip.Items.Add(hotkeyMenu);
            }

            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(new ToolStripMenuItem("About", Resources.About.ToBitmap(), OnAbout));
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

        private void OnAbout(object sender, EventArgs e)
        {
            using (var dialog = new AboutDialog())
                dialog.ShowDialog();
            Rebuild();
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
                var profile = _profiles[name];
                try
                {
                    profile.SetCurrent();
                }
                catch (Exception ex)
                {
                    throw AugmentException(ex, profile);
                }
            }, "Could not load display profile " + name);
        }

        private static Exception AugmentException(Exception ex, DisplaySettings profile)
        {
            if (profile.MissingAdapters.Count == 0)
                return ex;
            return new Exception(ex.Message + "\nThese adapters are missing:\n" + string.Join("\n", profile.MissingAdapters), ex);
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
                HandleError(ex, errorMessage);
            }
            finally
            {
                Rebuild();
            }
        }

        private void HandleError(Exception ex, string message)
        {
            notifyIcon.BalloonTipTitle = message;
            notifyIcon.BalloonTipText = ex.Message;
            notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
            notifyIcon.ShowBalloonTip(5000);
        }
    }
}
