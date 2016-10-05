using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DisplayProfilesGui
{
    public static class SystemCommands
    {
        private static readonly Dictionary<string, string> _commands = new Dictionary<string, string>
        {
            { MonitorsOffCommand, "Put all displays to sleep" }
        };

        public const string MonitorsOffCommand = ">monitorsoff";

        public static string GetTitle(string systemCommandOrProfileName)
        {
            string result;
            if (_commands.TryGetValue(systemCommandOrProfileName, out result))
                return result;
            return systemCommandOrProfileName;
        }

        public static bool IsSystemCommand(string systemCommandOrProfileName) => _commands.ContainsKey(systemCommandOrProfileName);
    }
}
