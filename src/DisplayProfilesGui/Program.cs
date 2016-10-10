using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        static void Main(string[] args)
        {
            bool firstApplicationInstance;
            using (var mre = new EventWaitHandle(false, EventResetMode.ManualReset, ApplicationId, out firstApplicationInstance))
            {
                if (args.Length == 0 || args[0] == "installed")
                {
                    if (!firstApplicationInstance)
                        return;
                    Task.Run(() =>
                    {
                        mre.WaitOne();
                        Application.Exit();
                    });
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Profile.JsonSerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
                    SettingsFiles.EnsureCreated();
                    using (new MainForm(args.Length != 0 && args[0] == "installed"))
                        Application.Run();
                }
                else if (args[0] == "exit")
                {
                    mre.Set();
                }
            }
        }
    }
}
