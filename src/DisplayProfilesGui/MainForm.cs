using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DisplayProfiles;
using DisplayProfilesGui.Properties;

namespace DisplayProfilesGui
{
    public partial class MainForm : Form
    {
        private readonly string SettingsProfilesDirectory;

        public MainForm()
        {
            var settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DisplayProfiles");
            SettingsProfilesDirectory = Path.Combine(settingsDirectory, "Profiles");
            if (!Directory.Exists(settingsDirectory))
                Directory.CreateDirectory(settingsDirectory);
            if (!Directory.Exists(SettingsProfilesDirectory))
                Directory.CreateDirectory(SettingsProfilesDirectory);

            InitializeComponent();
            notifyIcon.Icon = Resources.MainIcon;
            BuildTrayMenu();
        }

        private string ProfileNameToFileName(string profileName)
        {
            return Path.Combine(SettingsProfilesDirectory, profileName + ".json");
        }

        private string FileNameToProfileName(string fileName)
        {
            return Path.GetFileNameWithoutExtension(fileName);
        }

        private void BuildTrayMenu()
        {
            contextMenuStrip.Items.Clear();

            contextMenuStrip.Items.Add(new ToolStripLabel("Load Profile"));
            contextMenuStrip.Items.Add(new ToolStripSeparator());

            // Find all profile files
            var profiles = Directory.GetFiles(SettingsProfilesDirectory, "*.json").Select(FileNameToProfileName).ToList();

            // Add to load menu
            foreach (var profile in profiles)
                contextMenuStrip.Items.Add(new ToolStripMenuItem(profile, Resources.Profile.ToBitmap(), (_, __) => LoadProfile(profile)));

            // Menu for saving items
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            var saveMenu = new ToolStripMenuItem("Save Profile", Resources.SaveProfile.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
            contextMenuStrip.Items.Add(saveMenu);

            saveMenu.DropDownItems.Add(new ToolStripMenuItem("New Profile...", Resources.NewProfile.ToBitmap(), OnMenuSaveAs));
            saveMenu.DropDownItems.Add(new ToolStripSeparator());

            // Menu for deleting items
            var deleteMenu = new ToolStripMenuItem("Delete Profile", Resources.DeleteProfile.ToBitmap()) { DropDown = new ToolStripDropDownMenu() };
            contextMenuStrip.Items.Add(deleteMenu);

            //// Menu for hotkeys
            //ToolStripMenuItem hotkeyMenu = new ToolStripMenuItem("Set Hotkeys");
            //hotkeyMenu.ImageIndex = 7;
            //hotkeyMenu.DropDown = new ToolStripDropDownMenu();
            //hotkeyMenu.DropDown.ImageList = trayMenu.ImageList;
            //trayMenu.Items.Add(hotkeyMenu);

            // Profile-specific sub-menu items
            foreach (var profile in profiles)
            {
                saveMenu.DropDownItems.Add(new ToolStripMenuItem(profile, Resources.SaveProfile.ToBitmap(), (_, __) => SaveProfile(profile)));
                deleteMenu.DropDownItems.Add(new ToolStripMenuItem(profile, Resources.DeleteProfile.ToBitmap(), (_, __) => DeleteProfile(profile)));

                //string hotkeyString = "(No Hotkey)";
                //// check if a hotkey is assigned
                //Hotkey hotkey = FindHotkey(Path.GetFileNameWithoutExtension(profile));
                //if (hotkey != null)
                //{
                //    hotkeyString = "(" + hotkey.ToString() + ")";
                //}

                //newMenuItem = hotkeyMenu.DropDownItems.Add(itemCaption + " " + hotkeyString);
                //newMenuItem.Tag = itemCaption;
                //newMenuItem.Click += OnHotkeySet;
                //newMenuItem.ImageIndex = 3;
            }

            //trayMenu.Items.Add("-");
            //newMenuItem = trayMenu.Items.Add("Turn Off All Monitors");
            //newMenuItem.Click += OnEnergySaving;
            //newMenuItem.ImageIndex = 0;

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

        private void LoadProfile(string name)
        {
            try
            {
                Profile.LoadDisplaySettingsAndSetAsCurrent(ProfileNameToFileName(name));
            }
            catch (Exception ex)
            {
                HandleError(ex, "Could not load display profile " + name);
            }
            finally
            {
                BuildTrayMenu();
            }
        }

        private void SaveProfile(string name)
        {
            try
            {
                Profile.SaveCurrentDisplaySettings(ProfileNameToFileName(name));
            }
            catch (Exception ex)
            {
                HandleError(ex, "Could not save display profile " + name);
            }
            finally
            {
                BuildTrayMenu();
            }
        }

        private void DeleteProfile(string name)
        {
            try
            {
                File.Delete(ProfileNameToFileName(name));
            }
            catch (Exception ex)
            {
                HandleError(ex, "Could not delete display profile " + name);
            }
            finally
            {
                BuildTrayMenu();
            }
        }

        private void HandleError(Exception ex, string title)
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = ex.Message;
            notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
            notifyIcon.ShowBalloonTip(5000);
        }
    }
}
