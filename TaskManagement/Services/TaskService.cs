using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.DTO;
using TaskManagement.Enums;
using TaskManagement.Models;

namespace TaskManagement.Services;

public class TaskService
{
    private readonly TaskDbContext _dbContext;
    private readonly TaskEventQueue _eventQueue;
    private readonly ILogger<TaskService> _logger;

    public TaskService(TaskDbContext dbContext, TaskEventQueue eventQueue, ILogger<TaskService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<TaskItem>> GetAllAsync()
    {
        return await _dbContext.Tasks
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TaskItem> CreateAsync(CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title!,
            IsCompleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            CompletedAt = null,
            Priority = request.Priority ?? Priority.Medium
        };

        _dbContext.Tasks.Add(task);

        await _dbContext.SaveChangesAsync();

        return task;
    }

    // используем optimistic concurrency через PostgreSQL xmin.
    // при одновременном обновлении только один запрос успешно сохранит изменения.
    public async Task<CompleteTaskResult> CompleteAsync(Guid id)
    {
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(x => x.Id == id);

        if (task is null)
            return CompleteTaskResult.NotFound;

        if (task.IsCompleted)
            return CompleteTaskResult.Conflict;

        task.IsCompleted = true;
        task.CompletedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync();

            var completedEvent = new TaskCompletedEvent(task.Id, task.Title, task.CompletedAt!.Value, task.Priority);

            if (!_eventQueue.Enqueue(completedEvent))
            {
                _logger.LogWarning(
                    "Failed to enqueue task.completed event for {TaskId}",
                    task.Id);
            }

            _eventQueue.Enqueue(completedEvent);
            return CompleteTaskResult.Completed;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompleteTaskResult.Conflict;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deletedCount = await _dbContext.Tasks
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();

        return deletedCount > 0;
    }
}