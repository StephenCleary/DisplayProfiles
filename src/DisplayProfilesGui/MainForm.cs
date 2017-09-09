using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DisplayProfiles;
using DisplayProfilesGui.Hotkeys;
using DisplayProfilesGui.Properties;

namespace DisplayProfilesGui
{
    public partial class MainForm : Form
    {
        private readonly SortedDictionary<string, Exceptional<DisplaySettings>> _profiles = new SortedDictionary<string, Exceptional<DisplaySettings>>(StringComparer.InvariantCultureIgnoreCase);
        private readonly List<WinFormsHotkey> _hotkeys = new List<WinFormsHotkey>();
        private readonly Subject<Unit> _rebuild = new Subject<Unit>();
        private bool _contextMenuIsOpen;

        public MainForm(bool showInstalledMessage)
        {
            InitializeComponent();
            notifyIcon.Icon = Resources.MainIcon;
            Rebuild();
            DeviceChangeNotification.NativeMethods.RegisterForDeviceNotification(Handle);
            _rebuild.Throttle(TimeSpan.FromSeconds(0.5))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(_ => Rebuild());
            contextMenuStrip.Opening += (_, __) => _contextMenuIsOpen = true;
            contextMenuStrip.Closed += (_, __) => _contextMenuIsOpen = false;
            if (showInstalledMessage)
                BalloonTip("Display Profiles", "Display Profiles has been successfully installed and is now running.", ToolTipIcon.Info);
        }

