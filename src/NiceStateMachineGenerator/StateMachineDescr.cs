using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
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
        public readonly string InvokerName;

        public bool NeedOnTraverseEvent { get; set; }
        public string? TargetState { get; set; }
        public bool IsFailure { get; set; }

        public EdgeDescr(string invokerName)
        {
            this.InvokerName = invokerName;
        }
    }

    public sealed class StateDescr
    {
        public readonly string Name;
        
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
