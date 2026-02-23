using System.Collections.Generic;
using System.Xml.Linq;
using Elmansa_api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Elmansa_api.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // AI and Quota Management
        public DbSet<AIRequestLog> AIRequestLogs { get; set; }
        public DbSet<UsageQuota> UsageQuotas { get; set; }

        // Educational Content
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AIRequestLog
            modelBuilder.Entity<AIRequestLog>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<AIRequestLog>()
                .Property(a => a.Cost)
                .HasColumnType("decimal(10,4)");

            modelBuilder.Entity<AIRequestLog>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_AIRequestLog_UserId");

            modelBuilder.Entity<AIRequestLog>()
                .HasIndex(a => a.CreatedAt)
                .HasDatabaseName("IX_AIRequestLog_CreatedAt");

            modelBuilder.Entity<AIRequestLog>()
                .HasIndex(a => new { a.UserId, a.CreatedAt })
                .HasDatabaseName("IX_AIRequestLog_UserId_CreatedAt");

            modelBuilder.Entity<AIRequestLog>()
                .HasOne(a => a.User)
                .WithMany(u => u.AIRequestLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure UsageQuota
            modelBuilder.Entity<UsageQuota>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<UsageQuota>()
                .HasIndex(u => u.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UsageQuota_UserId");

            modelBuilder.Entity<UsageQuota>()
                .HasOne(u => u.User)
                .WithOne(u => u.UsageQuota)
                .HasForeignKey<UsageQuota>(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Course
            modelBuilder.Entity<Course>()
                .HasKey(c => c.Id);

            modelBuilder.Entity<Course>()
                .Property(c => c.EstimatedHours)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Course>()
                .HasIndex(c => c.Title)
                .HasDatabaseName("IX_Course_Title");

            modelBuilder.Entity<Course>()
                .HasIndex(c => c.IsPublished)
                .HasDatabaseName("IX_Course_IsPublished");

            modelBuilder.Entity<Course>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Lessons)
                .WithOne(l => l.Course)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Course>()
                .HasMany(c => c.Enrollments)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Lesson
            modelBuilder.Entity<Lesson>()
                .HasKey(l => l.Id);

            modelBuilder.Entity<Lesson>()
                .HasIndex(l => l.CourseId)
                .HasDatabaseName("IX_Lesson_CourseId");

            modelBuilder.Entity<Lesson>()
                .HasMany(l => l.Progress)
                .WithOne(p => p.Lesson)
                .HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Enrollment
            modelBuilder.Entity<Enrollment>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Enrollment>()
                .Property(e => e.Grade)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentUserId, e.CourseId })
                .IsUnique()
                .HasDatabaseName("IX_Enrollment_StudentCourse");

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.StudentUser)
                .WithMany()
                .HasForeignKey(e => e.StudentUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Enrollment>()
                .HasMany(e => e.LessonProgress)
                .WithOne(p => p.Enrollment)
                .HasForeignKey(p => p.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure LessonProgress
            modelBuilder.Entity<LessonProgress>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<LessonProgress>()
                .Property(p => p.Score)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<LessonProgress>()
                .HasIndex(p => new { p.StudentUserId, p.LessonId })
                .HasDatabaseName("IX_LessonProgress_StudentLesson");

            modelBuilder.Entity<LessonProgress>()
                .HasOne(p => p.StudentUser)
                .WithMany()
                .HasForeignKey(p => p.StudentUserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LessonProgress>()
                .HasOne(p => p.Lesson)
                .WithMany(l => l.Progress)
                .HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
