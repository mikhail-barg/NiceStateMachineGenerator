using GeneratedSMHttpSample;

namespace AsyncStateMachineExample
{
    internal class SampleHttpController
    {
        private HttpSampleStateMachine m_httpSampleStateMachine;
        public bool IsComplete { get; set; } = false;

        public async Task Initialize()
        {
            this.m_httpSampleStateMachine = new HttpSampleStateMachine(createTimer: null);
            this.m_httpSampleStateMachine.OnLog += Console.WriteLine;
            this.m_httpSampleStateMachine.OnStateEnter += M_httpSampleStateMachine_OnStateEnter;
            this.m_httpSampleStateMachine.OnStateEnter__LoadCategories += HttpSampleStateMachine_OnStateEnter__LoadCategories;
            this.m_httpSampleStateMachine.OnStateEnter__LoadGoodsList += HttpSampleStateMachine_OnStateEnter__LoadGoodsList;

            await this.m_httpSampleStateMachine.Start();
        }

        private Task M_httpSampleStateMachine_OnStateEnter(HttpSampleStateMachine.State state)
        {
            Console.WriteLine($"New state is {state}");
            if (state == HttpSampleStateMachine.State.GoodsListLoaded)
            {
                this.IsComplete = true;
            }

            return Task.Delay(200);
        }

        public async Task Authorized()
        {
            await this.m_httpSampleStateMachine.ProcessEvent__authorized(username: "testUser", userId: 123, firstName: "Ivan", secondName: "Ivanov", middleName: "Ivanovich");
            await this.m_httpSampleStateMachine.ProcessEvent__external_event_get_categories();
        }

        public Task LoadCategories()
        {
            return this.m_httpSampleStateMachine.ProcessEvent__external_event_user_choose_category(
                userId: 123,
                choosenCategory: "monitors",
                analyticsSelectionLocalTime: DateTime.Now,
                analyticsRegion: "en-US",
                analyticsIsVPNEnabled: true);
        }

        private async Task HttpSampleStateMachine_OnStateEnter__LoadGoodsList()
        {
            await SimulateSomeExternalAPI("https://example.com/goods_list");
            await this.m_httpSampleStateMachine.ProcessEvent__request_completed();
        }

        private async Task HttpSampleStateMachine_OnStateEnter__LoadCategories()
        {
            await SimulateSomeExternalAPI("https://example.com/categories");
            await this.m_httpSampleStateMachine.ProcessEvent__request_completed();
        }

        private Task SimulateSomeExternalAPI(string url)
        {
            Console.WriteLine($"Simulating some API. Request url: {url}");
            return Task.Delay(200);
        }
    }
}
