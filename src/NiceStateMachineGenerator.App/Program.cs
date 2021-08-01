using System;
using System.Diagnostics;

namespace NiceStateMachineGenerator.App
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            string fileName = args[0];

            StateMachineDescr stateMachine = Parser.ParseFile(fileName);

            ExportAndConvertToImage(stateMachine, fileName + ".dot", showEvents: false);
            //ExportAndConvertToImage(stateMachine, fileName + ".events.dot", showEvents: true);

            Validator.Validate(stateMachine);

            CsharpCodeExporter.Export(stateMachine, fileName + ".cs", new CsharpCodeExporter.Settings());
        }

        private static void RunGraphwiz(string dotFileName)
        {
            Process gw = Process.Start(@"c:\Program Files\Graphviz\bin\dot.exe", $"-Tpng -O {dotFileName}");
            gw.WaitForExit();
        }

        private static void ExportAndConvertToImage(StateMachineDescr stateMachine, string dotFileName, bool showEvents)
        {
            GraphwizExporter.Export(stateMachine, dotFileName, showEvents: showEvents);
            RunGraphwiz(dotFileName);
        }
    }
}
