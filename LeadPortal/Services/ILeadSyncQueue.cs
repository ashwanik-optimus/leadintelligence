namespace LeadPortal.Services;

/// <summary>
/// Decouples request handling from CRM synchronization. Producers (the API) enqueue a
/// lead id; a single background consumer drains the queue and performs the sync.
/// </summary>
public interface ILeadSyncQueue
{
    ValueTask EnqueueAsync(Guid leadId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
