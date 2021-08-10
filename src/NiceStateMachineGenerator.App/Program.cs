using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NiceStateMachineGenerator.App
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                WriteUsage();
                Environment.Exit(1);
            };
            string sourceFile = args[0];
            Config config = GetConfig(args.Skip(1).ToArray());

            Console.WriteLine("Reading state machine description from " + sourceFile);
            StateMachineDescr stateMachine = Parser.ParseFile(sourceFile);

            Console.WriteLine("Validating state machine");
            Validator.Validate(stateMachine);

            switch (config.mode)
            {
            case Mode.validate:
                Console.WriteLine("Only validation, no file output mode specified, exiting");
                return;
            case Mode.all:
                {
                    string outFile = config.output ?? sourceFile;
                    ExportSingleMode(stateMachine, outFile + Mode.dot.ToExtension(), Mode.dot, config);
                    ExportSingleMode(stateMachine, outFile + Mode.cs.ToExtension(), Mode.cs, config);
                    ExportSingleMode(stateMachine, outFile + Mode.cpp.ToExtension(), Mode.cpp, config);
                }
                break;
            default:
                ExportSingleMode(stateMachine, config.output ?? sourceFile + config.mode.ToExtension(), config.mode, config);
                break;
            }
        }

        private static void ExportSingleMode(StateMachineDescr stateMachine, string outFileName, Mode mode, Config config)
        {
            Console.WriteLine($"Writing output for mode {mode} to {outFileName}");
            switch (mode)
            {
            case Mode.dot:
                GraphwizExporter.Export(stateMachine, outFileName, config.graphwiz);
                break;
            case Mode.cs:
                CsharpCodeExporter.Export(stateMachine, outFileName, config.c_sharp);
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
                { "-m", "mode" },
            };
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
