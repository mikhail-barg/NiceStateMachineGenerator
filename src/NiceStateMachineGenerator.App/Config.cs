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
        public string? out_common { get; set; } = null;
        public Mode mode { get; set; } = Mode.validate;
        public bool daemon { get; set; } = false;
        public bool run_dot { get; set; } = false;
        public bool run_d2 { get; set; } = false;
        public int d2_theme { get; set; } = 8; //see https://d2lang.com/tour/themes
        public string d2_layout { get; set; } = "elk"; //see https://d2lang.com/tour/layouts

        public GraphwizExporter.Settings graphwiz { get; set; } = new GraphwizExporter.Settings();
        public CsharpCodeExporter.Settings c_sharp { get; set; } = new CsharpCodeExporter.Settings();
        public CppCodeExporter.Settings cpp { get; set; } = new CppCodeExporter.Settings();
        public D2Exporter.Settings d2 { get; set; } = new D2Exporter.Settings();
    }
}
