using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator.App
{
    public enum Mode
    {
        dot,
        cs,
        cpp,

        validate, //just validate
        all
    }

    public static class ModeExtensions
    {
        public static string ToExtension(this Mode mode)
        {
            switch (mode)
            {
            case Mode.dot:
                return ".dot";
            case Mode.cs:
                return ".cs";
            case Mode.cpp:
                return ".h";

            case Mode.all:
            case Mode.validate:
                throw new ApplicationException("No extension for mode " + mode);

            default:
                throw new ApplicationException("Unexpected mode " + mode);
            }
        }
    }
}
