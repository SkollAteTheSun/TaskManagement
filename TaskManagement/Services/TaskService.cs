using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.DTO;
using TaskManagement.Enums;
using TaskManagement.Models;

namespace TaskManagement.Services;

public class TaskService
{
    private readonly TaskDbContext _dbContext;

    public TaskService(TaskDbContext dbContext)
    {
        _dbContext = dbContext;
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

    // Тут без lock т.к. используем xmin 
    // xmin предотвратит гонку, эсли будет запущено несколько экземпляров сервиса
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
            return CompleteTaskResult.Completed;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CompleteTaskResult.Conflict;
        }
    }

    //EF Core может выбросить DbUpdateConcurrencyException
    public async Task<bool> DeleteAsync(Guid id)
    {
        var deletedCount = await _dbContext.Tasks
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();

        return deletedCount > 0;
    }
}