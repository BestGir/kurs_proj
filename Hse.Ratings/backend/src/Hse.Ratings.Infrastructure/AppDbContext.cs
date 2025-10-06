using Hse.Ratings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Infrastructure
{
    /// <summary>
    /// Главный контекст EF Core для приложения рейтингов.
    /// Описывает наборы сущностей (DbSet) и конфигурацию модели в OnModelCreating.
    /// </summary>
    public class AppDbContext(DbContextOptions<AppDbContext> opt) : DbContext(opt)
    {
        // ---------- Наборы сущностей ----------
        public DbSet<Teacher>       Teachers      => Set<Teacher>();
        public DbSet<Course>        Courses       => Set<Course>();
        public DbSet<Faculty>       Faculties     => Set<Faculty>();
        public DbSet<StudyProgram>  StudyPrograms => Set<StudyProgram>();
        public DbSet<TeacherReview> TeacherReviews=> Set<TeacherReview>();
        public DbSet<CourseReview>  CourseReviews => Set<CourseReview>();

        /// <summary>
        /// Конфигурация модели (ограничения, связи, индексы и т.п.).
        /// </summary>
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ---------- Связь многие-ко-многим: Teacher ↔ Course ----------
            // Хранится в промежуточной таблице CourseTeachers (имя задаём явно).
            b.Entity<Teacher>()
                .HasMany(t => t.Courses)
                .WithMany(c => c.Teachers)
                .UsingEntity(j => j.ToTable("CourseTeachers"));

            // ---------- Teacher ----------
            // Ограничения на длины строк и обязательность полей.
            b.Entity<Teacher>(e =>
            {
                e.Property(x => x.FullName).HasMaxLength(256).IsRequired();
                e.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
            });

            // ---------- Course ----------
            // Название обязательно, код курса опционален.
            b.Entity<Course>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();
                e.Property(x => x.Code).HasMaxLength(64);
            });

            // ---------- Faculty ----------
            // Код и имя факультета — обязательные, с ограничениями длины.
            b.Entity<Faculty>(e =>
            {
                e.Property(x => x.Code).HasMaxLength(32).IsRequired();
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            });

            // ---------- StudyProgram ----------
            // Обязательные Code/Name и явная FK-связь с Faculty.
            b.Entity<StudyProgram>(e =>
            {
                e.Property(x => x.Code).HasMaxLength(32).IsRequired();
                e.Property(x => x.Name).HasMaxLength(256).IsRequired();

                // Явная связь с факультетом (если не была настроена по соглашениям):
                // Program (N) — (1) Faculty
                e.HasOne(p => p.Faculty)
                 .WithMany(f => f.Programs)
                 .HasForeignKey(p => p.FacultyId);
            });

            // ---------- TeacherReview ----------
            // Индекс по TeacherId для быстрых выборок и ограничения по длинам строк.
            b.Entity<TeacherReview>(e =>
            {
                e.HasIndex(x => x.TeacherId);
                e.Property(x => x.Comment).HasMaxLength(4000);
                e.Property(x => x.Author).HasMaxLength(128);
            });

            // ---------- CourseReview ----------
            // Индекс по CourseId для быстрых выборок и ограничения по длинам строк.
            b.Entity<CourseReview>(e =>
            {
                e.HasIndex(x => x.CourseId);
                e.Property(x => x.Comment).HasMaxLength(4000);
                e.Property(x => x.Author).HasMaxLength(128);
            });
        }
    }
}
