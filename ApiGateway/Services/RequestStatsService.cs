using System.Threading;

namespace ApiGateway.Services;

public class RequestStatsService : IRequestStatsService
{
    private long _allowed;
    private long _rejected;

    public void RecordAllowed() => Interlocked.Increment(ref _allowed);

    public void RecordRejected() => Interlocked.Increment(ref _rejected);

    public RequestStats GetStats()
    {
        var allowed = Interlocked.Read(ref _allowed);
        var rejected = Interlocked.Read(ref _rejected);
        return new RequestStats(Total: allowed + rejected, Allowed: allowed, Rejected: rejected);
    }
}
