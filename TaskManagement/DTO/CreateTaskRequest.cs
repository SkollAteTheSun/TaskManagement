using TaskManagement.Enums;

namespace TaskManagement.DTO;

public class CreateTaskRequest
{
    public string? Title { get; set; }
    public Priority? Priority { get; set; }
}
