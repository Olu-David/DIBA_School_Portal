using System.Threading.Channels;
using DIBA_School_Portal.Server.Repository.IServices;

namespace DIBA_School_Portal.Server.Repository.Services.Background
{
    public class BackgroundSmsQueue : IBackgroundSmsQueue
    {
        private readonly Channel<SmsJob> _channel;

        public BackgroundSmsQueue()
        {
            var options = new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            };

            _channel = Channel.CreateBounded<SmsJob>(options);
        }

        public void Enqueue(SmsJob job)
        {
            if (!_channel.Writer.TryWrite(job))
                Console.WriteLine($"[SmsQueue] Failed to enqueue {job.ToPhone}");
        }

        public async IAsyncEnumerable<SmsJob> DequeueAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(ct))
                yield return job;
        }
    }
}