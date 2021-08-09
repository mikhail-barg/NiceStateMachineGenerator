// generated by NiceStateMachineGenerator v1.0.0.0

#pragma once

#include <stdexcept>
#include <functional>


namespace generated
{
    
    template<class T>
    concept Timer = requires(T t) {
        { t.StartOrReset(double timerDelaySeconds) };
        { t.Stop() };
    };
    
    template<Timer T>
    using TimerFiredCallback = void(*)(const T* timer);
    
    template<Timer T>
    using TimerFactory = T*(*)(const char* timerName, TimerFiredCallback<T> callback);
    
    
    template <Timer T>
    class StateMachine
    {
    public:
        enum class States
        {
            Trying_Start,
            Trying_Retransmit,
            Proceeding,
            Completed,
            Completed_Consume,
            Terminated,
        };
        
        /*send request*/
        std::function<void()> OnStateEnter__Trying_Start;
        /*The client transaction MUST be destroyed the instant it enters the 'Terminated' state*/
        std::function<void()> OnStateEnter__Terminated;
        
        /*the response MUST be passed to the TU*/
        std::function<void(t_packet)> OnEventTraverse__SIP_1xx; 
        /*the response MUST be passed to the TU*/
        std::function<void(t_packet)> OnEventTraverse__SIP_200_699; 
        /*the client transaction SHOULD inform the TU about the error*/
        std::function<void()> OnEventTraverse__TransportError;
        /*the client transaction SHOULD inform the TU about the timeout*/
        std::function<void()> OnTimerTraverse__Timer_F;
        /*retransmit*/
        std::function<void()> OnTimerTraverse__Timer_E;
        /*retransmit*/
        std::function<void()> OnTimerTraverse__Timer_E2;
        
    private:
        States m_currentState = States::Trying_Start;
        T* Timer_F;
        T* Timer_E;
        T* Timer_E2;
        T* Timer_K;
        double m_Timer_E_delay = 0.5;
        
    public:
        StateMachine(TimerFactory<T> timerFactory)
        {
            TimerFiredCallback<T> timerCallback = std::bind(&StateMachine::OnTimer, this, std::placeholders::_1);
            Timer_F = timerFactory("Timer_F", timerCallback);
            Timer_E = timerFactory("Timer_E", timerCallback);
            Timer_E2 = timerFactory("Timer_E2", timerCallback);
            Timer_K = timerFactory("Timer_K", timerCallback);
        }
        
        ~StateMachine()
        {
            delete Timer_F;
            delete Timer_E;
            delete Timer_E2;
            delete Timer_K;
        }
        
        States GetCurrentState()
        {
            return m_currentState;
        }
        
        void Start()
        {
            m_currentState = States::Trying_Start;
            Timer_F->StartOrReset(32);
            Timer_E->StartOrReset(m_Timer_E_delay);
            if (OnStateEnter__Trying_Start) { OnStateEnter__Trying_Start(); }
        }
        
