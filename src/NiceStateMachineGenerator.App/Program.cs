using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace NiceStateMachineGenerator.App
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    WriteUsage();
                    Environment.Exit(1);
                };

                string sourceFile = args[0];
                Config config = GetConfig(args.Skip(1).ToArray());

                if (config.daemon)
                {
                    RunInDaemonMode(sourceFile, config);
                }
                else
                {
                    bool succeed = TryGenerateStateMachine(sourceFile, config);
                    if (!succeed)
                    {
                        Environment.Exit(2);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Environment.Exit(2);
            }
        }

        private static void RunInDaemonMode(string sourceFile, Config config)
        {
            // Generate at startup before starting to watch for changes
            TryGenerateStateMachine(sourceFile, config);

            string sourceDirectory = Path.GetDirectoryName(sourceFile)!;
            string sourceFilename = Path.GetFileName(sourceFile);
            Console.WriteLine($"Waiting for file {sourceFilename} in {sourceDirectory} to change");
            using (FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(sourceDirectory, sourceFilename))
            {
                fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                fileSystemWatcher.IncludeSubdirectories = false;
                fileSystemWatcher.EnableRaisingEvents = true;

                object lockObject = new object();
                DateTime lastFireTime = DateTime.Now;
                // Events of FileSystemWatcher can trigger multiple times
                // We need to get rid of duplicate events
                // So current hack is to ignore any changes in 200ms from last event
                // Read more at https://weblogs.asp.net/ashben/31773
                fileSystemWatcher.Changed += (object _, FileSystemEventArgs eventArgs) =>
                {
                    lock (lockObject)
                    {
                        // We can get real last write time via File.GetLastWriteTime(eventArgs.FullPath), but DateTime.Now will do fine
                        DateTime fireTime = DateTime.Now;
                        if (fireTime - lastFireTime < TimeSpan.FromMilliseconds(200))
                        {
                            return;
                        }

                        lastFireTime = fireTime;
                        Thread.Sleep(100); // We need to wait for some time because this event triggers faster than filesystem write completes
                        Console.WriteLine("");
                        Console.WriteLine($"File {eventArgs.Name} changed at {fireTime}. Generating state machine");
                        bool succeed = TryGenerateStateMachine(sourceFile, config);
                        if (succeed)
                        {
                            Console.WriteLine($"Rendering Graphviz diagram at {eventArgs.FullPath}");
                            try
                            {
                                Process.Start("dot", $"-Tpng -O {eventArgs.FullPath}.dot");
                                Console.WriteLine("Render complete");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Graphviz render failed: {e}");
                            }
                        }
                    }
                };

                Console.WriteLine("Press any key to stop");
                Console.ReadKey();

                fileSystemWatcher.EnableRaisingEvents = false;
            }
        }

        private static bool TryGenerateStateMachine(string sourceFile, Config config)
        {
            try
            {
                GenerateStateMachine(sourceFile, config);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        private static void GenerateStateMachine(string sourceFile, Config config)
        {
            Console.WriteLine("Reading state machine description from " + sourceFile);
            StateMachineDescr stateMachine = Parser.ParseFile(sourceFile);

            Console.WriteLine("Validating state machine");
            Validator.Validate(stateMachine);
            Console.WriteLine("Validation done");

            switch (config.mode)
            {
                case Mode.validate:
                    Console.WriteLine("No file output mode specified");
                    return;
                case Mode.all:
                    {
                        string outFile = config.output ?? sourceFile;
                        ExportSingleMode(stateMachine, outFile + Mode.dot.ToExtension(), config.out_common, Mode.dot, config);
                        ExportSingleMode(stateMachine, outFile + Mode.cs.ToExtension(), config.out_common, Mode.cs, config);
                        ExportSingleMode(stateMachine, outFile + Mode.cpp.ToExtension(), config.out_common, Mode.cpp, config);
                    }
                    break;
                default:
                    ExportSingleMode(
                        stateMachine,
                        config.output ?? sourceFile + config.mode.ToExtension(),
                        config.out_common,
                        config.mode,
                        config
                    );
                    break;
            }
        }

        private static void ExportSingleMode(StateMachineDescr stateMachine, string outFileName, string? outCommonCodeFileName, Mode mode, Config config)
        {
            Console.WriteLine($"Writing output for mode {mode} to {outFileName} (common code in {(outCommonCodeFileName == null? "the same file" : outCommonCodeFileName)})");
            switch (mode)
            {
            case Mode.dot:
                GraphwizExporter.Export(stateMachine, outFileName, config.graphwiz);
                break;
            case Mode.cs:
                CsharpCodeExporter.Export(stateMachine, outFileName, outCommonCodeFileName, config.c_sharp);
                break;
            case Mode.cpp:
                CppCodeExporter.Export(stateMachine, outFileName, config.cpp);
                break;
            default:
                throw new Exception($"Unexpected output mode '{mode}'. Supported modes are: {String.Join(", ", Enum.GetNames<Mode>())}");
            }
        }

        private static void WriteUsage()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine($"{nameof(NiceStateMachineGenerator)}.{nameof(NiceStateMachineGenerator.App)} <state machine json file> [options]");
            Console.WriteLine($"Possible options:");
            Console.WriteLine($"-c/--config <config.json> : configuration file. Contains settings for all exporters and may contain any of the settings below");
            Console.WriteLine($"-m/--mode <mode> : export mode. One of 'dot', 'cs', 'cpp'.");
            Console.WriteLine($"\t\tUse 'all' ti output all 3 type of files.");
            Console.WriteLine($"\t\tUse 'validate' to suppress file output (default mode). All other modes also do validation.");
            Console.WriteLine($"-o/--output <output file name> : output file name.");
            Console.WriteLine($"\t\tIf not specified then <input file name>.<mode-specific extension> is used.");
            Console.WriteLine($"\t\tIn case of 'all' mode specific extensions are added to <output file name>.");
            Console.WriteLine($"\t\tIngnored for 'validate' mode.");
            Console.WriteLine($"-t/--out_common <output file name for common code> : output file name for common code (e.g. Timer interface definition).");
            Console.WriteLine($"\t\tIf empty, null, or not specified, the code is written to main output file");
            Console.WriteLine($"Also any option for exporter may be overriden via cmdline args. Nesting is specified by ':'");
            Console.WriteLine($"\t\tE.g.: '--c_sharp:ClassName=MyClass' or '--cpp:NamespaceName ns'");
        }

        static Config GetConfig(string[] args)
        { 
            ConfigurationBuilder builder = new ConfigurationBuilder();
            //builder.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));


            Dictionary<string, string> switchMappings = new Dictionary<string, string>()
            {
                { "-c", "config" },
                { "-o", "output" },
                { "-t", "out_common"},
                { "-m", "mode" },
                { "-d", "daemon" },
            };
            ValidateArgs(args, switchMappings);
            builder.AddCommandLine(args, switchMappings);

            //Config config = builder.Build().Get<Config>();
            Config config = new Config();
            builder.Build().Bind(config);

            if (config.config != null)
            {
                string filePath = Path.GetFullPath(config.config);
                Console.WriteLine("Reading config file " + filePath);

                builder = new ConfigurationBuilder();
                builder.AddJsonFile(filePath, optional: false, reloadOnChange: false);
                builder.AddCommandLine(args, switchMappings); //command line after file to allow overrides
                builder.Build().Bind(config);
            };

            return config;
        }
        
        private static void ValidateArgs(string[] args, Dictionary<string, string> validArguments)
        {
            Console.WriteLine("Parsing argument keys");
            string[] keys = args.Where((c,i) => i % 2 == 0).ToArray();

            foreach (string key in keys)
            {
                if (!validArguments.ContainsKey(key))
                {
                    string msg = "Could not parse keys, supported arguments: ";
                    msg += String.Join(", ", AsEnumerable(validArguments));
                    Console.WriteLine(msg);
                    Environment.Exit(1);
                }
            }
            
            Console.WriteLine("Argument keys are correct");
        }

        private static IEnumerable<string> AsEnumerable(Dictionary<string, string> dictionary)
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                yield return $"{pair.Key} ({pair.Value})";
            }
        }

        /*
        static void Main2(string[] args)
        {


            string fileName = args[0];

            StateMachineDescr stateMachine = Parser.ParseFile(fileName);

            ExportAndConvertToImage(stateMachine, fileName + ".dot", new GraphwizExporter.Settings());

            Validator.Validate(stateMachine);

            CsharpCodeExporter.Export(stateMachine, fileName + ".cs", new CsharpCodeExporter.Settings());
            CppCodeExporter.Export(stateMachine, fileName + ".h", new CppCodeExporter.Settings());
        }

        private static void RunGraphwiz(string dotFileName)
        {
            Process gw = Process.Start(@"c:\Program Files\Graphviz\bin\dot.exe", $"-Tpng -O {dotFileName}");
            gw.WaitForExit();
        }

        private static void ExportAndConvertToImage(StateMachineDescr stateMachine, string dotFileName, GraphwizExporter.Settings settings)
        {
            GraphwizExporter.Export(stateMachine, dotFileName, settings);
            RunGraphwiz(dotFileName);
        }
        */
    }
}
