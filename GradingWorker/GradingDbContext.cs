using Microsoft.EntityFrameworkCore;

namespace GradingWorker;

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string Status { get; set; } = "Submitted";
    public double? Score { get; set; }
    public string? Feedback { get; set; }
}

public class GradingDbContext : DbContext
{
    public GradingDbContext(DbContextOptions<GradingDbContext> options) : base(options) { }
    public DbSet<Submission> Submissions => Set<Submission>();
}