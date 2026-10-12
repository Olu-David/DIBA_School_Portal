using DIBA_School_Portal.Server.Repository.IServices;
using DIBA_School_Portal.Server.Services.Background;
using System.Threading.Channels;

namespace DIBA_School_Portal.Server.Repository.Services.Background
{
    public class BackgroundEmailQueue : IBackgroundEmailQueue
    {
        private readonly Channel<EmailJob> _channel;

        public BackgroundEmailQueue()
        {
            var options = new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            };

            _channel = Channel.CreateBounded<EmailJob>(options);
        }

        public void Enqueue(EmailJob job)
        {
            if (!_channel.Writer.TryWrite(job))
                Console.WriteLine($"[EmailQueue] Failed to enqueue {job.ToEmail}");
        }

        public async IAsyncEnumerable<EmailJob> DequeueAllAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(ct))
                yield return job;
        }
    }
}