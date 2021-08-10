using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator.App
{
    public sealed class Config
    {
        public string? config { get; set; } = null; //this config =)

        public string? output { get; set; } = null;
        public Mode mode { get; set; } = Mode.validate;

        public GraphwizExporter.Settings graphwiz { get; set; } = new GraphwizExporter.Settings();
        public CsharpCodeExporter.Settings c_sharp { get; set; } = new CsharpCodeExporter.Settings();
        public CppCodeExporter.Settings cpp { get; set; } = new CppCodeExporter.Settings();
    }
}
