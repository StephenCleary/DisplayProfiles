using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DisplayProfilesGui
{
    public partial class ProfileNameDialog : Form
    {
        public ProfileNameDialog()
        {
            InitializeComponent();
        }

        public string ProfileName => nameTextBox.Text;
    }
}
