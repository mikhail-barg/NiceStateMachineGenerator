using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    [Flags]
    public enum EdgeTraverseCallbackType
    {
        //FromState__EventOrTimer__ToState(args)
        full,

        //EventOrTimer(args)
        event_only,

        //EventOrTimer__ToState(args)
        event_and_target,

        //FromState__EventOrTimer(args)
        source_and_event,

        //FromState__ToState
        source_and_target,

        //FromState
        source_only,

        //ToState
        target_only,
    }

    public sealed class EventDescr
    {
        public readonly string Name;
        public readonly List<KeyValuePair<string, string>> Args = new List<KeyValuePair<string, string>>();
        public HashSet<string>? AfterStates { get; set; }
        public bool OnlyOnce { get; set; }

        public EventDescr(string name)
        {
            this.Name = name;
        }
    }

    public sealed class EdgeDescr
    {
        public readonly bool IsTimer;
        public readonly string InvokerName;

        public readonly HashSet<EdgeTraverseCallbackType> OnTraverseEventTypes = new HashSet<EdgeTraverseCallbackType>();
        public string? TraverseEventComment { get; set; }
        public string? TargetState { get; set; }
        public bool IsFailure { get; set; }

        public EdgeDescr(string invokerName, bool isTimer)
        {
            this.IsTimer = isTimer;
            this.InvokerName = invokerName;
        }
    }

    public sealed class StateDescr
    {
        public readonly string Name;
        
        public string? OnEnterEventComment { get; set; }
        public bool NeedOnEnterEvent { get; set; }
        public readonly HashSet<string> StartTimers = new HashSet<string>();
        public readonly HashSet<string> StopTimers = new HashSet<string>();

        public Dictionary<string, EdgeDescr>? EventEdges { get; set; }
        public Dictionary<string, EdgeDescr>? TimerEdges { get; set; }
        public string? NextStateName { get; set; }
        public bool IsFinal { get; set; }

        public StateDescr(string name)
        {
            this.Name = name;
        }
    }

    public sealed class TimerDescr
    {
        public readonly string Name;
        public readonly double IntervalSeconds;

        public TimerDescr(string name, double intervalSeconds)
        {
            this.Name = name;
            this.IntervalSeconds = intervalSeconds;
        }
    }

    public sealed class StateMachineDescr
    {
        public readonly Dictionary<string, TimerDescr> Timers; 
        public readonly Dictionary<string, EventDescr> Events;
        public readonly Dictionary<string, StateDescr> States;
        public readonly string StartState;

        public StateMachineDescr(string startState, Dictionary<string, TimerDescr> timers, Dictionary<string, EventDescr> events, Dictionary<string, StateDescr> states)
        {
            this.StartState = startState;
            this.Timers = timers;
            this.Events = events;
            this.States = states;
        }
    }
}
