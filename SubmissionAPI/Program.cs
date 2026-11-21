using Microsoft.EntityFrameworkCore;
using SubmissionAPI.Data;
using SubmissionAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<RabbitMqPublisher>();

var app = builder.Build();

app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/assignments", async (AppDbContext db) =>
{
    var list = await db.Assignments.ToListAsync();
    return Results.Ok(list);
});

app.MapPost("/assignments/{id}/submissions", async (int id, SubmitRequest req, AppDbContext db, RabbitMqPublisher publisher) =>
{
    var submission = new Submission
    {
        AssignmentId = id,
        StudentId = req.StudentId,
        Content = req.Content,
        SubmittedAt = DateTime.UtcNow,
        Status = "Submitted"
    };

    db.Submissions.Add(submission);
    await db.SaveChangesAsync();

    publisher.PublishSubmission(new
    {
        SubmissionId = submission.Id,
        StudentId = submission.StudentId,
        AssignmentId = submission.AssignmentId,
        Content = submission.Content
    });

    return Results.Created($"/submissions/{submission.Id}", submission);
});

app.MapGet("/submissions/{id}", async (int id, AppDbContext db) =>
{
    var sub = await db.Submissions.FindAsync(id);
    return sub is not null ? Results.Ok(sub) : Results.NotFound();
});

app.MapGet("/students/{studentId}/submissions", async (int studentId, AppDbContext db) =>
{
    var list = await db.Submissions
        .Where(s => s.StudentId == studentId)
        .OrderByDescending(s => s.SubmittedAt)
        .ToListAsync();
    return Results.Ok(list);
});

app.Run();

public record SubmitRequest(int StudentId, string Content);