using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DisplayProfiles;
using Newtonsoft.Json;

namespace DisplayProfilesGui
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Profile.JsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
            SettingsFiles.EnsureCreated();
            using (new MainForm())
                Application.Run();
        }
    }
}
