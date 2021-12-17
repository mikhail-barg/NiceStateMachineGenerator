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
    public sealed class CppCodeExporter
    {
        public sealed class Settings
        {
            public string NamespaceName { get; set; } = "generated";
            public string? ClassName { get; set; } = null; //generate from file name
            public List<string>? AdditionalIncludes { get; set; } = null;
        }

        public static void Export(StateMachineDescr stateMachine, string headerFile, Settings settings)
        {
            using (StreamWriter writer = new StreamWriter(headerFile))
            {
                if (String.IsNullOrEmpty(settings.ClassName))
                {
                    settings.ClassName = ExportHelper.GetClassNameFromFileName(headerFile);
                }
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

        public static void Export(StateMachineDescr stateMachine, IndentedTextWriter header, Settings settings)
        {
            CppCodeExporter exporter = new CppCodeExporter(stateMachine, header, settings);
            exporter.ExportInternal();
        }

        private readonly StateMachineDescr m_stateMachine;
        private readonly IndentedTextWriter m_writer; 
        private readonly Settings m_settings;
        private readonly HashSet<string> m_modifiedTimers;

        private CppCodeExporter(StateMachineDescr stateMachine, IndentedTextWriter headerWriter, Settings settings)
        {
            this.m_stateMachine = stateMachine;
            this.m_writer = headerWriter;
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

            WriteVerbatimCode(HEADER_PREAMBLE_CODE);
            if (this.m_settings.AdditionalIncludes != null)
            {
                foreach (string include in this.m_settings.AdditionalIncludes)
                {
                    this.m_writer.WriteLine($"#include {include}");
                };
                this.m_writer.WriteLine();
            };

            this.m_writer.WriteLine($"namespace {this.m_settings.NamespaceName}");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;

                WriteVerbatimCode(TIMER_CODE);

                this.m_writer.WriteLine($"template <Timer T>");
                this.m_writer.WriteLine($"class {this.m_settings.ClassName}");
                this.m_writer.WriteLine("{");
                {
                    this.m_writer.WriteLine("public:");
                    ++this.m_writer.Indent;
                    WriteEnum(STATES_ENUM_NAME, this.m_stateMachine.States.Keys);
                    WriteCallbackEvents();
                    --this.m_writer.Indent;

                    this.m_writer.WriteLine("private:");
                    ++this.m_writer.Indent;
                    WriteFields();
                    --this.m_writer.Indent;

                    this.m_writer.WriteLine("public:");
                    ++this.m_writer.Indent;
                    WriteConstructorDestructorStateGetter();
                    WriteStart();
                    foreach (EventDescr @event in this.m_stateMachine.Events.Values)
                    {
                        WriteProcessEvent(@event);
                    };
                    --this.m_writer.Indent;

                    this.m_writer.WriteLine("private:");
                    ++this.m_writer.Indent;
                    WriteOnTimer();
                    WriteSetState();
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("};");  //class
                --this.m_writer.Indent;
            };
            this.m_writer.WriteLine("}"); //namespace
        }

        private void WriteSetState()
        {
            this.m_writer.WriteLine($"void SetState({STATES_ENUM_NAME} state)");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("switch (state)");
                this.m_writer.WriteLine("{");
                {
                    foreach (StateDescr state in this.m_stateMachine.States.Values)
                    {
                        this.m_writer.WriteLine($"case {STATES_ENUM_NAME}::{state.Name}:");
                        ++this.m_writer.Indent;
                        {
                            WriteStateEnterCode(state);
                            if (state.NextStateName != null)
                            {
                                this.m_writer.WriteLine($"SetState({STATES_ENUM_NAME}::{state.NextStateName});");
                            }
                            this.m_writer.WriteLine("break;");
                        }
                        this.m_writer.WriteLine();
                        --this.m_writer.Indent;
                    };

                    this.m_writer.WriteLine($"default:");
                    ++this.m_writer.Indent;
                    {
                        this.m_writer.WriteLine("throw std::runtime_error(\"Unexpected state \" /* + state*/);");
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
            this.m_writer.Write($"void ProcessEvent__{@event.Name}(");
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
                this.m_writer.WriteLine("switch (m_currentState)");
                this.m_writer.WriteLine("{");
                {
                    foreach (StateDescr state in this.m_stateMachine.States.Values)
                    {
                        if (state.EventEdges != null && state.EventEdges.TryGetValue(@event.Name, out EdgeDescr? edge))
                        {
                            this.m_writer.WriteLine($"case {STATES_ENUM_NAME}::{state.Name}:");
                            ++this.m_writer.Indent;
                            {
                                WriteEdgeTraverse(state, edge, out bool throwsException);
                                if (!throwsException)
                                {
                                    this.m_writer.WriteLine("break;");
                                }
                            }
                            this.m_writer.WriteLine();
                            --this.m_writer.Indent;
                        }
                    };

                    this.m_writer.WriteLine($"default:");
                    ++this.m_writer.Indent;
                    {
                        this.m_writer.WriteLine($"throw std::runtime_error(\"Event {@event.Name} is not expected in current state \" /* + this.CurrentState*/);");
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
            this.m_writer.WriteLine($"void OnTimer(T* timer)");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("switch (m_currentState)");
                this.m_writer.WriteLine("{");
                {
                    foreach (StateDescr state in this.m_stateMachine.States.Values)
                    {
                        if (state.TimerEdges != null)
                        {
                            this.m_writer.WriteLine($"case {STATES_ENUM_NAME}::{state.Name}:");
                            ++this.m_writer.Indent;
                            {
                                foreach (EdgeDescr edge in state.TimerEdges.Values)
                                {
                                    this.m_writer.WriteLine($"if (timer == {edge.InvokerName})");
                                    this.m_writer.WriteLine("{");
                                    {
                                        ++this.m_writer.Indent;
                                        WriteEdgeTraverse(state, edge, out _);
                                        --this.m_writer.Indent;
                                    }
                                    this.m_writer.WriteLine("}");
                                    this.m_writer.Write("else ");
                                }
                                this.m_writer.WriteLine();
                                this.m_writer.WriteLine("{");
                                {
                                    ++this.m_writer.Indent;
                                    this.m_writer.WriteLine($"throw std::runtime_error(\"Unexpected timer finish in state {state.Name}\");");
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
                        this.m_writer.WriteLine("throw std::runtime_error(\"No timer events expected in current state\" /*+ this.CurrentState*/);");
                    }
                    --this.m_writer.Indent;
                }
                this.m_writer.WriteLine("}");
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteEdgeTraverse(StateDescr state, EdgeDescr edge, out bool throwsException)
        {
            foreach (EdgeTraverseCallbackType callbackType in edge.OnTraverseEventTypes)
            {
                string callbackName = ExportHelper.ComposeEdgeTraveseCallbackName(callbackType, state, edge, out bool needArgs, out bool isFunction);

                if (!isFunction)
                {
                    //regular callback code
                    this.m_writer.Write($"if ({callbackName}) {{ {callbackName}(");
                    WriteEdgeTraverseCallbackArgs(needArgs, edge);
                    this.m_writer.WriteLine("); }");
                }
                else
                {
                    //functions are for choosing next state based on result
                    if (edge.Targets == null)
                    {
                        throw new Exception("Should not happen! Check parser!");
                    };

                    this.m_writer.WriteLine("{"); //visibility guard
                    ++this.m_writer.Indent;
                    {
                        this.m_writer.Write($"std::optional<{STATES_ENUM_NAME}> nextState = {callbackName}(");
                        WriteEdgeTraverseCallbackArgs(needArgs, edge);
                        this.m_writer.WriteLine(");");

                        this.m_writer.WriteLine($"if (nextState)");
                        this.m_writer.WriteLine("{");
                        ++this.m_writer.Indent;
                        {
                            this.m_writer.WriteLine($"switch (*nextState)");
                            this.m_writer.WriteLine("{");
                            {
                                foreach (KeyValuePair<string, EdgeTarget> subEdge in edge.Targets)
                                {
                                    if (subEdge.Value.TargetType == EdgeTargetType.state)
                                    {
                                        this.m_writer.WriteLine($"case {STATES_ENUM_NAME}::{subEdge.Value.StateName}:");
                                        ++this.m_writer.Indent;
                                        this.m_writer.WriteLine($"/*{subEdge.Key}*/");
                                        this.m_writer.WriteLine($"SetState({STATES_ENUM_NAME}::{subEdge.Value.StateName});");
                                        this.m_writer.WriteLine($"break;");
                                        --this.m_writer.Indent;
                                    }
                                };
                                this.m_writer.WriteLine($"default:");
                                ++this.m_writer.Indent;
                                this.m_writer.WriteLine("throw std::runtime_error(\"Unexpected target state was chosen by callback function " + callbackName + "\");");
                                --this.m_writer.Indent;
                            }
                            this.m_writer.WriteLine("}"); //switch
                        }
                        --this.m_writer.Indent;
                        this.m_writer.WriteLine("}"); //if has value

                    }
                    --this.m_writer.Indent;
                    this.m_writer.WriteLine("}"); //visibility guard
                }
            };

            throwsException = false;
            //only happens in case of no function
            if (edge.Target != null)
            {
                switch (edge.Target.TargetType)
                {
                case EdgeTargetType.state:
                    this.m_writer.WriteLine($"SetState({STATES_ENUM_NAME}::{edge.Target.StateName});");
                    break;
                case EdgeTargetType.failure:
                    this.m_writer.WriteLine($"throw std::runtime_error(\"Event {edge.InvokerName} is forbidden in current state\");");
                    throwsException = true;
                    break;
                case EdgeTargetType.no_change:
                    //notnhing to do
                    break;
                default:
                    throw new Exception("Unexpected type " + edge.Target.TargetType);
                }
            }
        }

        private void WriteEdgeTraverseCallbackArgs(bool needArgs, EdgeDescr edge)
        {
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
        }

        private void WriteStart()
        {
            this.m_writer.WriteLine($"void Start()");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                WriteStateEnterCode(this.m_stateMachine.States[this.m_stateMachine.StartState]);
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteStateEnterCode(StateDescr state)
        {
            this.m_writer.WriteLine($"m_currentState = {STATES_ENUM_NAME}::{state.Name};");

            foreach (string timer in state.StopTimers)
            {
                this.m_writer.WriteLine($"{timer}->Stop();");
            }
            foreach (TimerStartDescr timerStart in state.StartTimers.Values)
            {
                if (this.m_modifiedTimers.Contains(timerStart.TimerName))
                {
                    string delayVariable = ComposeTimerDelayVariable(timerStart.TimerName);
                    if (timerStart.Modify != null)
                    {
                        if (timerStart.Modify.set != null)
                        {
                            this.m_writer.WriteLine($"{delayVariable} = {timerStart.Modify.set.Value.ToString(CultureInfo.InvariantCulture)};");
                        }
                        else
                        {
                            if (timerStart.Modify.multiplier != null)
                            {
                                this.m_writer.WriteLine($"{delayVariable} *= {timerStart.Modify.multiplier.Value.ToString(CultureInfo.InvariantCulture)};");
                            };
                            if (timerStart.Modify.increment != null)
                            {
                                this.m_writer.WriteLine($"{delayVariable} += {timerStart.Modify.increment.Value.ToString(CultureInfo.InvariantCulture)};");
                            };
                            if (timerStart.Modify.min != null)
                            {
                                this.m_writer.WriteLine($"if ({delayVariable} < {timerStart.Modify.min.Value.ToString(CultureInfo.InvariantCulture)}) {{ {delayVariable} = {timerStart.Modify.min.Value.ToString(CultureInfo.InvariantCulture)}; }}");
                            };
                            if (timerStart.Modify.max != null)
                            {
                                this.m_writer.WriteLine($"if ({delayVariable} > {timerStart.Modify.max.Value.ToString(CultureInfo.InvariantCulture)}) {{ {delayVariable} = {timerStart.Modify.max.Value.ToString(CultureInfo.InvariantCulture)}; }}");
                            };
                        };
                    };
                    this.m_writer.WriteLine($"{timerStart.TimerName}->StartOrReset({delayVariable});");
                }
                else
                {
                    TimerDescr descr = this.m_stateMachine.Timers[timerStart.TimerName];
                    this.m_writer.WriteLine($"{timerStart.TimerName}->StartOrReset({descr.IntervalSeconds.ToString(CultureInfo.InvariantCulture)});");
                }
            }

            if (state.NeedOnEnterEvent)
            {
                string callbackName = ComposeStateEnterCallback(state);
                this.m_writer.WriteLine($"if ({callbackName}) {{ {callbackName}(); }}");
            }
        }

        private string ComposeTimerDelayVariable(string timerName)
        {
            return $"m_{timerName}_delay";
        }

        private void WriteFields()
        {
            this.m_writer.WriteLine($"{STATES_ENUM_NAME} m_currentState = {STATES_ENUM_NAME}::{this.m_stateMachine.StartState};");

            foreach (string timer in this.m_stateMachine.Timers.Keys)
            {
                this.m_writer.WriteLine($"T* {timer};");
            }
            foreach (string timer in this.m_modifiedTimers)
            {
                TimerDescr descr = this.m_stateMachine.Timers[timer];
                this.m_writer.WriteLine($"double {ComposeTimerDelayVariable(timer)} = {descr.IntervalSeconds.ToString(CultureInfo.InvariantCulture)};");
            }
            this.m_writer.WriteLine();
        }

        private void WriteConstructorDestructorStateGetter()
        {
            this.m_writer.WriteLine($"{this.m_settings.ClassName}(TimerFactory<T> timerFactory)");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;

                if (this.m_stateMachine.Timers.Count > 0)
                {
                    this.m_writer.WriteLine($"TimerFiredCallback<T> timerCallback = std::bind(&{this.m_settings.ClassName}::OnTimer, this, std::placeholders::_1);");
                    foreach (string timer in this.m_stateMachine.Timers.Keys)
                    {
                        this.m_writer.WriteLine($"{timer} = timerFactory(\"{timer}\", timerCallback);");
                    }
                };

                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();

            this.m_writer.WriteLine($"~{this.m_settings.ClassName}()");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                foreach (string timer in this.m_stateMachine.Timers.Keys)
                {
                    this.m_writer.WriteLine($"delete {timer};");
                }
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();

            this.m_writer.WriteLine($"{STATES_ENUM_NAME} GetCurrentState()");
            this.m_writer.WriteLine("{");
            {
                ++this.m_writer.Indent;
                this.m_writer.WriteLine("return m_currentState;");
                --this.m_writer.Indent;
            }
            this.m_writer.WriteLine("}");
            this.m_writer.WriteLine();
        }

        private void WriteEnum(string enumName, IEnumerable<string> values)
        {
            this.m_writer.WriteLine($"enum class {enumName}");
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
            this.m_writer.WriteLine("};");
            this.m_writer.WriteLine("");
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
                    this.m_writer.WriteLine($"std::function<void()> {callbackName};");
                }
            }
            this.m_writer.WriteLine();

            Dictionary<string, bool> declaredEventCallbacks = new Dictionary<string, bool>();    //callback name -> is function callback
            foreach (StateDescr state in this.m_stateMachine.States.Values)
            {
                if (state.EventEdges != null)
                {
                    foreach (EdgeDescr edge in state.EventEdges.Values)
                    {
                        foreach (EdgeTraverseCallbackType callbackType in edge.OnTraverseEventTypes)
                        {
                            WriteCallbackEvent(state, edge, callbackType, declaredEventCallbacks);
                        }
                    }
                }
                if (state.TimerEdges != null)
                {
                    foreach (EdgeDescr edge in state.TimerEdges.Values)
                    {
                        foreach (EdgeTraverseCallbackType callbackType in edge.OnTraverseEventTypes)
                        {
                            WriteCallbackEvent(state, edge, callbackType, declaredEventCallbacks);
                        }
                    }
                }
            }
            this.m_writer.WriteLine();
        }

        private void WriteCallbackEvent(StateDescr state, EdgeDescr edge, EdgeTraverseCallbackType callbackType, Dictionary<string, bool> declaredEventCallbacks)
        {
            string callbackName = ExportHelper.ComposeEdgeTraveseCallbackName(callbackType, state, edge, out bool needArgs, out bool isFunction);
            if (declaredEventCallbacks.TryGetValue(callbackName, out bool oldCallbackIsFunction))
            {
                if (oldCallbackIsFunction != isFunction)
                {
                    throw new Exception("should not happen! check validator!");
                }
                return;
            }
            else
            {
                declaredEventCallbacks.Add(callbackName, isFunction);
            };
            WriteCommentIfSpecified(edge.TraverseEventComment);
            EventDescr @event = this.m_stateMachine.Events[edge.InvokerName];

            needArgs = needArgs && (@event.Args.Count > 0);

            this.m_writer.Write($"std::function<");
            this.m_writer.Write(isFunction ? $"std::optional<{STATES_ENUM_NAME}>" : "void");
            if (needArgs)
            {
                this.m_writer.Write("(");
                for (int i = 0; i < @event.Args.Count; ++i)
                {
                    KeyValuePair<string, string> arg = @event.Args[i];
                    if (i != 0)
                    {
                        this.m_writer.Write(", ");
                    };
                    this.m_writer.Write($"{arg.Value}");
                }
                this.m_writer.Write(")");
            };
            this.m_writer.WriteLine($"> {callbackName}; ");
        }

        private static Regex s_splitRegex = new Regex(@"\r?\n", RegexOptions.Compiled);
        private void WriteVerbatimCode(string code)
        {
            foreach (string line in s_splitRegex.Split(code))
            {
                this.m_writer.WriteLine(line);
            }
        }

        private const string STATES_ENUM_NAME = "State";

        private const string HEADER_PREAMBLE_CODE =
@"
#pragma once

#include <stdexcept>
#include <functional>
#include <optional>

";

        private const string TIMER_CODE =
@"
template<class T>
concept Timer = requires(T t) {
    { t.StartOrReset(double timerDelaySeconds) };
    { t.Stop() };
};

template<Timer T>
using TimerFiredCallback = void(*)(const T* timer);

template<Timer T>
using TimerFactory = T*(*)(const char* timerName, TimerFiredCallback<T> callback);

";

    }
}
