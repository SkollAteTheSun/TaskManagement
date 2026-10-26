using TaskManagement.DTO;
using TaskManagement.Enums;
using TaskManagement.Services;
using TaskManagement.Models;

namespace TaskManagement.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks").WithTags("Tasks");

        group.MapGet("", async (TaskService service) =>
        {
            var tasks = await service.GetAllAsync();

            return Results.Ok(tasks);
        });

        group.MapPost("", async (CreateTaskRequest request, TaskService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
            {
                return Results.BadRequest(
                    "The title must not be empty, its length must not exceed 200 characters.");
            }

            if (request.Priority.HasValue && !Enum.IsDefined(request.Priority.Value))
            {
                return Results.BadRequest("Invalid priority.");
            }

            var task = await service.CreateAsync(request);

            return Results.Created($"/tasks/{task.Id}", task);
        });

        group.MapPut("/{id:guid}/complete", async (Guid id, TaskService service) =>
        {
            var result = await service.CompleteAsync(id);

            return result switch
            {
                CompleteTaskResult.Completed =>
                    Results.NoContent(),

                CompleteTaskResult.NotFound =>
                    Results.NotFound(),

                CompleteTaskResult.Conflict =>
                    Results.Conflict("Task is already completed or was modified."),

                _ => Results.StatusCode(500)
            };
        });

        group.MapDelete("/{id:guid}", async (Guid id, TaskService service) =>
        {
            var deleted = await service.DeleteAsync(id);

            return deleted
                ? Results.NoContent()
                : Results.NotFound();
        });
    }
}