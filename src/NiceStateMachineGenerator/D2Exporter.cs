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
            public bool AlwaysUseClassShape { get; set; } = false;
            public string StartStateMark { get; set; } = "(start)";
            public string FinalStateMark { get; set; } = "(final)";
            public bool UseColors { get; set; } = true;

            public bool ShowStateTimersOnOff { get; set; } = true;
            public bool ShowStateEnterEvents { get; set; } = true;
            public bool ShowEdgeTraverseEvents { get; set; } = true;
            public bool ShowEdgeTraverseComments { get; set; } = true;

            internal bool UseClassShapeForStates(StateMachineDescr stateMachine)
            {
                if (this.AlwaysUseClassShape)
                {
                    return true;
                }
                if (this.ShowStateTimersOnOff && stateMachine.Timers.Count > 0)
                {
                    return true;
                }
                if (this.ShowStateEnterEvents && stateMachine.States.Any(s => s.Value.NeedOnEnterEvent))
                {
                    return true;
                }
                return false;
            }
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
            bool useClassShape = settings.UseClassShapeForStates(stateMachine);
            //classes
            {
                writer.WriteLine("classes: {");
                ++writer.Indent;
                {
                    writer.WriteLine("state: {");
                    ++writer.Indent;
                    if (useClassShape)
                    {
                        writer.WriteLine("shape: class");
                    }
                    else
                    {
                        writer.WriteLine("shape: rectangle");
                    }
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
                        if (!useClassShape)
                        {
                            writer.WriteLine("style.double-border: true"); 
                        }
                        else if (!String.IsNullOrWhiteSpace(settings.FinalStateMark))
                        {
                            //double border does not work on `class` shape, so we just change label
                            writer.WriteLine($"label: {state.Name} {settings.FinalStateMark}");
                        }
                    }
                    else if (state.Name == stateMachine.StartState && !String.IsNullOrWhiteSpace(settings.StartStateMark))
                    {
                        //in D2 start state is not always a topmost one, so it's nice to show it explicitly
                        writer.WriteLine($"label: {state.Name} {settings.StartStateMark}");
                    }

                    if (state.Color != null && settings.UseColors)
                    {
                        writer.WriteLine($"style.stroke: \"{state.Color}\"");
                    }

                    if (settings.ShowStateTimersOnOff)
                    {
                        foreach (string timer in state.StopTimers)
                        {
                            writer.WriteLine($"-{timer}");
                        }
                        foreach (TimerStartDescr timerStart in state.StartTimers.Values)
                        {
                            if (timerStart.Modify != null)
                            {
                                writer.WriteLine($"+{timerStart.TimerName}: *");
                            }
                            else
                            {
                                writer.WriteLine($"+{timerStart.TimerName}");
                            }
                        }
                    }
                    if (settings.ShowStateEnterEvents && state.NeedOnEnterEvent)
                    {
                        if (state.OnEnterEventComment != null)
                        {
                            writer.WriteLine($"\\#on_enter(): {state.OnEnterEventComment}");
                        }
                        else
                        {
                            writer.WriteLine("\\#on_enter()");
                        }
                    }

                    --writer.Indent;
                    writer.WriteLine("}");
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

        private static void WriteOnEnterEdge(IndentedTextWriter writer, StateDescr sourceState, EdgeTarget edgeTarget, string additionalComment, Settings settings)
        {
            if (edgeTarget.TargetType == EdgeTargetType.failure)
            {
                return;
            }
            else if (edgeTarget.TargetType == EdgeTargetType.no_change)
            {
                return;
            };

            writer.WriteLine($"{sourceState.Name} -> {edgeTarget.StateName ?? sourceState.Name} {{");
            ++writer.Indent;
            writer.WriteLine("class: event");

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
            writer.WriteLine($"label: |||md");
            writer.WriteLine(label);
            writer.WriteLine($"|||");

            --writer.Indent;
            writer.WriteLine("}");
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

            if (edgeDescr.Color != null && settings.UseColors)
            {
                writer.WriteLine($"style.stroke: \"{edgeDescr.Color}\"");
            }
            
            writer.WriteLine($"style.animated: true");

            writer.WriteLine("label: |||md"); //see https://d2lang.com/tour/text#standalone-text-is-markdown
            ++writer.Indent;
            {
                writer.WriteLine(edgeDescr.InvokerName);

                string? additionalLine = null;
                if (settings.ShowEdgeTraverseEvents && edgeDescr.OnTraverseEventTypes.Count > 0)
                {
                    additionalLine = $"[{String.Join(", ", edgeDescr.OnTraverseEventTypes.Select(s => s.ToString()).OrderBy(s => s))}]";
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
                        if (additionalLine != null)
                        {
                            additionalLine += " -> " + comment;
                        }
                        else
                        {
                            additionalLine = "-> " + comment;
                        }
                    };
                };
                if (additionalLine != null)
                {
                    writer.WriteLine(additionalLine);
                }
            }
            --writer.Indent;
            writer.WriteLine("|||");


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
