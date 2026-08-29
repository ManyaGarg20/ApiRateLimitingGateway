namespace ApiGateway.Services;

public record RequestStats(long Total, long Allowed, long Rejected);

public interface IRequestStatsService
{
    void RecordAllowed();
    void RecordRejected();
    RequestStats GetStats();
}
