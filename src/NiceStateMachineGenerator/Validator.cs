using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    public sealed class Validator
    {
        public static void Validate(StateMachineDescr stateMachine)
        {
            Validator validator = new Validator(stateMachine);
            validator.Validate();
        }

        private readonly StateMachineDescr m_stateMachine;
        private readonly Dictionary<string, int> m_timersToIndex;
        private readonly string[] m_timers;
        private readonly Dictionary<string, int> m_eventsToIndex;
        private readonly string[] m_events;

        private readonly HashSet<EdgeDescr> m_traversedEdges = new HashSet<EdgeDescr>();
        private readonly HashSet<StateDescr> m_visitedStates = new HashSet<StateDescr>();

        private Validator(StateMachineDescr stateMachine)
        {
            this.m_stateMachine = stateMachine;
            {
                this.m_timers = new string[this.m_stateMachine.Timers.Count];
                this.m_timersToIndex = new Dictionary<string, int>(this.m_timers.Length);
                int index = 0;
                foreach (string timer in this.m_stateMachine.Timers.Keys)
                {
                    this.m_timers[index] = timer;
                    this.m_timersToIndex.Add(timer, index);
                    ++index;
                }
            };
            {
                this.m_events = new string[this.m_stateMachine.Events.Count];
                this.m_eventsToIndex = new Dictionary<string, int>(this.m_events.Length);
                int index = 0;
                foreach (string @event in this.m_stateMachine.Events.Keys)
                {
                    this.m_events[index] = @event;
                    this.m_eventsToIndex.Add(@event, index);
                    ++index;
                }
            };
        }

        private void Validate()
        {
            this.Dfs(CreateBeforeStartState(), this.m_stateMachine.StartState, null, new Stack<ExecutionState>(), new Stack<string>());

            List<string> errors = new List<string>();
            foreach (StateDescr state in this.m_stateMachine.States.Values)
            {
                if (!this.m_visitedStates.Contains(state))
                {
                    errors.Add($"Unreachable state: " + state.Name);
                };
                if (state.EventEdges != null)
                {
                    foreach (EdgeDescr edge in state.EventEdges.Values)
                    {
                        if (!this.m_traversedEdges.Contains(edge))
                        {
                            errors.Add($"Unusable edge: {state.Name} [event: {edge.InvokerName}] -> {edge.TargetState}");
                        }
                    }
                };
                if (state.TimerEdges != null)
                {
                    foreach (EdgeDescr edge in state.TimerEdges.Values)
                    {
                        if (!this.m_traversedEdges.Contains(edge))
                        {
                            errors.Add($"Unusable edge: {state.Name} [timer: {edge.InvokerName}] -> {edge.TargetState}");
                        }
                    }
                }
            };
            if (errors.Count > 0)
            {
                throw new LogicValidationException(errors);
            };
        }

        private ExecutionState CreateBeforeStartState()
        {
            bool[] timersEnabledMask = new bool[this.m_timers.Length]; //all timers are disabled by default

            bool[] eventsEnabledMask = new bool[this.m_events.Length];
            for (int eventIndex = 0; eventIndex < this.m_events.Length; ++eventIndex)
            {
                string @event = this.m_events[eventIndex];
                EventDescr eventDescr = this.m_stateMachine.Events[@event];
                if (eventDescr.AfterStates == null)
                {
                    eventsEnabledMask[eventIndex] = true;
                }
            }

            bool[] onetimeEventFiredMask = new bool[this.m_events.Length]; //all events are not fired

            ExecutionState noState = new ExecutionState(
                stateName: "",
                eventsEnabledMask: eventsEnabledMask,
                timersEnabledMask: timersEnabledMask,
                onetimeEventFiredMask: onetimeEventFiredMask
            );

            return noState;
        }

        private void Dfs(ExecutionState prevState, string stateName, int? entryEventIndex, Stack<ExecutionState> steps, Stack<string> edges)
        {
            StateDescr state = this.m_stateMachine.States[stateName];
            this.m_visitedStates.Add(state);

            if (state.IsFinal)
            {
                //finished
                return;
            }

            bool[] timersEnabledMask = new bool[prevState.timersEnabledMask.Length];
            for (int timerIndex = 0; timerIndex < this.m_timers.Length; ++timerIndex)
            {
                string timer = this.m_timers[timerIndex];
                bool enabled;
                if (state.StartTimers.ContainsKey(timer))
                {
                    enabled = true;
                }
                else if (state.StopTimers.Contains(timer))
                {
                    enabled = false;
                }
                else
                {
                    enabled = prevState.timersEnabledMask[timerIndex];
                }
                timersEnabledMask[timerIndex] = enabled;
            }

            bool[] eventsEnabledMask = new bool[prevState.eventsEnabledMask.Length];
            for (int eventIndex = 0; eventIndex < this.m_events.Length; ++eventIndex)
            {
                string @event = this.m_events[eventIndex];
                bool enabled;
                EventDescr eventDescr = this.m_stateMachine.Events[@event];
                if (eventDescr.AfterStates != null && eventDescr.AfterStates.Contains(stateName))
                {
                    enabled = true;
                }
                else
                {
                    enabled = prevState.eventsEnabledMask[eventIndex];
                }
                eventsEnabledMask[eventIndex] = enabled;
            }

            bool[] onetimeEventFiredMask = new bool[prevState.onetimeEventFiredMask.Length];
            for (int eventIndex = 0; eventIndex < this.m_events.Length; ++eventIndex)
            {
                onetimeEventFiredMask[eventIndex] = prevState.onetimeEventFiredMask[eventIndex];
                if (eventIndex == entryEventIndex)
                {
                    string @event = this.m_events[eventIndex];
                    if (this.m_stateMachine.Events[@event].OnlyOnce)
                    {
                        onetimeEventFiredMask[eventIndex] = true;
                    }
                }
            }

            ExecutionState newState = new ExecutionState(
                stateName: stateName,
                eventsEnabledMask: eventsEnabledMask,
                timersEnabledMask: timersEnabledMask,
                onetimeEventFiredMask: onetimeEventFiredMask
            );

            if (steps.Contains(newState))
            {
                //already visited
                return;
            }


            //ok, go deeper
            steps.Push(newState);

            if (state.NextStateName != null)
            {
                edges.Push("[next_state]");
                Dfs(newState, state.NextStateName, null, steps, edges);
                edges.Pop();
            }
            else
            {
                bool traversedSomething = false;
                for (int eventIndex = 0; eventIndex < this.m_events.Length; ++eventIndex)
                {
                    string @event = this.m_events[eventIndex];
                    if (newState.eventsEnabledMask[eventIndex])
                    {
                        EventDescr eventDescr = this.m_stateMachine.Events[@event];
                        if (!eventDescr.OnlyOnce || !newState.onetimeEventFiredMask[eventIndex])
                        {
                            //event is avaliable to traverse
                            if (state.EventEdges == null || !state.EventEdges.TryGetValue(@event, out EdgeDescr? edge))
                            {
                                throw new LogicValidationException($"In state {stateName} event {@event} is NOT specified while it's enabled and available. Path: {PrintPath(steps, edges)}");
                            }
                            else
                            {
                                this.m_traversedEdges.Add(edge);
                                if (edge.TargetState != null)
                                {
                                    traversedSomething = true;
                                    edges.Push($"[event: {edge.InvokerName}]");
                                    Dfs(newState, edge.TargetState, eventIndex, steps, edges);
                                    edges.Pop();
                                }
                            }
                        }
                    }
                };

                for (int timerIndex = 0; timerIndex < this.m_timers.Length; ++timerIndex)
                {
                    string timer = this.m_timers[timerIndex];
                    if (!newState.timersEnabledMask[timerIndex])
                    {
                        //timer is disabled
                        if (state.TimerEdges != null && state.TimerEdges.TryGetValue(timer, out _))
                        {
                            //actually this may happen when in later loops we enter same state again. Let's think on fixing this if it actually happens
                            throw new LogicValidationException($"In state {stateName} timer {timer} is specified while it's not enabled. Path: {PrintPath(steps, edges)}");
                        };
                    }
                    else
                    {
                        //timer is enabled
                        if (state.TimerEdges == null || !state.TimerEdges.TryGetValue(timer, out EdgeDescr? edge))
                        {
                            throw new LogicValidationException($"In state {stateName} timer {timer} is NOT specified while it's enabled. Path: {PrintPath(steps, edges)}");
                        }
                        else
                        {
                            this.m_traversedEdges.Add(edge);
                            if (edge.TargetState != null)
                            {
                                traversedSomething = true;
                                edges.Push($"[timer: {edge.InvokerName}]");
                                Dfs(newState, edge.TargetState, null, steps, edges);
                                edges.Pop();
                            }
                        }
                    }
                };
                if (!traversedSomething)
                {
                    throw new LogicValidationException($"Stall detected in state {stateName}. Path: {PrintPath(steps, edges)}");
                }
            }

            steps.Pop();
        }

        private static string PrintPath(Stack<ExecutionState> steps, Stack<string> edges)
        {
            edges.Push("");
            string result = String.Join(
                " -> ",
                steps.Reverse()
                .Zip(edges.Reverse())
                .Select(t => t.First.stateName + " " + t.Second)
            );
            edges.Pop();
            return result;
        }

        private sealed class ExecutionState : IEquatable<ExecutionState>
        {
            public readonly string stateName;
            public readonly bool[] eventsEnabledMask;
            public readonly bool[] onetimeEventFiredMask;
            public readonly bool[] timersEnabledMask;

            public ExecutionState(string stateName, bool[] eventsEnabledMask, bool[] timersEnabledMask, bool[] onetimeEventFiredMask)
            {
                this.stateName = stateName;
                this.eventsEnabledMask = eventsEnabledMask;
                this.timersEnabledMask = timersEnabledMask;
                this.onetimeEventFiredMask = onetimeEventFiredMask;
            }

            public override int GetHashCode()
            {
                int result = 31;
                result = result * 27 + this.stateName.GetHashCode();
                for (int i = 0; i < this.eventsEnabledMask.Length; ++i)
                {
                    result = result * 27 + this.eventsEnabledMask[i].GetHashCode();
                }
                for (int i = 0; i < this.timersEnabledMask.Length; ++i)
                {
                    result = result * 27 + this.timersEnabledMask[i].GetHashCode();
                }
                for (int i = 0; i < this.onetimeEventFiredMask.Length; ++i)
                {
                    result = result * 27 + this.onetimeEventFiredMask[i].GetHashCode();
                }
                return result;
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as ExecutionState);
            }

            public bool Equals(ExecutionState? other)
            {
                if (other == null)
                {
                    return false;
                }

                if (this.stateName != other.stateName)
                {
                    return false;
                }

                if (!this.eventsEnabledMask.SequenceEqual(other.eventsEnabledMask))
                {
                    return false;
                }
                if (!this.timersEnabledMask.SequenceEqual(other.timersEnabledMask))
                {
                    return false;
                }

                return true;
            }
        }

    }
}
