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
        public static void Export(StateMachineDescr stateMachine, string fileName, bool showEvents)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                Export(stateMachine, writer, showEvents: showEvents);
            }
        }

        public static void Export(StateMachineDescr stateMachine, TextWriter writer, bool showEvents)
        {
            using (IndentedTextWriter indentedWriter = new IndentedTextWriter(writer))
            {
                Export(stateMachine, indentedWriter, showEvents: showEvents);
            }
        }

        public static void Export(StateMachineDescr stateMachine, IndentedTextWriter writer, bool showEvents)
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
                    foreach (string timer in state.StartTimers)
                    {
                        writer.Write("| (+) ");
                        writer.Write(timer);
                    }
                    if (showEvents && state.NeedOnEnterEvent)
                    {
                        writer.Write("| on_enter ");
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
                            WriteEdge(writer, state, edgeDescr, false, showEvents);
                        }
                    }
                    if (state.TimerEdges != null)
                    {
                        foreach (EdgeDescr edgeDescr in state.TimerEdges.Values)
                        {
                            WriteEdge(writer, state, edgeDescr, true, showEvents);
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

        private static void WriteEdge(TextWriter writer, StateDescr sourceState, EdgeDescr edgeDescr, bool isTimer, bool showEvents)
        {
            if (edgeDescr.TargetState == null)
            {
                return;
            };
            string label = edgeDescr.InvokerName;
            if (showEvents && edgeDescr.NeedOnTraverseEvent)
            {
                label += "\n(on_traverse)";
            }
            writer.Write($"{sourceState.Name} -> {edgeDescr.TargetState} [label = \"{label}\"]");
            if (isTimer)
            {
                writer.Write("[style = dashed]");
            }
            writer.WriteLine(";");
        }
    }
}
