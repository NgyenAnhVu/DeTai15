using Microsoft.EntityFrameworkCore;

namespace SubmissionAPI.Data;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

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

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Assignment>().HasData(
            new Assignment { Id = 1, Title = "Bài tập 1: Lập trình hướng sự kiện", Description = "Xây dựng hệ thống nộp bài dùng RabbitMQ", DueDate = DateTime.UtcNow.AddDays(7) },
            new Assignment { Id = 2, Title = "Bài tập 2: Thiết kế RESTful API", Description = "Viết API chuẩn REST với .NET", DueDate = DateTime.UtcNow.AddDays(14) }
        );

        modelBuilder.Entity<Student>().HasData(
            new Student { Id = 1, FullName = "Nguyễn Văn A", Email = "sv1@ictu.edu.vn" },
            new Student { Id = 2, FullName = "Trần Thị B", Email = "sv2@ictu.edu.vn" }
        );
    }
}