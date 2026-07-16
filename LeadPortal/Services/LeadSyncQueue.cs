using System.Threading.Channels;

namespace LeadPortal.Services;

/// <summary>
/// In-memory, unbounded, thread-safe implementation of <see cref="ILeadSyncQueue"/>
/// backed by <see cref="System.Threading.Channels.Channel{T}"/>.
/// </summary>
/// <remarks>
/// The queue is in-process: items still in flight are lost on a hard shutdown. For
/// at-least-once delivery across restarts, back this with a durable queue (Azure
/// Storage/Service Bus) behind the same interface.
/// </remarks>
public class LeadSyncQueue : ILeadSyncQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid leadId, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(leadId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
