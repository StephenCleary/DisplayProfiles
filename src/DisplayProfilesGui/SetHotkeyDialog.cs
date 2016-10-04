using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DisplayProfilesGui.Hotkeys;

namespace DisplayProfilesGui
{
    public partial class SetHotkeyDialog : Form
    {
        private Keys originalKeys;
        private Keys keys;

        public SetHotkeyDialog()
        {
            InitializeComponent();
            hotkeyTextBox.KeyDown += WinFormsHotkey.CreateKeyDownHandler(x =>
            {
                keys = x;
                EnableDisableButtons();
            });
            hotkeyTextBox.KeyUp += (_, __) => hotkeyTextBox.Text = WinFormsHotkey.HotkeyString(keys);
        }

        /// <summary>
        /// Executes the dialog for a specific profile name and existing hotkey. Returns <c>null</c> if the user cancelled, <c>Keys.None</c> to remove a hotkey; otherwise, returns the new hotkey.
        /// </summary>
        /// <param name="profileName">The name of the profile.</param>
        /// <param name="existingHotkey">The existing hotkey for that profile.</param>
        public static Keys? ExecuteDialog(string profileName, Keys existingHotkey)
        {
            using (var dialog = new SetHotkeyDialog())
            {
                dialog.Text += profileName;
                dialog.originalKeys = dialog.keys = existingHotkey;
                dialog.hotkeyTextBox.Text = WinFormsHotkey.HotkeyString(existingHotkey);
                dialog.EnableDisableButtons();
                var result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                    return null;
                return dialog.keys;
            }
        }

        private void EnableDisableButtons()
        {
            clearButton.Enabled = keys != Keys.None;
            okButton.Enabled = keys != originalKeys;
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            hotkeyTextBox.Text = "";
            keys = Keys.None;
            EnableDisableButtons();
        }
    }
}
