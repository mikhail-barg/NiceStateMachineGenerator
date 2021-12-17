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

        internal static string ComposeEdgeTraveseCallback(EdgeTraverseCallbackType callbackType, StateDescr source, EdgeDescr edge, out bool eventMayHaveArgs, out bool eventIsFunction)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(edge.IsTimer ? "OnTimerTraverse__" : "OnEventTraverse__");
            switch (callbackType)
            {
            case EdgeTraverseCallbackType.full:
                builder.Append(source.Name);
                builder.Append("__");
                builder.Append(edge.InvokerName);
                builder.Append("__");
                if (edge.Target == null || edge.Targets != null)
                {
                    throw new Exception("Should not happen, check parser!");
                };
                builder.Append(edge.Target.StateName ?? source.Name);   //in case of no_change
                eventMayHaveArgs = !edge.IsTimer;
                eventIsFunction = false;
                break;

            case EdgeTraverseCallbackType.event_only:
                builder.Append(edge.InvokerName);
                eventMayHaveArgs = !edge.IsTimer;
                eventIsFunction = edge.Targets != null;
                break;

            case EdgeTraverseCallbackType.event_and_target:
                builder.Append(edge.InvokerName);
                builder.Append("__");
                if (edge.Target == null || edge.Targets != null)
                {
                    throw new Exception("Should not happen, check parser!");
                };
                builder.Append(edge.Target.StateName ?? source.Name);   //in case of no_change
                eventMayHaveArgs = !edge.IsTimer;
                eventIsFunction = false;
                break;

            case EdgeTraverseCallbackType.source_and_event:
                builder.Append(source.Name);
                builder.Append("__");
                builder.Append(edge.InvokerName);
                eventMayHaveArgs = !edge.IsTimer;
                eventIsFunction = edge.Targets != null;
                break;

            case EdgeTraverseCallbackType.source_and_target:
                builder.Append(source.Name);
                builder.Append("__");
                if (edge.Target == null || edge.Targets != null)
                {
                    throw new Exception("Should not happen, check parser!");
                };
                builder.Append(edge.Target.StateName ?? source.Name);   //in case of no_change
                eventMayHaveArgs = false;
                eventIsFunction = false;
                break;

            case EdgeTraverseCallbackType.source_only:
                builder.Append(source.Name);
                eventMayHaveArgs = false;
                eventIsFunction = edge.Targets != null;
                break;

            case EdgeTraverseCallbackType.target_only:
                if (edge.Target == null || edge.Targets != null)
                {
                    throw new Exception("Should not happen, check parser!");
                };
                builder.Append(edge.Target.StateName ?? source.Name);   //in case of no_change
                eventMayHaveArgs = false;
                eventIsFunction = false;
                break;

            default:
                throw new ApplicationException("Unexpected type " + callbackType);
            }

            return builder.ToString();
        }
    }
}
