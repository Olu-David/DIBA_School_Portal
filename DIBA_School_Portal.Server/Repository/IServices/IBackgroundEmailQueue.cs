using DIBA_School_Portal.Server.Repository.Services.Background;
using System.Collections.Generic;
using System.Threading;

namespace DIBA_School_Portal.Server.Repository.IServices
{
    public interface IBackgroundEmailQueue
    {
        void Enqueue(EmailJob job);

        IAsyncEnumerable<EmailJob> DequeueAllAsync(CancellationToken ct);
    }
}