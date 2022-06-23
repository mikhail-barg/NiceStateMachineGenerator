// generated by NiceStateMachineGenerator v1.0.0.0

using System;

using SampleApp.Sip;

namespace SampleApp.Sip.Generated
{
    public partial class client__non_invite__udp: IDisposable
    {
        
        public delegate void TimerFiredCallback(ITimer timer);
        
        public interface ITimer: IDisposable
        {
            void StartOrReset(double timerDelaySeconds);
            void Stop();
        }
        
        public delegate ITimer CreateTimerDelegate(string timerName, TimerFiredCallback callback);
        
        
        public enum State
        {
            Trying_Start,
            Trying_Retransmit,
            Proceeding,
            Completed,
            Completed_Consume,
            Terminated,
        }
        
        /**<summary>send request</summary>*/
        public event Action OnStateEnter__Trying_Start;
        /**<summary>The client transaction MUST be destroyed the instant it enters the 'Terminated' state</summary>*/
        public event Action OnStateEnter__Terminated;
        
        /**<summary>the response MUST be passed to the TU</summary>*/
        public event Action<t_packet> OnEventTraverse__SIP_1xx; 
        /**<summary>the response MUST be passed to the TU</summary>*/
        public event Action<t_packet> OnEventTraverse__SIP_200_699; 
        /**<summary>the client transaction SHOULD inform the TU about the error</summary>*/
        public event Action OnEventTraverse__TransportError; 
        /**<summary>the client transaction SHOULD inform the TU about the timeout</summary>*/
        public event Action OnTimerTraverse__Timer_F; 
        /**<summary>retransmit</summary>*/
        public event Action OnTimerTraverse__Timer_E; 
        /**<summary>retransmit</summary>*/
        public event Action OnTimerTraverse__Timer_E2; 
        
        private bool m_isDisposed = false;
        public event Action<string> OnLog;
        public event Action<State> OnStateEnter;
        private readonly ITimer Timer_F;
        private readonly ITimer Timer_E;
        private readonly ITimer Timer_E2;
        private readonly ITimer Timer_K;
        private double m_Timer_E_delay = 0.5;
        
        public State CurrentState { get; private set; } = State.Trying_Start;
        
        public client__non_invite__udp(CreateTimerDelegate createTimer)
        {
            this.Timer_F = createTimer("Timer_F", this.OnTimer);
            this.Timer_E = createTimer("Timer_E", this.OnTimer);
            this.Timer_E2 = createTimer("Timer_E2", this.OnTimer);
            this.Timer_K = createTimer("Timer_K", this.OnTimer);
        }
        
        public void Dispose()
        {
            if (!this.m_isDisposed)
            {
                this.Timer_F.Dispose();
                this.Timer_E.Dispose();
                this.Timer_E2.Dispose();
                this.Timer_K.Dispose();
                this.m_isDisposed = true;
            }
        }
        
        public void Start()
        {
            if (this.m_isDisposed)
            {
                return;
            }
            
            this.OnLog?.Invoke("Start");
            this.CurrentState = State.Trying_Start;
            this.OnStateEnter?.Invoke(State.Trying_Start);
            this.Timer_F.StartOrReset(32);
            this.Timer_E.StartOrReset(m_Timer_E_delay);
            OnStateEnter__Trying_Start?.Invoke();
        }
        
        private void OnTimer(ITimer timer)
        {
            if (this.m_isDisposed)
            {
                return;
            }
            
            switch (this.CurrentState)
            {
            case State.Trying_Start:
                if (timer == this.Timer_F)
                {
                    this.OnLog?.Invoke("OnTimer: Timer_F");
                    OnTimerTraverse__Timer_F?.Invoke();
                    SetState(State.Terminated);
                }
                else 
                if (timer == this.Timer_E)
                {
                    this.OnLog?.Invoke("OnTimer: Timer_E");
                    OnTimerTraverse__Timer_E?.Invoke();
                    SetState(State.Trying_Retransmit);
                }
                else 
                {
                    throw new Exception("Unexpected timer finish in state Trying_Start. Timer was " + timer);
                }
                break;
                
            case State.Trying_Retransmit:
                if (timer == this.Timer_F)
                {
                    this.OnLog?.Invoke("OnTimer: Timer_F");
                    OnTimerTraverse__Timer_F?.Invoke();
                    SetState(State.Terminated);
                }
                else 
                if (timer == this.Timer_E)
                {
                    this.OnLog?.Invoke("OnTimer: Timer_E");
                    OnTimerTraverse__Timer_E?.Invoke();
                    SetState(State.Trying_Retransmit);
                }
                else 
                {
                    throw new Exception("Unexpected timer finish in state Trying_Retransmit. Timer was " + timer);
                }
                break;
                
            case State.Proceeding:
                if (timer == this.Timer_F)
                {
                    this.OnLog?.Invoke("OnTimer: Timer_F");
                    OnTimerTraverse__Timer_F?.Invoke();
                    SetState(State.Terminated);
                }
                else 
                if (timer == this.Timer_E2)
                {
                    this.OnLog?.Invoke("OnTimer: Timer_E2");
                    OnTimerTraverse__Timer_E2?.Invoke();
                    SetState(State.Proceeding);
                }
                else 
                {
                    throw new Exception("Unexpected timer finish in state Proceeding. Timer was " + timer);
                }
                break;
                
            case State.Completed_Consume:
                if (timer == this.Timer_K)
                {
                    this.OnLog?.Invoke("OnTimer: Timer_K");
                    SetState(State.Terminated);
                }
                else 
                {
                    throw new Exception("Unexpected timer finish in state Completed_Consume. Timer was " + timer);
                }
                break;
                
            default:
                throw new Exception("No timer events expected in state " + this.CurrentState);
            }
        }
        