        private void RequestRebuild() => _rebuild.OnNext(Unit.Default);

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg != DeviceChangeNotification.NativeMethods.WM_DEVICECHANGE)
                return;
            if (m.WParam != DeviceChangeNotification.NativeMethods.DBT_DEVNODES_CHANGED)
                return;
            RequestRebuild();
        }

        private void Rebuild()
        {
            if (_contextMenuIsOpen)
            {
                RequestRebuild();
                return;
            }

            // Load all profile files
            _profiles.Clear();
            var profiles = ProfileFiles.GetProfileNames();
            foreach (var name in profiles)
            {
                _profiles.Add(name, Exceptional.Create(() => ProfileFiles.LoadProfile(name)));
            }

            BuildTrayMenu();
            RefreshHotkeys();
        }

        private void RefreshHotkeys()
        {
            // Unregister all hotkeys.
            foreach (var hotkey in _hotkeys)
                hotkey.Dispose();
            _hotkeys.Clear();

            // Remove any hotkeys for profiles that no longer exist (but keep hotkeys for system commands).
            if (SettingsFiles.ApplicationSettings.Hotkeys.RemoveAll(x => !SystemCommands.IsSystemCommand(x.Id) && !_profiles.ContainsKey(x.Id)) != 0)
                SettingsFiles.SaveApplicationSettings();

            // Register hotkeys.
            foreach (var hotkeySetting in SettingsFiles.ApplicationSettings.Hotkeys)
            {
                var id = hotkeySetting.Id;
                try
                {
                    _hotkeys.Add(new WinFormsHotkey(hotkeySetting.Hotkey, true, () => ExecuteSystemCommandOrLoadProfile(hotkeySetting.Id)));
                }
                catch (Exception ex)
                {
                    BalloonTip("Could not bind hotkey " + WinFormsHotkey.HotkeyString(hotkeySetting.Hotkey) + " to " + SystemCommands.GetTitle(id), ex.Message, ToolTipIcon.Warning);
                }
            }
        }

        private void BuildTrayMenu()
        {
            contextMenuStrip.Items.Clear();

            // Add to load menu
            foreach (var kvp in _profiles)
            {
                var name = kvp.Key;
                var profile = kvp.Value;
                var item = new ToolStripMenuItem(name, Resources.Profile.ToBitmap(), (_, __) => LoadProfile(name));
                var ex = profile.Exception ?? profile.Value.Validate();
                if (ex != null)
                {
                    item.Image = Resources.Warning.ToBitmap();
                    var message = ex.Message;
                    var extraMessage = profile.TryValue?.MissingAdaptersMessage();
                    if (!string.IsNullOrEmpty(extraMessage))
                        message += "\r\n" + extraMessage;
                    item.ToolTipText = message;
                }
                contextMenuStrip.Items.Add(item);
            }

            contextMenuStrip.Items.Add(new ToolStripMenuItem(SystemCommands.GetTitle(SystemCommands.MonitorsOffCommand), Resources.MainIcon.ToBitmap(),
                (_, __) => ExecuteSystemCommand(SystemCommands.MonitorsOffCommand)));
            contextMenuStrip.Items.Add(new ToolStripSeparator());

            // Menu for saving items
            var newProfileMenuItem = new ToolStripMenuItem("New Profile...", Resources.NewProfile.ToBitmap(), OnMenuSaveAs);
            if (_profiles.Count != 0)
            {
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
                    hotkeyMenu.DropDownItems.Add(HotkeyMenuItem(Resources.Profile, profile));
                hotkeyMenu.DropDownItems.Add(HotkeyMenuItem(Resources.MainIcon, SystemCommands.MonitorsOffCommand));
                contextMenuStrip.Items.Add(hotkeyMenu);
            }

            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(new ToolStripMenuItem("About", Resources.About.ToBitmap(), OnAbout));
            contextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (_, __) => Application.Exit()));
        }

        private ToolStripMenuItem HotkeyMenuItem(Icon icon, string id)
        {
            var hotkey = SettingsFiles.ApplicationSettings.FindHotkey(id);
            return new ToolStripMenuItem(SystemCommands.GetTitle(id), icon.ToBitmap(), (_, __) => SetHotkey(id))
            {
                ShortcutKeys = hotkey,
                ShowShortcutKeys = hotkey != Keys.None,
            };
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

        private void SetHotkey(string id)
        {
            ExecuteUiAction(() =>
            {
                var result = SetHotkeyDialog.ExecuteDialog(SystemCommands.GetTitle(id), SettingsFiles.ApplicationSettings.FindHotkey(id));
                if (result == null)
                    return;
                SettingsFiles.ApplicationSettings.SetHotkey(id, result.Value);
                SettingsFiles.SaveApplicationSettings();
            }, "Could not set hotkey for " + SystemCommands.GetTitle(id));
        }

        private void LoadProfile(string name)
        {
            ExecuteUiAction(() =>
            {
                var profile = _profiles[name];
                try
                {
                    profile.Value.SetCurrent();
                }
                catch (Exception ex)
                {
                    var extraMessage = profile.TryValue?.MissingAdaptersMessage();
                    if (!string.IsNullOrEmpty(extraMessage))
                        throw;
                    throw new Exception(ex.Message + "\r\n" + extraMessage, ex);
                }
            }, "Could not load display profile " + name);
        }

        private void SaveProfile(string name)
        {
            ExecuteUiAction(() => ProfileFiles.SaveProfile(name),
                "Could not save display profile " + name);
        }

        private void DeleteProfile(string name)
        {
            ExecuteUiAction(() =>
            {
                SettingsFiles.ApplicationSettings.SetHotkey(name, Keys.None);
                SettingsFiles.SaveApplicationSettings();
                ProfileFiles.DeleteProfile(name);
            }, "Could not delete display profile " + name);
        }

        private void ExecuteSystemCommand(string name)
        {
            if (name == SystemCommands.MonitorsOffCommand)
            {
                ExecuteUiAction(async () =>
                {
                    // Monitors turn back on based on mouse activity, so we delay shutting them down to ensure they're not immediately turned back on again.
                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                    Messages.NativeMethods.TurnMonitorsOff();
                }, "Could not turn monitors off");
            }
        }

        private void ExecuteSystemCommandOrLoadProfile(string id)
        {
            if (SystemCommands.IsSystemCommand(id))
                ExecuteSystemCommand(id);
            else
                LoadProfile(id);
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
                RequestRebuild();
            }
        }

        private async void ExecuteUiAction(Func<Task> action, string errorMessage)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                HandleError(ex, errorMessage);
            }
            finally
            {
                RequestRebuild();
            }
        }

        private void HandleError(Exception ex, string message) => BalloonTip(message, ex.Message, ToolTipIcon.Error);

        private void BalloonTip(string title, string text, ToolTipIcon icon)
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = text;
            notifyIcon.BalloonTipIcon = icon;
            notifyIcon.ShowBalloonTip(5000);
        }
    }
}
