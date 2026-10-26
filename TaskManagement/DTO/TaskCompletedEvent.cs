using TaskManagement.Enums;

namespace TaskManagement.DTO;

//record т.к. не будем изменять после создания
public record TaskCompletedEvent(
    Guid TaskId,
    string Title,
    DateTimeOffset CompletedAt,
    Priority Priority
);