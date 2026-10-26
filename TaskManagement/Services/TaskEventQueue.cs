using System.Threading.Channels;
using TaskManagement.DTO;

namespace TaskManagement.Services;

public class TaskEventQueue
{

    // HACK: используется in-memory очередь для упрощения задания
    // при аварийном завершении приложения события могут потеряться
    // для гарантированной доставки можно использовать Transactional Outbox
    private readonly Channel<TaskCompletedEvent> _channel = Channel.CreateUnbounded<TaskCompletedEvent>();

    public ChannelReader<TaskCompletedEvent> Reader => _channel.Reader;

    public bool Enqueue(TaskCompletedEvent message)
    {
        return _channel.Writer.TryWrite(message);
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}