/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;

namespace MonitorSwitcherGUI
{
    public class MonitorSwitcherGUI : Form
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MonitorSwitcherGUI());
        }

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private String settingsDirectory;
        private String settingsDirectoryProfiles;
        private List<Hotkey> Hotkeys;
        //private GlobalKeyboardHook KeyHook; 

        public MonitorSwitcherGUI()
        {
            // Initialize settings directory
            settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"MonitorSwitcher");
            settingsDirectoryProfiles = Path.Combine(settingsDirectory, "Profiles");
            if (!Directory.Exists(settingsDirectory))
                Directory.CreateDirectory(settingsDirectory);
            if (!Directory.Exists(settingsDirectoryProfiles))
                Directory.CreateDirectory(settingsDirectoryProfiles);

            // Initialize Hotkey list before loading settings
            Hotkeys = new List<Hotkey>();

            // Load all settings
            LoadSettings();

            // Inizialize globa keyboard hook or hotkeys
            //KeyHook = new GlobalKeyboardHook();
            //KeyHook.KeyDown += new KeyEventHandler(KeyHook_KeyDown);
            //KeyHook.KeyUp += new KeyEventHandler(KeyHook_KeyUp);

            // Refresh Hotkey Hooks
            KeyHooksRefresh();

            // Build up context menu
            trayMenu = new ContextMenuStrip();
            trayMenu.ImageList = new ImageList();
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "MainIcon.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "DeleteProfile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "Exit.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "Profile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "SaveProfile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "NewProfile.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "About.ico"));
            trayMenu.ImageList.Images.Add(new Icon(GetType(), "Hotkey.ico"));
            BuildTrayMenu();            

            // Create tray icon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Monitor Profile Switcher";
            trayIcon.Icon = new Icon(GetType(), "MainIcon.ico");
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.MouseUp += OnTrayClick;
        }

        private void KeyHooksRefresh()
        {
            List<Hotkey> removeList = new List<Hotkey>();
            // check which hooks are still valid
            foreach (Hotkey hotkey in Hotkeys)
            {
                if (!File.Exists(ProfileFileFromName(hotkey.profileName)))
                {
                    hotkey.UnregisterHotkey();
                    removeList.Add(hotkey);
                }
            }
            if (removeList.Count > 0)
            {
                foreach (Hotkey hotkey in removeList)
                {
                    Hotkeys.Remove(hotkey);                    
                }
                removeList.Clear();
                SaveSettings();
            }

            // register the valid hooks
            foreach (Hotkey hotkey in Hotkeys)
            {
                hotkey.UnregisterHotkey();
                hotkey.RegisterHotkey(this);
            }           
        }

        public void KeyHook_KeyUp(object sender, HandledEventArgs e)
        {
            HotkeyCtrl hotkeyCtrl = (sender as HotkeyCtrl);   
            Hotkey hotkey = FindHotkey(hotkeyCtrl);
            LoadProfile(hotkey.profileName);
            e.Handled = true;
        }

        public void KeyHook_KeyDown(object sender, HandledEventArgs e)
        {            
            e.Handled = true;
        } 

        public void LoadSettings()
        {
            // Unregister and clear all existing hotkeys
            foreach (Hotkey hotkey in Hotkeys) {
                hotkey.UnregisterHotkey();
            }
            Hotkeys.Clear();

            // Loading the xml file
            if (!File.Exists(SettingsFileFromName("Hotkeys")))
                return;

            System.Xml.Serialization.XmlSerializer readerHotkey = new System.Xml.Serialization.XmlSerializer(typeof(Hotkey));           

            try
            {
                XmlReader xml = XmlReader.Create(SettingsFileFromName("Hotkeys"));
                xml.Read();
                while (true)
                {
                    if ((xml.Name.CompareTo("Hotkey") == 0) && (xml.IsStartElement()))
                    {
                        Hotkey hotkey = (Hotkey)readerHotkey.Deserialize(xml);
                        Hotkeys.Add(hotkey);
                        continue;
                    }

                    if (!xml.Read())
                    {
                        break;
                    }
                }
                xml.Close();
            }
            catch
            {
            }
        }

        public void SaveSettings()
        {
            System.Xml.Serialization.XmlSerializer writerHotkey = new System.Xml.Serialization.XmlSerializer(typeof(Hotkey));

            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.CloseOutput = true;

            try
            {
                using (FileStream fileStream = new FileStream(SettingsFileFromName("Hotkeys"), FileMode.Create))
                {
                    XmlWriter xml = XmlWriter.Create(fileStream, xmlSettings);
                    xml.WriteStartDocument();
                    xml.WriteStartElement("hotkeys");
                    foreach (Hotkey hotkey in Hotkeys)
                    {
                        writerHotkey.Serialize(xml, hotkey);
                    }
                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                    xml.Flush();
                    xml.Close();

                    fileStream.Close();
                }
            }
            catch
            {
            }
        }

        public Hotkey FindHotkey(HotkeyCtrl ctrl)
        {
            foreach (Hotkey hotkey in Hotkeys)
            {
                if (hotkey.hotkeyCtrl == ctrl)
                    return hotkey;
            }

            return null;
        }

        public Hotkey FindHotkey(String name)
        {
            foreach (Hotkey hotkey in Hotkeys)
            {
                if (hotkey.profileName.CompareTo(name) == 0)
                    return hotkey;
            }

            return null;
        }

        public void BuildTrayMenu()
        {
            ToolStripItem newMenuItem;

            trayMenu.Items.Clear();

            trayMenu.Items.Add("Load Profile").Enabled = false;
            trayMenu.Items.Add("-");

            // Find all profile files
            string[] profiles = Directory.GetFiles(settingsDirectoryProfiles, "*.xml");

            // Add to load menu
            foreach (string profile in profiles)
            {
                string itemCaption = Path.GetFileNameWithoutExtension(profile);
                newMenuItem = trayMenu.Items.Add(itemCaption);
                newMenuItem.Click += OnMenuLoad;
                newMenuItem.ImageIndex = 3;
            }

            // Menu for saving items
            trayMenu.Items.Add("-");
            ToolStripMenuItem saveMenu = new ToolStripMenuItem("Save Profile");
            saveMenu.ImageIndex = 4;
            saveMenu.DropDown = new ToolStripDropDownMenu();
            saveMenu.DropDown.ImageList = trayMenu.ImageList;
            trayMenu.Items.Add(saveMenu);            

            newMenuItem = saveMenu.DropDownItems.Add("New Profile...");
            newMenuItem.Click += OnMenuSaveAs;
            newMenuItem.ImageIndex = 5;
            saveMenu.DropDownItems.Add("-");

            // Menu for deleting items
            ToolStripMenuItem deleteMenu = new ToolStripMenuItem("Delete Profile");
            deleteMenu.ImageIndex = 1;
            deleteMenu.DropDown = new ToolStripDropDownMenu();
            deleteMenu.DropDown.ImageList = trayMenu.ImageList;
            trayMenu.Items.Add(deleteMenu);

            // Menu for hotkeys
            ToolStripMenuItem hotkeyMenu = new ToolStripMenuItem("Set Hotkeys");
            hotkeyMenu.ImageIndex = 7;
            hotkeyMenu.DropDown = new ToolStripDropDownMenu();
            hotkeyMenu.DropDown.ImageList = trayMenu.ImageList;
            trayMenu.Items.Add(hotkeyMenu);

            // Add to delete, save and hotkey menus
            foreach (string profile in profiles)
            {
                string itemCaption = Path.GetFileNameWithoutExtension(profile);
                newMenuItem = saveMenu.DropDownItems.Add(itemCaption);
                newMenuItem.Click += OnMenuSave;
                newMenuItem.ImageIndex = 3;

                newMenuItem = deleteMenu.DropDownItems.Add(itemCaption);
                newMenuItem.Click += OnMenuDelete;
                newMenuItem.ImageIndex = 3;

                string hotkeyString = "(No Hotkey)";
                // check if a hotkey is assigned
                Hotkey hotkey = FindHotkey(Path.GetFileNameWithoutExtension(profile));
                if (hotkey != null)
                {
                    hotkeyString = "(" + hotkey.ToString() + ")";
                }

                newMenuItem = hotkeyMenu.DropDownItems.Add(itemCaption + " " + hotkeyString);
                newMenuItem.Tag = itemCaption;
                newMenuItem.Click += OnHotkeySet;
                newMenuItem.ImageIndex = 3;                
            }

            trayMenu.Items.Add("-");
            newMenuItem = trayMenu.Items.Add("Turn Off All Monitors");
            newMenuItem.Click += OnEnergySaving;
            newMenuItem.ImageIndex = 0;

            trayMenu.Items.Add("-");
            newMenuItem = trayMenu.Items.Add("About");
            newMenuItem.Click += OnMenuAbout;
            newMenuItem.ImageIndex = 6;

            newMenuItem = trayMenu.Items.Add("Exit");
            newMenuItem.Click += OnMenuExit;
            newMenuItem.ImageIndex = 2;
        }

        public string ProfileFileFromName(string name)
        {
            string fileName = name + ".xml";
            string filePath = Path.Combine(settingsDirectoryProfiles, fileName);

            return filePath;
        }

        public string SettingsFileFromName(string name)
        {
            string fileName = name + ".xml";
            string filePath = Path.Combine(settingsDirectory, fileName);

            return filePath;
        }

        public void OnEnergySaving(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(500); // wait for 500 milliseconds to give the user the chance to leave the mouse alone
            SendMessageAPI.PostMessage(new IntPtr(SendMessageAPI.HWND_BROADCAST), SendMessageAPI.WM_SYSCOMMAND, new IntPtr(SendMessageAPI.SC_MONITORPOWER), new IntPtr(SendMessageAPI.MONITOR_OFF));
        }

        public void OnMenuAbout(object sender, EventArgs e)
        { 
            MessageBox.Show("Monitor Profile Switcher by Martin Krämer \n(MartinKraemer84@gmail.com)\nVersion 0.5.2.0\nCopyright 2013-2016 \n\nhttps://sourceforge.net/projects/monitorswitcher/", "About Monitor Profile Switcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void OnMenuSaveAs(object sender, EventArgs e)
        {
            string profileName = "New Profile";
            if (InputBox("Save as new profile", "Enter name of new profile", ref profileName) == DialogResult.OK)
            {
                string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                foreach (char invalidChar in invalidChars)
                {
                    profileName = profileName.Replace(invalidChar.ToString(), "");
                }

                if (profileName.Trim().Length > 0)
                {
                    if (!MonitorSwitcher.SaveDisplaySettings(ProfileFileFromName(profileName)))
                    {
                        trayIcon.BalloonTipTitle = "Failed to save Multi Monitor profile";
                        trayIcon.BalloonTipText = "MonitorSwitcher was unable to save the current profile to a new profile with name\"" + profileName + "\"";
                        trayIcon.BalloonTipIcon = ToolTipIcon.Error;
                        trayIcon.ShowBalloonTip(5000);
                    }
                }
            }
        }

        public void OnHotkeySet(object sender, EventArgs e)
        {
            string profileName = (((ToolStripMenuItem)sender).Tag as string);
            Hotkey hotkey = FindHotkey(profileName);
            Boolean isNewHotkey = false;
            if (hotkey == null)
                isNewHotkey = true;
            if (HotkeySetting("Set Hotkey for Monitor Profile '" + profileName + "'", "Enter name of new profile", ref hotkey) == DialogResult.OK)
            {
                if ((isNewHotkey) && (hotkey != null))
                {
                    if (!hotkey.RemoveKey)
                    {
                        hotkey.profileName = profileName;
                        Hotkeys.Add(hotkey);
                    }
                }
                else if (hotkey != null)
                {
                    if (hotkey.RemoveKey)
                    {
                        Hotkeys.Remove(hotkey);
                    }
                }

                KeyHooksRefresh();
                SaveSettings();
            }
        }

        public void LoadProfile(string name)
        {
            if (!MonitorSwitcher.LoadDisplaySettings(ProfileFileFromName(name)))
            {
                trayIcon.BalloonTipTitle = "Failed to load Multi Monitor profile";
                trayIcon.BalloonTipText = "MonitorSwitcher was unable to load the previously saved profile \"" + name + "\"";
                trayIcon.BalloonTipIcon = ToolTipIcon.Error;
                trayIcon.ShowBalloonTip(5000);
            }
        }

        public void OnMenuLoad(object sender, EventArgs e)
        {
            LoadProfile(((ToolStripMenuItem)sender).Text);
        }

        public void OnMenuSave(object sender, EventArgs e)
        {
            if (!MonitorSwitcher.SaveDisplaySettings(ProfileFileFromName(((ToolStripMenuItem)sender).Text)))
            {
                trayIcon.BalloonTipTitle = "Failed to save Multi Monitor profile";
                trayIcon.BalloonTipText = "MonitorSwitcher was unable to save the current profile to name\"" + ((ToolStripMenuItem)sender).Text + "\"";
                trayIcon.BalloonTipIcon = ToolTipIcon.Error;
                trayIcon.ShowBalloonTip(5000);
            }
        }

        public void OnMenuDelete(object sender, EventArgs e)
        {
            File.Delete(ProfileFileFromName(((ToolStripMenuItem)sender).Text));
        }

        public void OnTrayClick(object sender, MouseEventArgs e)
        {
            BuildTrayMenu();

            if (e.Button == MouseButtons.Left)
            {
                System.Reflection.MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                mi.Invoke(trayIcon, null);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            KeyHooksRefresh();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.
            
            base.OnLoad(e);
        }

        private void OnMenuExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }

        public static DialogResult HotkeySetting(string title, string promptText, ref Hotkey value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            Button buttonClear = new Button();

            form.Text = title;
            label.Text = "Press hotkey combination or click 'Clear Hotkey' to remove the current hotkey";
            if (value != null)
                textBox.Text = value.ToString();
            textBox.Tag = value;

            buttonClear.Text = "Clear Hotkey";
            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 10, 372, 13);
            textBox.SetBounds(12, 36, 372 - 75 -8, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);
            buttonClear.SetBounds(309, 36 - 1, 75, 23);

            buttonClear.Tag = textBox;
            buttonClear.Click += new EventHandler(buttonClear_Click);
            textBox.KeyDown += new KeyEventHandler(textBox_KeyDown);
            textBox.KeyUp += new KeyEventHandler(textBox_KeyUp);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel, buttonClear });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = (textBox.Tag as Hotkey);
            return dialogResult;
        }

        static void textBox_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox textBox = (sender as TextBox);

            if (textBox.Tag != null)
            {
                Hotkey hotkey = (textBox.Tag as Hotkey);
                // check if any additional key was pressed, if not don't acceppt hotkey
                if ((hotkey.Key < Keys.D0) || ((!hotkey.Alt) && (!hotkey.Ctrl) && (!hotkey.Shift)))
                    textBox.Text = "";
            }                
        }
        
        static void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (sender as TextBox);
            Hotkey hotkey = (textBox.Tag as Hotkey);
            if (hotkey == null)
                hotkey = new Hotkey();
            hotkey.AssignFromKeyEventArgs(e);            

            e.Handled = true;
            e.SuppressKeyPress = true; // don't add user input to text box, just use custom display

            textBox.Text = hotkey.ToString();
            textBox.Tag = hotkey; // store the current key combination in the textbox tag (for later use)
         }

        static void buttonClear_Click(object sender, EventArgs e)
        {
            TextBox textBox = (sender as Button).Tag as TextBox;

            if (textBox.Tag != null)
            {
                Hotkey hotkey = (textBox.Tag as Hotkey);
                hotkey.RemoveKey = true;
            }
            textBox.Clear();
        }

        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;            

            label.SetBounds(9, 10, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Hotkey
    {
        public Boolean Ctrl;
        public Boolean Alt;
        public Boolean Shift;
        public Boolean RemoveKey; 
        public Keys Key;
        public String profileName;

        public HotkeyCtrl hotkeyCtrl;

        public Hotkey()
        {
            hotkeyCtrl = new HotkeyCtrl();
            RemoveKey = false;
        }

        public void RegisterHotkey(MonitorSwitcherGUI parent)
        {
            hotkeyCtrl.Alt = Alt;
            hotkeyCtrl.Shift = Shift;
            hotkeyCtrl.Control = Ctrl;
            hotkeyCtrl.KeyCode = Key;
            hotkeyCtrl.Pressed += new HandledEventHandler(parent.KeyHook_KeyUp);

            if (!hotkeyCtrl.GetCanRegister(parent))
            {
                // something went wrong, ignore for nw
            }
            else
            {
                hotkeyCtrl.Register(parent);
            }
        }

        public void UnregisterHotkey()
        {
            if (hotkeyCtrl.Registered)
            {
                hotkeyCtrl.Unregister();
            }
        }

        public void AssignFromKeyEventArgs(KeyEventArgs keyEvents)
        {
            Ctrl = keyEvents.Control;
            Alt = keyEvents.Alt;
            Shift = keyEvents.Shift;
            Key = keyEvents.KeyCode;
        }

        public override string ToString()
        {            
            List<string> keys = new List<string>();

            if (Ctrl == true)
            {
                keys.Add("CTRL");
            }

            if (Alt == true)
            {
                keys.Add("ALT");
            }

            if (Shift == true)
            {
                keys.Add("SHIFT");
            }

            switch (Key)
            {
                case Keys.ControlKey:
                case Keys.Alt:
                case Keys.ShiftKey:
                case Keys.Menu:
                    break;
                default:
                    keys.Add(Key.ToString()
                        .Replace("Oem", string.Empty)
                        );
                    break;
            }

            return string.Join(" + ", keys);
        }
    }
}
