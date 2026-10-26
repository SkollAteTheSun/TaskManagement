using TaskManagement.Enums;

namespace TaskManagement.Models;

public class TaskItem
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public Priority Priority { get; set; } = Priority.Medium;

    public uint Version { get; set; }
}