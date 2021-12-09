using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    public static class GraphwizExporter
    {
        public sealed class Settings
        {
            public bool ShowStateEnterEvents { get; set; } = false;
            public bool ShowEdgeTraverseEvents { get; set; } = false;
            public bool ShowEdgeTraverseComments { get; set; } = false;
        }

        public static void Export(StateMachineDescr stateMachine, string fileName, Settings settings)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                Export(stateMachine, writer, settings);
            }
        }

        public static void Export(StateMachineDescr stateMachine, TextWriter writer, Settings settings)
        {
            using (IndentedTextWriter indentedWriter = new IndentedTextWriter(writer))
            {
                Export(stateMachine, indentedWriter, settings);
            }
        }

        public static void Export(StateMachineDescr stateMachine, IndentedTextWriter writer, Settings settings)
        {
            writer.WriteLine("digraph {");

            //styles
            {
                ++writer.Indent;
                writer.WriteLine("edge[fontname = \"tahoma\"; fontsize = 8];");
                writer.WriteLine("node[fontname = \"tahoma bold\"; fontsize = 10];");
                --writer.Indent;
            }

            //nodes
            {
                ++writer.Indent;
                foreach (StateDescr state in stateMachine.States.Values)
                {
                    writer.Write(state.Name);

                    writer.Write($" [shape = Mrecord; label = \"{{ {state.Name} ");
                    foreach (string timer in state.StopTimers)
                    {
                        writer.Write("| (-) ");
                        writer.Write(timer);
                    }
                    foreach (TimerStartDescr timerStart in state.StartTimers.Values)
                    {
                        writer.Write("| (+) ");
                        writer.Write(timerStart.TimerName);
                        if (timerStart.Modify != null)
                        {
                            writer.Write("*");
                        }
                    }
                    if (settings.ShowStateEnterEvents && state.NeedOnEnterEvent)
                    {
                        writer.Write("| \\-\\> ");
                        if (state.OnEnterEventComment != null)
                        {
                            writer.Write(state.OnEnterEventComment);
                        }
                    }
                    writer.Write("}\"");
                    if (state.IsFinal)
                    {
                        writer.Write("; style = bold");
                    }
                    writer.WriteLine("];");
                };
                --writer.Indent;
            }

            //edges
            {
                ++writer.Indent;
                foreach (StateDescr state in stateMachine.States.Values)
                {
                    if (state.EventEdges != null)
                    {
                        foreach (EdgeDescr edgeDescr in state.EventEdges.Values)
                        {
                            WriteEdge(writer, state, edgeDescr, settings);
                        }
                    }
                    if (state.TimerEdges != null)
                    {
                        foreach (EdgeDescr edgeDescr in state.TimerEdges.Values)
                        {
                            WriteEdge(writer, state, edgeDescr, settings);
                        }
                    }
                    if (state.NextStateName != null)
                    {
                        writer.WriteLine($"{state.Name} -> {state.NextStateName} [style = bold];");
                    }
                };
                --writer.Indent;
            }

            writer.WriteLine("}");
        }

        private static void WriteEdge(TextWriter writer, StateDescr sourceState, EdgeDescr edgeDescr, Settings settings)
        {
            if (edgeDescr.TargetState == null)
            {
                return;
            };
            string label = edgeDescr.InvokerName;
            if (settings.ShowEdgeTraverseEvents && edgeDescr.OnTraverseEventTypes.Count > 0)
            {
                label += $"\n(on_traverse: {String.Join(", ", edgeDescr.OnTraverseEventTypes.Select(s => s.ToString()).OrderBy(s => s))})";
            };
            if (settings.ShowEdgeTraverseComments && edgeDescr.TraverseEventComment != null)
            {
                label += $"\n/* {edgeDescr.TraverseEventComment} */";
            };
            writer.Write($"{sourceState.Name} -> {edgeDescr.TargetState} [label = \"{label}\"]");
            if (edgeDescr.IsTimer)
            {
                writer.Write("[style = dashed]");
            }
            else if (edgeDescr.GoesBackWithNoEnterEvent)
            {
                writer.Write("[style = dotted]");
            }
            writer.WriteLine(";");
        }
    }
}