        void ProcessEvent__SIP_1xx(t_packet packet)
        {
            switch (m_currentState)
            {
            case States::Trying_Start:
                if (OnEventTraverse__SIP_1xx) { OnEventTraverse__SIP_1xx(packet); }
                SetState(States::Proceeding);
                break;
                
            case States::Trying_Retransmit:
                if (OnEventTraverse__SIP_1xx) { OnEventTraverse__SIP_1xx(packet); }
                SetState(States::Proceeding);
                break;
                
            case States::Proceeding:
                if (OnEventTraverse__SIP_1xx) { OnEventTraverse__SIP_1xx(packet); }
                SetState(States::Proceeding);
                break;
                
            case States::Completed_Consume:
                SetState(States::Completed_Consume);
                break;
                
            default:
                throw std::runtime_error("Event SIP_1xx is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__SIP_200_699(t_packet packet)
        {
            switch (m_currentState)
            {
            case States::Trying_Start:
                if (OnEventTraverse__SIP_200_699) { OnEventTraverse__SIP_200_699(packet); }
                SetState(States::Completed);
                break;
                
            case States::Trying_Retransmit:
                if (OnEventTraverse__SIP_200_699) { OnEventTraverse__SIP_200_699(packet); }
                SetState(States::Completed);
                break;
                
            case States::Proceeding:
                if (OnEventTraverse__SIP_200_699) { OnEventTraverse__SIP_200_699(packet); }
                SetState(States::Completed);
                break;
                
            case States::Completed_Consume:
                SetState(States::Completed_Consume);
                break;
                
            default:
                throw std::runtime_error("Event SIP_200_699 is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__TransportError()
        {
            switch (m_currentState)
            {
            case States::Trying_Start:
                if (OnEventTraverse__TransportError) { OnEventTraverse__TransportError(); }
                SetState(States::Terminated);
                break;
                
            case States::Trying_Retransmit:
                if (OnEventTraverse__TransportError) { OnEventTraverse__TransportError(); }
                SetState(States::Terminated);
                break;
                
            case States::Proceeding:
                if (OnEventTraverse__TransportError) { OnEventTraverse__TransportError(); }
                SetState(States::Terminated);
                break;
                
            case States::Completed_Consume:
                SetState(States::Completed_Consume);
                break;
                
            default:
                throw std::runtime_error("Event TransportError is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
    private:
        void OnTimer(T* timer)
        {
            switch (m_currentState)
            {
            case States::Trying_Start:
                if (timer == Timer_F)
                {
                    if (OnTimerTraverse__Timer_F) { OnTimerTraverse__Timer_F(); }
                    SetState(States::Terminated);
                }
                else if (timer == Timer_E)
                {
                    if (OnTimerTraverse__Timer_E) { OnTimerTraverse__Timer_E(); }
                    SetState(States::Trying_Retransmit);
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state Trying_Start");
                }
                break;
                
            case States::Trying_Retransmit:
                if (timer == Timer_F)
                {
                    if (OnTimerTraverse__Timer_F) { OnTimerTraverse__Timer_F(); }
                    SetState(States::Terminated);
                }
                else if (timer == Timer_E)
                {
                    if (OnTimerTraverse__Timer_E) { OnTimerTraverse__Timer_E(); }
                    SetState(States::Trying_Retransmit);
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state Trying_Retransmit");
                }
                break;
                
            case States::Proceeding:
                if (timer == Timer_F)
                {
                    if (OnTimerTraverse__Timer_F) { OnTimerTraverse__Timer_F(); }
                    SetState(States::Terminated);
                }
                else if (timer == Timer_E2)
                {
                    if (OnTimerTraverse__Timer_E2) { OnTimerTraverse__Timer_E2(); }
                    SetState(States::Proceeding);
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state Proceeding");
                }
                break;
                
            case States::Completed_Consume:
                if (timer == Timer_K)
                {
                    SetState(States::Terminated);
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state Completed_Consume");
                }
                break;
                
            default:
                throw std::runtime_error("No timer events expected in current state" /*+ this.CurrentState*/);
            }
        }
        
        void SetState(States state)
        {
            switch (state)
            {
            case States::Trying_Start:
                m_currentState = States::Trying_Start;
                Timer_F->StartOrReset(32);
                Timer_E->StartOrReset(m_Timer_E_delay);
                if (OnStateEnter__Trying_Start) { OnStateEnter__Trying_Start(); }
                break;
                
            case States::Trying_Retransmit:
                m_currentState = States::Trying_Retransmit;
                m_Timer_E_delay *= 2;
                if (m_Timer_E_delay > 4) { m_Timer_E_delay = 4; }
                Timer_E->StartOrReset(m_Timer_E_delay);
                break;
                
            case States::Proceeding:
                m_currentState = States::Proceeding;
                Timer_E->Stop();
                Timer_E2->StartOrReset(4);
                break;
                
            case States::Completed:
                m_currentState = States::Completed;
                Timer_E->Stop();
                Timer_E2->Stop();
                Timer_F->Stop();
                Timer_K->StartOrReset(5);
                SetState(States::Completed_Consume);
                break;
                
            case States::Completed_Consume:
                m_currentState = States::Completed_Consume;
                break;
                
            case States::Terminated:
                m_currentState = States::Terminated;
                if (OnStateEnter__Terminated) { OnStateEnter__Terminated(); }
                break;
                
            default:
                throw std::runtime_error("Unexpected state " /* + state*/);
            }
        }
        
    };
}
