﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    public sealed class CsharpCodeExporter
    {
        public sealed class Settings
        {
            public string NamespaceName { get; set; } = "Generated";
            public string ClassName { get; set; } = "StateMachine";
            public List<string>? AdditionalUsings { get; set; }
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
            CsharpCodeExporter exporter = new CsharpCodeExporter(stateMachine, writer, settings);
            exporter.ExportInternal();
        }

        private readonly StateMachineDescr m_stateMachine;
        private readonly IndentedTextWriter m_writer;
        private readonly Settings m_settings;
        private readonly HashSet<string> m_modifiedTimers;

        private CsharpCodeExporter(StateMachineDescr stateMachine, IndentedTextWriter writer, Settings settings)
        {
            this.m_stateMachine = stateMachine;
            this.m_writer = writer;
            this.m_settings = settings;


            this.m_modifiedTimers = this.m_stateMachine.States.Values
                .SelectMany(s => s.StartTimers.Values)
                .Where(t => t.Modify != null)
                .Select(t => t.TimerName)
                .ToHashSet();
        }

        private void ExportInternal()
        {
            this.m_writer.WriteLine($"// generated by {nameof(NiceStateMachineGenerator)} v{Assembly.GetExecutingAssembly().GetName().Version}");

            WriteVerbatimCode(HEADER_CODE);
            if (this.m_settings.AdditionalUsings != null)
            {
                foreach (string ns in this.m_settings.AdditionalUsings)
                {
                    this.m_writer.WriteLine($"using {ns};");
                };
                this.m_writer.WriteLine();
            };

            this.m_writer.WriteLine($"namespace {this.m_settings.NamespaceName}");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine($"public partial class {this.m_settings.ClassName}: IDisposable");
                this.m_writer.WriteLine("{");
                {
                    ++this.m_writer.Indent;
                    WriteVerbatimCode(TIMER_CODE);
                    WriteEnum(STATES_ENUM_NAME, this.m_stateMachine.States.Keys);
                    WriteCallbackEvents();
                    WriteFieldsAndConstructorDestructor();
                    WriteStart();
                    WriteOnTimer();
                    foreach (EventDescr @event in this.m_stateMachine.Events.Values)
                    {
                        WriteProcessEvent(@event);
                    };
                    WriteSetState();
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("}");  //class
                --this.m_writer.Indent;
            };
            this.m_writer.WriteLine("}"); //namespace
        }

        private void WriteSetState()
        {
            this.m_writer.WriteLine($"private void SetState({STATES_ENUM_NAME} state)");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("CheckNotDisposed();");
                this.m_writer.WriteLine("switch (state)");
                this.m_writer.WriteLine("{");
                {
                    foreach (StateDescr state in this.m_stateMachine.States.Values)
                    {
                        this.m_writer.WriteLine($"case {STATES_ENUM_NAME}.{state.Name}:");
                        ++this.m_writer.Indent;
                        {
                            WriteStateEnterCode(state);
                            if (state.NextStateName != null)
                            {
                                this.m_writer.WriteLine($"SetState({STATES_ENUM_NAME}.{state.NextStateName});");
                            }
                            this.m_writer.WriteLine("break;");
                        }
                        this.m_writer.WriteLine();
                        --this.m_writer.Indent;
                    };

                    this.m_writer.WriteLine($"default:");
                    ++this.m_writer.Indent;
                    {
                        this.m_writer.WriteLine("throw new Exception(\"Unexpected state \" + state);");
                    }
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("}");
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteProcessEvent(EventDescr @event)
        {
            this.m_writer.Write($"public void ProcessEvent__{@event.Name}(");
            for (int i = 0; i < @event.Args.Count; ++i)
            {
                KeyValuePair<string, string> arg = @event.Args[i];
                if (i != 0)
                {
                    this.m_writer.Write(", ");
                }
                this.m_writer.Write($"{arg.Value} {arg.Key}");
            }
            this.m_writer.WriteLine(")");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("CheckNotDisposed();");
                this.m_writer.WriteLine("switch (this.CurrentState)");
                this.m_writer.WriteLine("{");
                {
                    foreach (StateDescr state in this.m_stateMachine.States.Values)
                    {
                        if (state.EventEdges != null && state.EventEdges.TryGetValue(@event.Name, out EdgeDescr? edge))
                        {
                            this.m_writer.WriteLine($"case {STATES_ENUM_NAME}.{state.Name}:");
                            ++this.m_writer.Indent;
                            {
                                WriteEdgeTraverse(state, edge);
                                this.m_writer.WriteLine("break;");
                            }
                            this.m_writer.WriteLine();
                            --this.m_writer.Indent;
                        }
                    };

                    this.m_writer.WriteLine($"default:");
                    ++this.m_writer.Indent;
                    {
                        this.m_writer.WriteLine($"throw new Exception(\"Event {@event.Name} is not expected in state \" + this.CurrentState);");
                    }
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("}");
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteOnTimer()
        {
            this.m_writer.WriteLine($"private void OnTimer(ITimer timer)");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("CheckNotDisposed();");
                this.m_writer.WriteLine("switch (this.CurrentState)");
                this.m_writer.WriteLine("{");
                {
                    foreach (StateDescr state in this.m_stateMachine.States.Values)
                    {
                        if (state.TimerEdges != null)
                        {
                            this.m_writer.WriteLine($"case {STATES_ENUM_NAME}.{state.Name}:");
                            ++this.m_writer.Indent;
                            {
                                foreach (EdgeDescr edge in state.TimerEdges.Values)
                                {
                                    this.m_writer.WriteLine($"if (timer == this.{edge.InvokerName})");
                                    this.m_writer.WriteLine("{");
                                    {
                                        ++this.m_writer.Indent;
                                        WriteEdgeTraverse(state, edge);
                                        --this.m_writer.Indent;
                                    }
                                    this.m_writer.WriteLine("}");
                                    this.m_writer.Write("else ");
                                }
                                this.m_writer.WriteLine();
                                this.m_writer.WriteLine("{");
                                {
                                    ++this.m_writer.Indent;
                                    this.m_writer.WriteLine($"throw new Exception(\"Unexpected timer finish in state {state.Name}. Timer was \" + timer);");
                                    --this.m_writer.Indent;
                                }
                                this.m_writer.WriteLine("}");
                                this.m_writer.WriteLine("break;");
                            }
                            this.m_writer.WriteLine();
                            --this.m_writer.Indent;
                        }
                    };

                    this.m_writer.WriteLine($"default:");
                    ++this.m_writer.Indent;
                    {
                        this.m_writer.WriteLine("throw new Exception(\"No timer events expected in state \" + this.CurrentState);");
                    }
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("}");
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteEdgeTraverse(StateDescr state, EdgeDescr edge)
        {
            foreach (EdgeTraverseCallbackType callbackType in edge.OnTraverseEventTypes)
            {
                string callbackName = ComposeEdgeTraveseCallback(callbackType, state, edge, out bool needArgs);
                this.m_writer.Write($"{callbackName}?.Invoke(");
                if (needArgs)
                {
                    EventDescr @event = this.m_stateMachine.Events[edge.InvokerName];
                    for (int i = 0; i < @event.Args.Count; ++i)
                    {
                        KeyValuePair<string, string> arg = @event.Args[i];
                        if (i != 0)
                        {
                            this.m_writer.Write(", ");
                        }
                        this.m_writer.Write(arg.Key);
                    }
                }
                this.m_writer.WriteLine(");");
            };
            if (edge.TargetState != null)
            {
                this.m_writer.WriteLine($"SetState({STATES_ENUM_NAME}.{edge.TargetState});");
            }
        }

        private void WriteStart()
        {
            this.m_writer.WriteLine($"public void Start()");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("CheckNotDisposed();");
                WriteStateEnterCode(this.m_stateMachine.States[this.m_stateMachine.StartState]);
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteStateEnterCode(StateDescr state)
        {
            this.m_writer.WriteLine($"this.CurrentState = {STATES_ENUM_NAME}.{state.Name};");

            foreach (string timer in state.StopTimers)
            {
                this.m_writer.WriteLine($"this.{timer}.Stop();");
            }
            foreach (TimerStartDescr timerStart in state.StartTimers.Values)
            {
                if (this.m_modifiedTimers.Contains(timerStart.TimerName))
                {
                    string delayVariable = ComposeTimerDelayVariable(timerStart.TimerName);
                    if (timerStart.Modify != null)
                    {
                        if (timerStart.Modify.multiplier != null)
                        {
                            this.m_writer.WriteLine($"this.{delayVariable} *= {timerStart.Modify.multiplier.Value.ToString(CultureInfo.InvariantCulture)};");
                        };
                        if (timerStart.Modify.increment != null)
                        {
                            this.m_writer.WriteLine($"this.{delayVariable} += {timerStart.Modify.increment.Value.ToString(CultureInfo.InvariantCulture)};");
                        };
                        if (timerStart.Modify.min != null)
                        {
                            this.m_writer.WriteLine($"if (this.{delayVariable} < {timerStart.Modify.min.Value.ToString(CultureInfo.InvariantCulture)}) {{ this.{delayVariable} = {timerStart.Modify.min.Value.ToString(CultureInfo.InvariantCulture)}; }}");
                        };
                        if (timerStart.Modify.max != null)
                        {
                            this.m_writer.WriteLine($"if (this.{delayVariable} > {timerStart.Modify.max.Value.ToString(CultureInfo.InvariantCulture)}) {{ this.{delayVariable} = {timerStart.Modify.max.Value.ToString(CultureInfo.InvariantCulture)}; }}");
                        };
                    };
                    this.m_writer.WriteLine($"this.{timerStart.TimerName}.StartOrReset({delayVariable});");
                }
                else
                {
                    TimerDescr descr = this.m_stateMachine.Timers[timerStart.TimerName];
                    this.m_writer.WriteLine($"this.{timerStart.TimerName}.StartOrReset({descr.IntervalSeconds.ToString(CultureInfo.InvariantCulture)});");
                }
            }

            if (state.NeedOnEnterEvent)
            {
                string callbackName = ComposeStateEnterCallback(state);
                this.m_writer.WriteLine($"{callbackName}?.Invoke();");
            }
        }

        private string ComposeTimerDelayVariable(string timerName)
        {
            return $"m_{timerName}_delay";
        }

        private void WriteFieldsAndConstructorDestructor()
        {
            this.m_writer.WriteLine($"private bool m_isDisposed = false;");

            foreach (string timer in this.m_stateMachine.Timers.Keys)
            {
                this.m_writer.WriteLine($"private readonly ITimer {timer};");
            }
            foreach (string timer in this.m_modifiedTimers)
            {
                TimerDescr descr = this.m_stateMachine.Timers[timer];
                this.m_writer.WriteLine($"private double {ComposeTimerDelayVariable(timer)} = {descr.IntervalSeconds.ToString(CultureInfo.InvariantCulture)};");
            }

            this.m_writer.WriteLine();
            this.m_writer.WriteLine($"public {STATES_ENUM_NAME} CurrentState {{ get; private set; }} = {STATES_ENUM_NAME}.{this.m_stateMachine.StartState};");
            this.m_writer.WriteLine();
            this.m_writer.WriteLine($"public {this.m_settings.ClassName}(ITimerFactory timerFactory)");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                foreach (string timer in this.m_stateMachine.Timers.Keys)
                {
                    this.m_writer.WriteLine($"this.{timer} = timerFactory.CreateTimer(\"{timer}\", this.OnTimer);");
                }
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();

            this.m_writer.WriteLine($"public void Dispose()");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("if (!this.m_isDisposed)");
                this.m_writer.WriteLine("{");
                {
                    ++this.m_writer.Indent;
                    foreach (string timer in this.m_stateMachine.Timers.Keys)
                    {
                        this.m_writer.WriteLine($"this.{timer}.Dispose();");
                    }
                    this.m_writer.WriteLine($"this.m_isDisposed = true;");
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("}");
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();

            this.m_writer.WriteLine($"private void CheckNotDisposed()");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("if (this.m_isDisposed)");
                this.m_writer.WriteLine("{");
                {
                    ++this.m_writer.Indent;
                    this.m_writer.WriteLine($"throw new ObjectDisposedException(\"{this.m_settings.ClassName}\");");
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("}");
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteEnum(string enumName, IEnumerable<string> values)
        {
            this.m_writer.WriteLine($"public enum {enumName}");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                foreach (string @event in values)
                {
                    this.m_writer.Write(@event);
                    this.m_writer.WriteLine(",");
                };
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine("");
        }

        private static string ComposeEdgeTraveseCallback(EdgeTraverseCallbackType callbackType, StateDescr source, EdgeDescr edge, out bool eventNeedArgs)
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
                builder.Append(edge.TargetState);
                eventNeedArgs = !edge.IsTimer;
                break;

            case EdgeTraverseCallbackType.event_only:
                builder.Append(edge.InvokerName);
                eventNeedArgs = !edge.IsTimer;
                break;

            case EdgeTraverseCallbackType.event_and_target:
                builder.Append(edge.InvokerName);
                builder.Append("__");
                builder.Append(edge.TargetState);
                eventNeedArgs = !edge.IsTimer;
                break;

            case EdgeTraverseCallbackType.source_and_event:
                builder.Append(source.Name);
                builder.Append("__");
                builder.Append(edge.InvokerName);
                eventNeedArgs = !edge.IsTimer;
                break;

            case EdgeTraverseCallbackType.source_and_target:
                builder.Append(source.Name);
                builder.Append("__");
                builder.Append(edge.TargetState);
                eventNeedArgs = false;
                break;

            case EdgeTraverseCallbackType.source_only:
                builder.Append(source.Name);
                eventNeedArgs = false;
                break;

            case EdgeTraverseCallbackType.target_only:
                builder.Append(edge.TargetState);
                eventNeedArgs = false;
                break;

            default:
                throw new ApplicationException("Unexpected type " + callbackType);
            }

            return builder.ToString();
        }

        private static string ComposeStateEnterCallback(StateDescr state)
        {
            return $"OnStateEnter__{state.Name}";
        }

        private void WriteCommentIfSpecified(string? comment)
        {
            if (comment != null)
            {
                this.m_writer.Write("/*");
                this.m_writer.Write(comment); //could be an injection )
                this.m_writer.WriteLine("*/");
            }
        }

        private void WriteCallbackEvents()
        {
            foreach (StateDescr state in this.m_stateMachine.States.Values)
            {
                if (state.NeedOnEnterEvent)
                {
                    string callbackName = ComposeStateEnterCallback(state);
                    WriteCommentIfSpecified(state.OnEnterEventComment);
                    this.m_writer.WriteLine($"public event Action {callbackName};");
                }
            }
            this.m_writer.WriteLine();

            HashSet<string> declaredEventCallbacks = new HashSet<string>();
            foreach (StateDescr state in this.m_stateMachine.States.Values)
            {
                if (state.EventEdges != null)
                {
                    foreach (EdgeDescr edge in state.EventEdges.Values)
                    {
                        foreach (EdgeTraverseCallbackType callbackType in edge.OnTraverseEventTypes)
                        {
                            string callbackName = ComposeEdgeTraveseCallback(callbackType, state, edge, out bool needArgs);
                            if (!declaredEventCallbacks.Add(callbackName))
                            {
                                continue;
                            }

                            WriteCommentIfSpecified(edge.TraverseEventComment);
                            
                            EventDescr @event = this.m_stateMachine.Events[edge.InvokerName];
                            if (!needArgs || @event.Args.Count == 0)
                            {
                                this.m_writer.WriteLine($"public event Action {callbackName};");
                            }
                            else
                            {
                                this.m_writer.Write($"public event Action<");
                                for (int i = 0; i < @event.Args.Count; ++i)
                                {
                                    KeyValuePair<string, string> arg = @event.Args[i];
                                    if (i != 0)
                                    {
                                        this.m_writer.Write(", ");
                                    };
                                    this.m_writer.Write($"{arg.Value}");
                                }
                                this.m_writer.WriteLine($"> {callbackName}; ");
                            }
                        }
                    }
                }
                if (state.TimerEdges != null)
                {
                    foreach (EdgeDescr edge in state.TimerEdges.Values)
                    {
                        foreach (EdgeTraverseCallbackType callbackType in edge.OnTraverseEventTypes)
                        {
                            string callbackName = ComposeEdgeTraveseCallback(callbackType, state, edge, out _);
                            if (!declaredEventCallbacks.Add(callbackName))
                            {
                                continue;
                            }
                            WriteCommentIfSpecified(edge.TraverseEventComment);
                            this.m_writer.WriteLine($"public event Action {callbackName};");
                        }
                    }
                }
            }
            this.m_writer.WriteLine();
        }

        private static Regex s_splitRegex = new Regex(@"\r?\n", RegexOptions.Compiled);
        private void WriteVerbatimCode(string code)
        {
            foreach (string line in s_splitRegex.Split(code))
            {
                this.m_writer.WriteLine(line);
            }
        }

        private const string STATES_ENUM_NAME = "States";

        private const string HEADER_CODE =
@"
using System;
";

        private const string TIMER_CODE =
@"
public delegate void TimerFiredCallback(ITimer timer);

public interface ITimer: IDisposable
{
    void StartOrReset(double timerDelaySeconds);
    void Stop();
}

public interface ITimerFactory
{
    ITimer CreateTimer(string timerName, TimerFiredCallback callback);
}

";

    }
}
