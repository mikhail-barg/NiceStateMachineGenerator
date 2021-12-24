using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
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

    public enum EdgeTargetType
    {
        state,
        no_change,
        failure
    }

    public sealed class EdgeTarget: IEquatable<EdgeTarget>
    {
        public readonly EdgeTargetType TargetType;
        public readonly string? StateName;

        private EdgeTarget(EdgeTargetType targetType, string? stateName)
        {
            this.TargetType = targetType;
            this.StateName = stateName;
        }

        public static EdgeTarget CreateNoChangeTarget()
        {
            return new EdgeTarget(EdgeTargetType.no_change, null);
        }

        public static EdgeTarget CreateFailureTarget()
        {
            return new EdgeTarget(EdgeTargetType.failure, null);
        }

        public static EdgeTarget CreateStateTarget(string stateName)
        {
            return new EdgeTarget(EdgeTargetType.state, stateName);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EdgeTarget);
        }

        public bool Equals(EdgeTarget? other)
        {
            if (other == null)
            {
                return false;
            }

            return this.TargetType == other.TargetType
                && String.Equals(this.StateName, other.StateName);
        }

        public override int GetHashCode()
        {
            int result = 17;
            result = result * 11 + this.TargetType.GetHashCode();
            result = result * 11 + (this.StateName?.GetHashCode() ?? 0);
            return result;
        }

        public override string ToString()
        {
            switch (this.TargetType)
            {
            case EdgeTargetType.no_change:
                return "null";
            case EdgeTargetType.failure:
                return "false";
            case EdgeTargetType.state:
                return this.StateName!;
            default:
                throw new Exception("Unexpected target type " + this.TargetType);
            }
        }
    }

    public sealed class EdgeDescr
    {
        public readonly bool IsTimer;
        public readonly string InvokerName;

        public readonly HashSet<EdgeTraverseCallbackType> OnTraverseEventTypes = new HashSet<EdgeTraverseCallbackType>();
        public string? TraverseEventComment { get; set; }
        public EdgeTarget? Target { get; set; }
        public Dictionary<string, EdgeTarget>? Targets { get; set; }

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
        public Dictionary<string, EdgeTarget>? OnEnterEventAlluxTargets { get; set; }
        public readonly Dictionary<string, TimerStartDescr> StartTimers = new Dictionary<string, TimerStartDescr>();
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

    public sealed class TimerStartDescr
    {
        public readonly string TimerName;
        public TimerModifyDescr? Modify { get; set; } = null;

        public TimerStartDescr(string timerName)
        {
            this.TimerName = timerName;
        }
    }

    public sealed class TimerModifyDescr
    {
        public double? set = null;

        public double? multiplier = null;
        public double? increment = null;
        public double? min = null;
        public double? max = null;
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
