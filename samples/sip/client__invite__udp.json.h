// generated by NiceStateMachineGenerator v1.0.0.0

#pragma once

#include <stdexcept>
#include <functional>
#include <optional>


#include sip.h

namespace sip::generated
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
    class client__invite__udp
    {
    public:
        enum class State
        {
            Calling_Start,
            Calling_Retransmit,
            Proceeding,
            Completed,
            Terminated,
        };
        
        /*INVITE sent*/
        std::function<void()> OnStateEnter__Calling_Start;
        /*INVITE sent*/
        std::function<void()> OnStateEnter__Calling_Retransmit;
        /*The client transaction MUST be destroyed the instant it enters the 'Terminated' state*/
        std::function<void()> OnStateEnter__Terminated;
        
        /*Furthermore, the provisional response MUST be passed to the TU*/
        std::function<void(t_packet)> OnEventTraverse__SIP_1xx; 
        /*and the response MUST be passed up to the TU*/
        std::function<void(t_packet)> OnEventTraverse__SIP_2xx; 
        /*The client transaction MUST pass the received response up to the TU, and the client transaction MUST generate an ACK request*/
        std::function<void(t_packet)> OnEventTraverse__SIP_300_699; 
        /*Inform TU*/
        std::function<void()> OnEventTraverse__TransportError; 
        /*the client transaction SHOULD inform the TU that a timeout has occurred.*/
        std::function<void()> OnTimerTraverse__Timer_B; 
        /*Any retransmissions of the final response that are received while in the 'Completed' state MUST cause the ACK to be re-passed to the transport layer for retransmission, but the newly received response MUST NOT be passed up to the TU.*/
        std::function<void(t_packet)> OnEventTraverse__Completed__SIP_300_699; 
        
    private:
        State m_currentState = State::Calling_Start;
        T* Timer_A;
        T* Timer_A2;
        T* Timer_B;
        T* Timer_D;
        
    public:
        client__invite__udp(TimerFactory<T> timerFactory)
        {
            TimerFiredCallback<T> timerCallback = std::bind(&client__invite__udp::OnTimer, this, std::placeholders::_1);
            Timer_A = timerFactory("Timer_A", timerCallback);
            Timer_A2 = timerFactory("Timer_A2", timerCallback);
            Timer_B = timerFactory("Timer_B", timerCallback);
            Timer_D = timerFactory("Timer_D", timerCallback);
        }
        
        ~client__invite__udp()
        {
            delete Timer_A;
            delete Timer_A2;
            delete Timer_B;
            delete Timer_D;
        }
        
        State GetCurrentState()
        {
            return m_currentState;
        }
        
        void Start()
        {
            m_currentState = State::Calling_Start;
            Timer_A->StartOrReset(0.5);
            Timer_B->StartOrReset(32);
            if (OnStateEnter__Calling_Start) { OnStateEnter__Calling_Start(); }
        }
        
        void ProcessEvent__SIP_1xx(t_packet packet)
        {
            switch (m_currentState)
            {
            case State::Calling_Start:
                if (OnEventTraverse__SIP_1xx) { OnEventTraverse__SIP_1xx(packet); }
                SetState(State::Proceeding);
                break;
                
            case State::Calling_Retransmit:
                if (OnEventTraverse__SIP_1xx) { OnEventTraverse__SIP_1xx(packet); }
                SetState(State::Proceeding);
                break;
                
            case State::Proceeding:
                if (OnEventTraverse__SIP_1xx) { OnEventTraverse__SIP_1xx(packet); }
                SetState(State::Proceeding);
                break;
                
            case State::Completed:
                throw std::runtime_error("Event SIP_1xx is forbidden in current state");
                
            default:
                throw std::runtime_error("Event SIP_1xx is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__SIP_2xx(t_packet packet)
        {
            switch (m_currentState)
            {
            case State::Calling_Start:
                if (OnEventTraverse__SIP_2xx) { OnEventTraverse__SIP_2xx(packet); }
                SetState(State::Terminated);
                break;
                
            case State::Calling_Retransmit:
                if (OnEventTraverse__SIP_2xx) { OnEventTraverse__SIP_2xx(packet); }
                SetState(State::Terminated);
                break;
                
            case State::Proceeding:
                if (OnEventTraverse__SIP_2xx) { OnEventTraverse__SIP_2xx(packet); }
                SetState(State::Terminated);
                break;
                
            case State::Completed:
                throw std::runtime_error("Event SIP_2xx is forbidden in current state");
                
            default:
                throw std::runtime_error("Event SIP_2xx is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__SIP_300_699(t_packet packet)
        {
            switch (m_currentState)
            {
            case State::Calling_Start:
                if (OnEventTraverse__SIP_300_699) { OnEventTraverse__SIP_300_699(packet); }
                SetState(State::Completed);
                break;
                
            case State::Calling_Retransmit:
                if (OnEventTraverse__SIP_300_699) { OnEventTraverse__SIP_300_699(packet); }
                SetState(State::Completed);
                break;
                
            case State::Proceeding:
                if (OnEventTraverse__SIP_300_699) { OnEventTraverse__SIP_300_699(packet); }
                SetState(State::Completed);
                break;
                
            case State::Completed:
                if (OnEventTraverse__Completed__SIP_300_699) { OnEventTraverse__Completed__SIP_300_699(packet); }
                SetState(State::Completed);
                break;
                
            default:
                throw std::runtime_error("Event SIP_300_699 is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__TransportError()
        {
            switch (m_currentState)
            {
            case State::Calling_Start:
                if (OnEventTraverse__TransportError) { OnEventTraverse__TransportError(); }
                SetState(State::Terminated);
                break;
                
            case State::Calling_Retransmit:
                if (OnEventTraverse__TransportError) { OnEventTraverse__TransportError(); }
                SetState(State::Terminated);
                break;
                
            case State::Proceeding:
                if (OnEventTraverse__TransportError) { OnEventTraverse__TransportError(); }
                SetState(State::Terminated);
                break;
                
            case State::Completed:
                if (OnEventTraverse__TransportError) { OnEventTraverse__TransportError(); }
                SetState(State::Terminated);
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
            case State::Calling_Start:
                if (timer == Timer_A)
                {
                    SetState(State::Calling_Retransmit);
                }
                else if (timer == Timer_B)
                {
                    throw std::runtime_error("Event Timer_B is forbidden in current state");
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state Calling_Start");
                }
                break;
                
            case State::Calling_Retransmit:
                if (timer == Timer_A2)
                {
                    SetState(State::Calling_Retransmit);
                }
                else if (timer == Timer_B)
                {
                    if (OnTimerTraverse__Timer_B) { OnTimerTraverse__Timer_B(); }
                    SetState(State::Terminated);
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state Calling_Retransmit");
                }
                break;
                
            case State::Completed:
                if (timer == Timer_D)
                {
                    SetState(State::Terminated);
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state Completed");
                }
                break;
                
            default:
                throw std::runtime_error("No timer events expected in current state" /*+ this.CurrentState*/);
            }
        }
        
        void SetState(State state)
        {
            switch (state)
            {
            case State::Calling_Start:
                m_currentState = State::Calling_Start;
                Timer_A->StartOrReset(0.5);
                Timer_B->StartOrReset(32);
                if (OnStateEnter__Calling_Start) { OnStateEnter__Calling_Start(); }
                break;
                
            case State::Calling_Retransmit:
                m_currentState = State::Calling_Retransmit;
                Timer_A->Stop();
                Timer_A2->StartOrReset(1);
                if (OnStateEnter__Calling_Retransmit) { OnStateEnter__Calling_Retransmit(); }
                break;
                
            case State::Proceeding:
                m_currentState = State::Proceeding;
                Timer_A->Stop();
                Timer_A2->Stop();
                Timer_B->Stop();
                break;
                
            case State::Completed:
                m_currentState = State::Completed;
                Timer_A->Stop();
                Timer_A2->Stop();
                Timer_B->Stop();
                Timer_D->StartOrReset(32);
                break;
                
            case State::Terminated:
                m_currentState = State::Terminated;
                if (OnStateEnter__Terminated) { OnStateEnter__Terminated(); }
                break;
                
            default:
                throw std::runtime_error("Unexpected state " /* + state*/);
            }
        }
        
    };
}
