using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    internal static class ExportHelper
    {
        internal static string GetClassNameFromFileName(string fileName)
        {
            fileName = Path.GetFileNameWithoutExtension(fileName);
            string className = Path.GetFileNameWithoutExtension(fileName);
            while (className != fileName)
            {
                fileName = className;
                className = Path.GetFileNameWithoutExtension(fileName);
            }
            return className;
        }
    }
}
