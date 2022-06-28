using GeneratedSMHttpSample;

namespace AsyncStateMachineExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            SampleHttpController sampleHttpController = new SampleHttpController();
            await sampleHttpController.Initialize();
            await sampleHttpController.Authorized();
            await sampleHttpController.LoadCategories();
            // Try to execute event in wrong state of state machine. It will not affect state
            await sampleHttpController.Authorized();

            Console.WriteLine($"State machine complete = {sampleHttpController.IsComplete}");
        }
    }
}