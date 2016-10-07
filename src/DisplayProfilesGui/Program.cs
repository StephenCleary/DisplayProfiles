using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DisplayProfiles;
using Newtonsoft.Json;

namespace DisplayProfilesGui
{
    static class Program
    {
        private const string ApplicationId = "DisplayProfilesE6B3BA7BF428421AAA940209D4531850";
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool firstApplicationInstance;
            using (new Mutex(false, ApplicationId, out firstApplicationInstance))
            {
                if (!firstApplicationInstance)
                    return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Profile.JsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
                SettingsFiles.EnsureCreated();
                using (new MainForm())
                    Application.Run();
            }
        }
    }
}
