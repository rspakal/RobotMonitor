namespace MotionMonitor
{
    internal class Program
    {
        public async Task Main()
        {
            MotionDataProvider dataProvider = new MotionDataProvider();
            await dataProvider.StreamHandler.StartAsync();
        }
    }
}