        public void ProcessEvent__SIP_1xx(t_packet packet)
        {
            if (this.m_isDisposed)
            {
                return;
            }
            
            this.OnLog?.Invoke("Event: SIP_1xx");
            switch (this.CurrentState)
            {
            case State.Trying_Start:
                OnEventTraverse__SIP_1xx?.Invoke(packet);
                SetState(State.Proceeding);
                break;
                
            case State.Trying_Retransmit:
                OnEventTraverse__SIP_1xx?.Invoke(packet);
                SetState(State.Proceeding);
                break;
                
            case State.Proceeding:
                OnEventTraverse__SIP_1xx?.Invoke(packet);
                SetState(State.Proceeding);
                break;
                
            case State.Completed_Consume:
                SetState(State.Completed_Consume);
                break;
                
            default:
                throw new Exception("Event SIP_1xx is not expected in state " + this.CurrentState);
            }
        }
        
        public void ProcessEvent__SIP_200_699(t_packet packet)
        {
            if (this.m_isDisposed)
            {
                return;
            }
            
            this.OnLog?.Invoke("Event: SIP_200_699");
            switch (this.CurrentState)
            {
            case State.Trying_Start:
                OnEventTraverse__SIP_200_699?.Invoke(packet);
                SetState(State.Completed);
                break;
                
            case State.Trying_Retransmit:
                OnEventTraverse__SIP_200_699?.Invoke(packet);
                SetState(State.Completed);
                break;
                
            case State.Proceeding:
                OnEventTraverse__SIP_200_699?.Invoke(packet);
                SetState(State.Completed);
                break;
                
            case State.Completed_Consume:
                SetState(State.Completed_Consume);
                break;
                
            default:
                throw new Exception("Event SIP_200_699 is not expected in state " + this.CurrentState);
            }
        }
        
        public void ProcessEvent__TransportError()
        {
            if (this.m_isDisposed)
            {
                return;
            }
            
            this.OnLog?.Invoke("Event: TransportError");
            switch (this.CurrentState)
            {
            case State.Trying_Start:
                OnEventTraverse__TransportError?.Invoke();
                SetState(State.Terminated);
                break;
                
            case State.Trying_Retransmit:
                OnEventTraverse__TransportError?.Invoke();
                SetState(State.Terminated);
                break;
                
            case State.Proceeding:
                OnEventTraverse__TransportError?.Invoke();
                SetState(State.Terminated);
                break;
                
            case State.Completed_Consume:
                SetState(State.Completed_Consume);
                break;
                
            default:
                throw new Exception("Event TransportError is not expected in state " + this.CurrentState);
            }
        }
        
        private void SetState(State state)
        {
            if (this.m_isDisposed)
            {
                return;
            }
            
            this.OnLog?.Invoke("SetState: " + state);
            switch (state)
            {
            case State.Trying_Start:
                this.CurrentState = State.Trying_Start;
                this.OnStateEnter?.Invoke(State.Trying_Start);
                this.Timer_F.StartOrReset(32);
                this.Timer_E.StartOrReset(m_Timer_E_delay);
                OnStateEnter__Trying_Start?.Invoke();
                break;
                
            case State.Trying_Retransmit:
                this.CurrentState = State.Trying_Retransmit;
                this.OnStateEnter?.Invoke(State.Trying_Retransmit);
                this.m_Timer_E_delay *= 2;
                if (this.m_Timer_E_delay > 4) { this.m_Timer_E_delay = 4; }
                this.Timer_E.StartOrReset(m_Timer_E_delay);
                break;
                
            case State.Proceeding:
                this.CurrentState = State.Proceeding;
                this.OnStateEnter?.Invoke(State.Proceeding);
                this.Timer_E.Stop();
                this.Timer_E2.StartOrReset(4);
                break;
                
            case State.Completed:
                this.CurrentState = State.Completed;
                this.OnStateEnter?.Invoke(State.Completed);
                this.Timer_E.Stop();
                this.Timer_E2.Stop();
                this.Timer_F.Stop();
                this.Timer_K.StartOrReset(5);
                SetState(State.Completed_Consume);
                break;
                
            case State.Completed_Consume:
                this.CurrentState = State.Completed_Consume;
                this.OnStateEnter?.Invoke(State.Completed_Consume);
                break;
                
            case State.Terminated:
                this.CurrentState = State.Terminated;
                this.OnStateEnter?.Invoke(State.Terminated);
                OnStateEnter__Terminated?.Invoke();
                break;
                
            default:
                throw new Exception("Unexpected state " + state);
            }
        }
        
    }
}
