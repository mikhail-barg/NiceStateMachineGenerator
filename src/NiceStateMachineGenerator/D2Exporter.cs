using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    //see https://github.com/terrastruct/d2
    public static class D2Exporter
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
            //classes
            {
                writer.WriteLine("classes: {");
                ++writer.Indent;
                {
                    writer.WriteLine("state: {");
                    ++writer.Indent;
                    writer.WriteLine("shape: rectangle");
                    writer.WriteLine("style.border-radius: 10");
                    --writer.Indent;
                    writer.WriteLine("}");
                }
                {
                    writer.WriteLine("event: {");
                    ++writer.Indent;
                    --writer.Indent;
                    writer.WriteLine("}");
                }
                {
                    writer.WriteLine("timer: {");
                    ++writer.Indent;
                    writer.WriteLine("style.stroke-dash: 5");
                    --writer.Indent;
                    writer.WriteLine("}");
                }
                {
                    writer.WriteLine("no-change: {");
                    ++writer.Indent;
                    writer.WriteLine("style.stroke-dash: 1");
                    --writer.Indent;
                    writer.WriteLine("}");
                }
                {
                    writer.WriteLine("next: {");
                    ++writer.Indent;
                    writer.WriteLine("style.stroke-width: 4");
                    --writer.Indent;
                    writer.WriteLine("}");
                }
                --writer.Indent;
                writer.WriteLine("}");
            }

            writer.WriteLine();

            //nodes
            {
                foreach (StateDescr state in stateMachine.States.Values)
                {
                    writer.WriteLine($"{state.Name}: {{");
                    ++writer.Indent;
                    writer.WriteLine($"class: state");

                    if (state.IsFinal)
                    {
                        writer.WriteLine("style.double-border: true");
                    }
                    if (state.Color != null)
                    {
                        writer.WriteLine($"style.stroke: {state.Color}");
                    }

                    --writer.Indent;
                    writer.WriteLine("}");

                    /*TODO:
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
                    */
                    --writer.Indent;
                };
                
            }

            //edges
            {
                foreach (StateDescr state in stateMachine.States.Values)
                {
                    if (state.OnEnterEventAlluxTargets != null)
                    {
                        foreach ((string comment, EdgeTarget target) in state.OnEnterEventAlluxTargets)
                        {
                            WriteOnEnterEdge(writer, state, target, comment, settings);
                        }
                    };
                    if (state.EventEdges != null)
                    {
                        foreach (EdgeDescr edgeDescr in state.EventEdges.Values)
                        {
                            WriteEdge(writer, state, edgeDescr, settings);
                        }
                    };
                    if (state.TimerEdges != null)
                    {
                        foreach (EdgeDescr edgeDescr in state.TimerEdges.Values)
                        {
                            WriteEdge(writer, state, edgeDescr, settings);
                        }
                    };
                    if (state.NextStateName != null)
                    {
                        writer.WriteLine($"{state.Name} -> {state.NextStateName} {{");
                        ++writer.Indent;
                        writer.WriteLine("class: next");
                        --writer.Indent;
                        writer.WriteLine("}");
                    }
                };
            }

        }

        private static void WriteOnEnterEdge(TextWriter writer, StateDescr sourceState, EdgeTarget edgeTarget, string additionalComment, Settings settings)
        {
            //TODO:
            /*
            if (edgeTarget.TargetType == EdgeTargetType.failure)
            {
                return;
            }
            else if (edgeTarget.TargetType == EdgeTargetType.no_change)
            {
                return;
            };

            string label = "[on_enter]";
            if (settings.ShowEdgeTraverseComments)
            {
                string? comment;
                if (sourceState.OnEnterEventComment != null && additionalComment != null)
                {
                    comment = $"{sourceState.OnEnterEventComment} -> {additionalComment}";
                }
                else if (sourceState.OnEnterEventComment != null)
                {
                    comment = sourceState.OnEnterEventComment;
                }
                else if (additionalComment != null)
                {
                    comment = additionalComment;
                }
                else
                {
                    comment = null;
                };
                if (comment != null)
                {
                    label += $" -> {comment}";
                };
            };
            writer.Write($"{sourceState.Name} -> {edgeTarget.StateName ?? sourceState.Name} [label = \"{label}\"]");
            if (edgeTarget.TargetType == EdgeTargetType.no_change)
            {
                writer.Write("[style = dotted]");
            }
            writer.WriteLine(";");
            */
        }

        private static void WriteEdge(IndentedTextWriter writer, StateDescr sourceState, EdgeDescr edgeDescr, EdgeTarget edgeTarget, string? additionalComment, Settings settings)
        {
            if (edgeTarget.TargetType == EdgeTargetType.failure)
            {
                return;
            }
            else if (edgeTarget.TargetType == EdgeTargetType.no_change && edgeDescr.OnTraverseEventTypes.Count == 0)
            {
                return;
            };

            writer.WriteLine($"{sourceState.Name} -> {edgeTarget.StateName ?? sourceState.Name} {{");
            ++writer.Indent;
            if (edgeDescr.IsTimer)
            {
                writer.WriteLine("class: timer");
            }
            else if (edgeTarget.TargetType == EdgeTargetType.no_change)
            {
                writer.WriteLine("class: no-change");
            }
            else
            {
                writer.WriteLine("class: event");
            }

            if (edgeDescr.Color != null)
            {
                writer.WriteLine($"stroke: {edgeDescr.Color}");
            }


            string label = edgeDescr.InvokerName;
            /*TODO:
            if (settings.ShowEdgeTraverseEvents && edgeDescr.OnTraverseEventTypes.Count > 0)
            {
                label += $"\n[{String.Join(", ", edgeDescr.OnTraverseEventTypes.Select(s => s.ToString()).OrderBy(s => s))}]";
            };
            if (settings.ShowEdgeTraverseComments)
            {
                string? comment;
                if (edgeDescr.TraverseEventComment != null && additionalComment != null)
                {
                    comment = $"{edgeDescr.TraverseEventComment} -> {additionalComment}";
                }
                else if (edgeDescr.TraverseEventComment != null)
                {
                    comment = edgeDescr.TraverseEventComment;
                }
                else if (additionalComment != null)
                {
                    comment = additionalComment;
                }
                else
                {
                    comment = null;
                };
                if (comment != null)
                {
                    label += $" -> {comment}";
                };
            };
            */
            writer.WriteLine($"label: {label}");


            --writer.Indent;
            writer.WriteLine("}");
        }


        private static void WriteEdge(IndentedTextWriter writer, StateDescr sourceState, EdgeDescr edgeDescr, Settings settings)
        {
            if (edgeDescr.Target != null)
            {
                WriteEdge(writer, sourceState, edgeDescr, edgeDescr.Target, null, settings);
            }
            else if (edgeDescr.Targets != null)
            {
                foreach (KeyValuePair<string, EdgeTarget> subEdge in edgeDescr.Targets)
                {
                    WriteEdge(writer, sourceState, edgeDescr, subEdge.Value, subEdge.Key, settings);
                }
            };
        }
    }
}
