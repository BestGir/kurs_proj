using Hse.Ratings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Infrastructure.Seed
{
    /// <summary>
    /// Инициализатор БД тестовыми/стартовыми данными.
    /// Выполняет миграции и добавляет минимальный набор сущностей,
    /// если соответствующие таблицы пусты.
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Заполняет базу данных начальными данными.
        /// Идempotent: каждый блок проверяет наличие записей перед вставкой.
        /// </summary>
        public static async Task SeedAsync(AppDbContext db)
        {
            // Применяем все доступные миграции к БД
            await db.Database.MigrateAsync();

            // ===== 1) Факультет =====
            Guid facultyId;
            if (!await db.Faculties.AnyAsync())
            {
                // Если факультетов нет — создаём дефолтный
                var f = new Faculty
                {
                    Id        = Guid.NewGuid(),
                    Code      = "FCS",
                    Name      = "Факультет компьютерных наук",
                    CreatedAt = DateTime.UtcNow
                };
                db.Faculties.Add(f);
                await db.SaveChangesAsync();
                facultyId = f.Id; // сохраняем Id для связи с программой
            }
            else
            {
                // Берём любой существующий факультет (первый попавшийся)
                facultyId = await db.Faculties.Select(x => x.Id).FirstAsync();
            }

            // ===== 2) Программа =====
            if (!await db.StudyPrograms.AnyAsync())
            {
                // Создаём одну учебную программу, привязанную к найденному/созданному факультету
                var p = new StudyProgram
                {
                    Id        = Guid.NewGuid(),
                    FacultyId = facultyId,
                    Code      = "PMI",
                    Name      = "Прикладная математика и информатика",
                    CreatedAt = DateTime.UtcNow
                };
                db.StudyPrograms.Add(p);
                await db.SaveChangesAsync();
            }

            // ===== 3) Преподаватель =====
            Teacher teacher;
            if (!await db.Teachers.AnyAsync())
            {
                // Если преподавателей нет — добавляем пример
                teacher = new Teacher
                {
                    Id          = Guid.NewGuid(),
                    FullName    = "Иванов Иван Иванович",
                    DisplayName = "Иванов И.И.",
                    Bio         = "К.ф.-м.н., доцент",
                    CreatedAt   = DateTime.UtcNow
                };
                db.Teachers.Add(teacher);
                await db.SaveChangesAsync();
            }
            // Получаем любого преподавателя (для связи с курсом)
            teacher = await db.Teachers.FirstAsync();

            // ===== 4) Курс (пример) =====
            if (!await db.Courses.AnyAsync())
            {
                // Берём любую существующую программу
                var programId = await db.StudyPrograms.Select(x => x.Id).FirstAsync();

                // Создаём пример курса и сразу связываем его с преподавателем
                var course = new Course
                {
                    Id          = Guid.NewGuid(),
                    ProgramId   = programId,
                    Code        = "ALG101",
                    Name        = "Алгоритмы",
                    Description = "Базовый курс по алгоритмам",
                    Year        = 2025,
                    Semester    = 1,
                    CreatedAt   = DateTime.UtcNow
                };

                // Добавляем связь many-to-many через навигацию
                course.Teachers.Add(teacher);
                db.Courses.Add(course);
                await db.SaveChangesAsync();
            }

            // ===== 5) Пример отзывов =====
            if (!await db.TeacherReviews.AnyAsync())
            {
                // Отзыв о преподавателе
                var tr = new TeacherReview
                {
                    Id            = Guid.NewGuid(),
                    TeacherId     = teacher.Id,
                    Overall       = 5,
                    Leniency      = 4,
                    Knowledge     = 5,
                    Communication = 4,
                    Comment       = "Объясняет доступно, отвечает на вопросы",
                    Author        = "Seeder",
                    CreatedAt     = DateTime.UtcNow
                };
                db.TeacherReviews.Add(tr);
            }

            if (!await db.CourseReviews.AnyAsync())
            {
                // Отзыв о курсе (берём любой существующий курс)
                var courseId = await db.Courses.Select(x => x.Id).FirstAsync();
                var cr = new CourseReview
                {
                    Id         = Guid.NewGuid(),
                    CourseId   = courseId,
                    Overall    = 4,
                    Leniency   = 3,
                    Usefulness = 5,
                    Interest   = 4,
                    Comment    = "Материал топ, много практики",
                    Author     = "Seeder",
                    CreatedAt  = DateTime.UtcNow
                };
                db.CourseReviews.Add(cr);
            }

            // Финальный Save для отзывов (и на случай пропущенных изменений)
            await db.SaveChangesAsync();
        }
    }
}
