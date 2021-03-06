// generated by NiceStateMachineGenerator v1.0.0.0

#pragma once

#include <stdexcept>
#include <functional>


namespace generated
{
    
    template<class T>
    concept Timer = requires(T t) {
        { t.StartOrReset() };
        { t.Stop() };
    };
    
    template<Timer T>
    using TimerFiredCallback = void(*)(const T* timer);
    
    template<Timer T>
    using TimerFactory = T*(*)(const char* timerName, double timerDelaySeconds, TimerFiredCallback<T> callback);
    
    
    template <Timer T>
    class StateMachine
    {
    public:
        enum class States
        {
            in_call,
            session_termination_process,
            awaiting_asr_fully_finalized,
            asr_fully_finalized,
            early_termination,
            termination,
        };
        
        /*sipCall.Stop()*/
        std::function<void()> OnStateEnter__session_termination_process;
        /*asr.SendFinalized()*/
        std::function<void()> OnStateEnter__awaiting_asr_fully_finalized;
        /*stateMachine.OnExternalEvent(DialogTerminated)*/
        std::function<void()> OnStateEnter__asr_fully_finalized;
        /*warn*/
        std::function<void()> OnStateEnter__early_termination;
        /*productionPlugin.WriteSessionToDb*/
        std::function<void()> OnStateEnter__termination;
        
        
    private:
        States m_currentState = States::in_call;
        T* asr_timeout;
        
    public:
        StateMachine(TimerFactory<T> timerFactory)
        {
            TimerFiredCallback<T> timerCallback = std::bind(&StateMachine::OnTimer, this, std::placeholders::_1);
            asr_timeout = timerFactory("asr_timeout", 10, timerCallback);
            asr_timeout->Stop();
        }
        
        ~StateMachine()
        {
            delete asr_timeout;
        }
        
        States GetCurrentState()
        {
            return m_currentState;
        }
        
        void Start()
        {
            m_currentState = States::in_call;
        }
        
        void ProcessEvent__telephony_session_terminated()
        {
            switch (m_currentState)
            {
            case States::in_call:
                SetState(States::awaiting_asr_fully_finalized);
                break;
                
            case States::session_termination_process:
                SetState(States::awaiting_asr_fully_finalized);
                break;
                
            default:
                throw std::runtime_error("Event telephony_session_terminated is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__asr_fully_finalized()
        {
            switch (m_currentState)
            {
            case States::awaiting_asr_fully_finalized:
                SetState(States::asr_fully_finalized);
                break;
                
            case States::asr_fully_finalized:
                break;
                
            default:
                throw std::runtime_error("Event asr_fully_finalized is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__script_final_state_reached()
        {
            switch (m_currentState)
            {
            case States::in_call:
                SetState(States::early_termination);
                break;
                
            case States::session_termination_process:
                SetState(States::early_termination);
                break;
                
            case States::awaiting_asr_fully_finalized:
                SetState(States::early_termination);
                break;
                
            case States::asr_fully_finalized:
                SetState(States::termination);
                break;
                
            default:
                throw std::runtime_error("Event script_final_state_reached is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
        void ProcessEvent__session_termination_request()
        {
            switch (m_currentState)
            {
            case States::in_call:
                SetState(States::session_termination_process);
                break;
                
            case States::session_termination_process:
                break;
                
            case States::awaiting_asr_fully_finalized:
                break;
                
            case States::asr_fully_finalized:
                break;
                
            default:
                throw std::runtime_error("Event session_termination_request is not expected in current state " /* + this.CurrentState*/);
            }
        }
        
    private:
        void OnTimer(T* timer)
        {
            switch (m_currentState)
            {
            case States::awaiting_asr_fully_finalized:
                if (timer == asr_timeout)
                {
                    SetState(States::asr_fully_finalized);
                }
                else 
                {
                    throw std::runtime_error("Unexpected timer finish in state awaiting_asr_fully_finalized");
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
            case States::in_call:
                m_currentState = States::in_call;
                break;
                
            case States::session_termination_process:
                m_currentState = States::session_termination_process;
                if (OnStateEnter__session_termination_process) { OnStateEnter__session_termination_process(); }
                break;
                
            case States::awaiting_asr_fully_finalized:
                m_currentState = States::awaiting_asr_fully_finalized;
                asr_timeout->StartOrReset();
                if (OnStateEnter__awaiting_asr_fully_finalized) { OnStateEnter__awaiting_asr_fully_finalized(); }
                break;
                
            case States::asr_fully_finalized:
                m_currentState = States::asr_fully_finalized;
                asr_timeout->Stop();
                if (OnStateEnter__asr_fully_finalized) { OnStateEnter__asr_fully_finalized(); }
                break;
                
            case States::early_termination:
                m_currentState = States::early_termination;
                if (OnStateEnter__early_termination) { OnStateEnter__early_termination(); }
                SetState(States::termination);
                break;
                
            case States::termination:
                m_currentState = States::termination;
                if (OnStateEnter__termination) { OnStateEnter__termination(); }
                break;
                
            default:
                throw std::runtime_error("Unexpected state " /* + state*/);
            }
        }
        
    };
}
