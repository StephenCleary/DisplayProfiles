using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisplayProfiles;
using DisplayProfiles.Interop;
using Nito.Comparers;
using Nito.KitchenSink.OptionParsing;

namespace DisplayProfilesCmd
{
    class Program
    {
        static int Main()
        {
            var context = "";
            try
            {
                ProfileFiles.EnsureCreated();
                var options = OptionParser.Parse<Options>(Environment.CommandLine.Lex().Skip(1));
                if (options.Command == Options.ListCommand)
                {
                    OptionParser.Parse<ListOptions>(options.CommandArguments);
                    context = "Could not list profiles: ";
                    Console.WriteLine("Profile list:");
                    foreach (var name in ProfileFiles.GetProfileNames())
                        Console.WriteLine("  " + name);
                }
                else if (options.Command == Options.SaveCommand)
                {
                    var saveOptions = OptionParser.Parse<SaveOptions>(options.CommandArguments);
                    context = "Could not save profile " + saveOptions.Name + ": ";
                    ProfileFiles.SaveProfile(saveOptions.Name);
                    Console.WriteLine("Successfully saved profile " + saveOptions.Name);
                }
                else if (options.Command == Options.LoadCommand)
                {
                    var loadOptions = OptionParser.Parse<LoadOptions>(options.CommandArguments);
                    context = "Could not load profile " + loadOptions.Name + ": ";
                    var profile = ProfileFiles.LoadProfile(loadOptions.Name);
                    var extraMessage = profile.MissingAdaptersMessage();
                    if (extraMessage != "")
                        Console.Error.WriteLine("Warning: " + extraMessage);
                    profile.SetCurrent();
                    Console.WriteLine("Successfully loaded profile " + loadOptions.Name);
                }
                else if (options.Command == Options.DetailCommand)
                {
                    var detailOptions = OptionParser.Parse<DetailOptions>(options.CommandArguments);
                    context = "Could not display details for profile " + detailOptions.Name + ": ";
                    var profile = ProfileFiles.LoadRawProfile(detailOptions.Name);
                    var pathComparer = ComparerBuilder.For<NativeMethods.DisplayConfigPathInfo>()
                        .OrderBy(x => profile.ModeInfo[(int) x.sourceInfo.modeInfoIdx].sourceMode.position.x)
                        .ThenBy(x => profile.ModeInfo[(int) x.sourceInfo.modeInfoIdx].sourceMode.position.y);
                    foreach (var adapterPath in profile.PathInfo.OrderBy(x => x, pathComparer).GroupBy(x => x.sourceInfo.adapterId))
                    {
                        var sourceAdapterId = adapterPath.Key;
                        var sourceAdapter = profile.Adapters[sourceAdapterId];
                        Console.WriteLine(sourceAdapter + ":");
                        foreach (var path in adapterPath)
                        {
                            var targetAdapterId = path.targetInfo.adapterId;
                            var targetAdapter = profile.Adapters[targetAdapterId];
                            var target = targetAdapter.Targets[path.targetInfo.id];

                            var line = "  " + target;
                            if (sourceAdapterId != targetAdapterId)
                                line += " (" + targetAdapter + ")";
                            line += " ";

                            var sourceMode = profile.ModeInfo[(int) path.sourceInfo.modeInfoIdx];
                            line += sourceMode.sourceMode.width + "x" + sourceMode.sourceMode.height;
                            line += " offset " + sourceMode.sourceMode.position.x + "," + sourceMode.sourceMode.position.y;
                            line += " @" + Math.Round(path.targetInfo.refreshRate.numerator / (double)path.targetInfo.refreshRate.denominator, 2) + "Hz";
                            Console.WriteLine(line);
                        }
                    }
                }
                else if (options.Command == Options.ValidateCommand)
                {
                    var validateOptions = OptionParser.Parse<ValidateOptions>(options.CommandArguments);
                    context = "Could not validate profile " + validateOptions.Name + ": ";
                    var profile = ProfileFiles.LoadProfile(validateOptions.Name);
                    var ex = profile.Validate();
                    if (ex == null)
                    {
                        Console.WriteLine("Profile " + validateOptions.Name + " passed validation.");
                        return 0;
                    }
                    Console.WriteLine("Profile " + validateOptions.Name + " failed validation:");
                    Console.WriteLine(ex.Message);
                    var extraMessage = profile.MissingAdaptersMessage();
                    if (extraMessage != "")
                        Console.WriteLine(extraMessage);
                    return 1;
                }
                return 0;
            }
            catch (OptionParsingException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Options.Usage();
                return -2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(context + ex.Message);
                return -1;
            }
        }

        private sealed class Options : IOptionArguments
        {
            public const string LoadCommand = "load";
            public const string SaveCommand = "save";
            public const string ListCommand = "list";
            public const string DetailCommand = "detail";
            public const string ValidateCommand = "validate";

            public static readonly string[] Commands = { LoadCommand, SaveCommand, ListCommand, DetailCommand, ValidateCommand };

            [PositionalArgument(0)]
            public string Command { get; set; }

            [PositionalArguments]
            public List<string> CommandArguments { get; } = new List<string>();

            public void Validate()
            {
                if (Command == null)
                    throw new OptionParsingException("No command specified.");
                if (!Commands.Contains(Command))
                    throw new OptionParsingException("Unknown command " + Command);
            }

            public static void Usage()
            {
                Console.Error.WriteLine("Usage: DisplayProfilesCmd COMMAND OPTIONS...");
                Console.Error.WriteLine("  list            Lists all display profiles.");
                Console.Error.WriteLine("  load NAME       Loads a display profile.");
                Console.Error.WriteLine("  save NAME       Saves a display profile.");
                Console.Error.WriteLine("  detail NAME     Describes a display profile.");
                Console.Error.WriteLine("  validate NAME   Tests a display profile.");
                Console.Error.WriteLine("    Returns 0 if it can be applied.");
                Console.Error.WriteLine("    Returns 1 if it cannot be applied (details in STDERR).");
                Console.Error.WriteLine("    Returns negative if some other error (details in STDERR).");
                Console.Error.WriteLine("If the profile name has spaces, enclose it \"in quotes\".");
            }
        }

        private sealed class ListOptions : OptionArgumentsBase
        {
        }

        private sealed class SaveOptions : OptionArgumentsBase
        {
            [PositionalArgument(0)]
            public string Name { get; set; }

            public override void Validate()
            {
                base.Validate();
                if (Name == null)
                    throw new OptionParsingException("No profile name specified for save command.");
            }
        }

        private sealed class LoadOptions : OptionArgumentsBase
        {
            [PositionalArgument(0)]
            public string Name { get; set; }

            public override void Validate()
            {
                base.Validate();
                if (Name == null)
                    throw new OptionParsingException("No profile name specified for load command.");
            }
        }

        private sealed class DetailOptions : OptionArgumentsBase
        {
            [PositionalArgument(0)]
            public string Name { get; set; }

            public override void Validate()
            {
                base.Validate();
                if (Name == null)
                    throw new OptionParsingException("No profile name specified for detail command.");
            }
        }

        private sealed class ValidateOptions : OptionArgumentsBase
        {
            [PositionalArgument(0)]
            public string Name { get; set; }

            public override void Validate()
            {
                base.Validate();
                if (Name == null)
                    throw new OptionParsingException("No profile name specified for validate command.");
            }
        }
    }
}
