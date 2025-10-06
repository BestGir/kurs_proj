using Hse.Ratings.Application.DTOs;
using Hse.Ratings.Domain.Entities;
using Hse.Ratings.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Api.Controllers
{
    /// <summary>
    /// Контроллер для управления курсами:
    /// - список и фильтрация;
    /// - получение деталей (проекции без циклических ссылок);
    /// - создание/обновление/удаление (для администратора);
    /// - привязка/отвязка преподавателей;
    /// - операции с отзывами курса.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CoursesController(AppDbContext db) => _db = db;

        // ================== LIST ==================
        /// <summary>
        /// Получить список курсов с пагинацией и простым поиском по имени.
        /// Параметры: q — подстрока имени (case-insensitive), page (>=1), pageSize (1..200).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            // Нормализация параметров пагинации
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _db.Courses.AsNoTracking();

            // Поиск по подстроке имени (toLower для простоты; для БД лучше использовать полнотекстовый/ILIKE)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(needle));
            }

            // Подсчёт общего количества и выборка страницы
            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                // Безопасная проекция (чтобы не тащить навигации и не ловить циклы в JSON)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.Year,
                    c.Semester,
                    c.Code,
                    c.ProgramId
                })
                .ToListAsync(ct);

            return Ok(new { total, items });
        }

        // ================== DETAILS (safe projection: no cycles) ==================
        /// <summary>
        /// Получить детальную информацию о курсе, включая преподавателей и отзывы.
        /// Возвращает объект с полями course, teachers, reviews (все — проекции).
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOne(Guid id, CancellationToken ct)
        {
            // Основные поля курса
            var course = await _db.Courses
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.Year,
                    c.Semester,
                    c.Code,
                    c.ProgramId
                })
                .FirstOrDefaultAsync(ct);

            if (course is null) return NotFound();

            // Преподаватели курса (используем EF.Property на случай расхождения с именами в модели)
            var teachers = await _db.Teachers
                .AsNoTracking()
                .Where(t => t.Courses.Any(c => c.Id == id))
                .Select(t => new
                {
                    t.Id,
                    // В модели заданы обязательные FullName/DisplayName
                    FullName = EF.Property<string>(t, "FullName"),
                    DisplayName = EF.Property<string>(t, "DisplayName")
                })
                .ToListAsync(ct);

            // Отзывы по курсу (по убыванию Id)
            var reviews = await _db.CourseReviews
                .AsNoTracking()
                .Where(r => r.CourseId == id)
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Overall,
                    r.Leniency,
                    r.Usefulness,
                    r.Interest,
                    r.Comment,
                    r.Author,
                    r.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(new { course, teachers, reviews });
        }

        // ================== CREATE / UPDATE ==================
        /// <summary>
        /// Локальный DTO для создания курса в данном контроллере
        /// (не путать с Application.DTOs — здесь простая форма для Admin API).
        /// </summary>
        public record CreateCourseDto(
            string Name,
            string? Description,
            int Year,
            int Semester,
            Guid? ProgramId,
            string? Code
        );

        /// <summary>
        /// Создать курс (только администратор).
        /// Возвращает Location на ресурс и Id созданного курса.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required");

            var entity = new Course
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Year = dto.Year,
                Semester = dto.Semester,
                ProgramId = dto.ProgramId,
                Code = dto.Code?.Trim()
            };

            _db.Courses.Add(entity);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetOne), new { id = entity.Id }, new { entity.Id });
        }

        /// <summary>
        /// Обновить курс (только администратор). Поддерживаются частичные обновления.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseDto dto, CancellationToken ct)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (course is null) return NotFound();

            // Частичное обновление полей
            if (!string.IsNullOrWhiteSpace(dto.Name)) course.Name = dto.Name!.Trim();
            if (dto.Description is not null) course.Description = dto.Description?.Trim();
            if (dto.Year.HasValue) course.Year = dto.Year.Value;
            if (dto.Semester.HasValue) course.Semester = dto.Semester.Value;
            if (dto.ProgramId.HasValue) course.ProgramId = dto.ProgramId;
            if (dto.Code is not null) course.Code = dto.Code?.Trim();

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // ================== DELETE ==================
        /// <summary>
        /// Удалить курс (только администратор). Сначала удаляются связанные отзывы.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (course is null) return NotFound();

            // Каскадное удаление отзывов (вручную, если в БД не настроен Cascade)
            var reviews = _db.CourseReviews.Where(r => r.CourseId == id);
            _db.CourseReviews.RemoveRange(reviews);

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // ================== LINK: course ↔ teacher (через навигации) ==================
        /// <summary>
        /// DTO для привязки преподавателя к курсу.
        /// </summary>
        public record LinkDto(Guid TeacherId);

        /// <summary>
        /// Привязать преподавателя к курсу (через навигационную коллекцию).
        /// </summary>
        [HttpPost("{courseId:guid}/teachers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LinkTeacher(Guid courseId, [FromBody] LinkDto dto, CancellationToken ct)
        {
            // Загружаем курс вместе с коллекцией Teachers
            var course  = await _db.Courses.Include(c => c.Teachers).FirstOrDefaultAsync(c => c.Id == courseId, ct);
            var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == dto.TeacherId, ct);
            if (course is null || teacher is null) return NotFound("Course or Teacher not found");

            // Добавляем связь, если её ещё нет
            if (!course.Teachers.Any(t => t.Id == teacher.Id))
            {
                course.Teachers.Add(teacher);
                await _db.SaveChangesAsync(ct);
            }
            return NoContent();
        }

        /// <summary>
        /// Отвязать преподавателя от курса (через навигационную коллекцию).
        /// </summary>
        [HttpDelete("{courseId:guid}/teachers/{teacherId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlinkTeacher(Guid courseId, Guid teacherId, CancellationToken ct)
        {
            var course = await _db.Courses.Include(c => c.Teachers).FirstOrDefaultAsync(c => c.Id == courseId, ct);
            if (course is null) return NotFound();

            var toRemove = course.Teachers.FirstOrDefault(t => t.Id == teacherId);
            if (toRemove is null) return NotFound();

            course.Teachers.Remove(toRemove);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // ================== REVIEWS (Course) ==================
        /// <summary>
        /// Получить список отзывов по курсу (упорядочены по убыванию Id).
        /// </summary>
        [HttpGet("{id:guid}/reviews")]
        public async Task<IActionResult> GetReviews(Guid id, CancellationToken ct)
        {
            var list = await _db.CourseReviews
                .Where(r => r.CourseId == id)
                .OrderByDescending(r => r.Id)
                .AsNoTracking()
                .Select(r => new
                {
                    r.Id,
                    r.Overall,
                    r.Leniency,
                    r.Usefulness,
                    r.Interest,
                    r.Comment,
                    r.Author,
                    r.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        /// <summary>
        /// Создать отзыв по курсу (временно AllowAnonymous для локальной проверки без JWT).
        /// </summary>
        [HttpPost("{id:guid}/reviews")]
        [AllowAnonymous] // <— временно для локальной проверки без JWT
        public async Task<IActionResult> CreateReview(Guid id, [FromBody] CreateCourseReviewDto dto, CancellationToken ct)
        {
            // Защита от рассинхронизации пути и тела запроса
            if (dto.CourseId != id) return BadRequest("CourseId mismatch");
            if (!await _db.Courses.AnyAsync(c => c.Id == id, ct)) return NotFound("Course not found");

            var review = new CourseReview
            {
                Id         = Guid.NewGuid(),
                CourseId   = id,
                Overall    = dto.Overall,
                Leniency   = dto.Leniency,
                Usefulness = dto.Usefulness,
                Interest   = dto.Interest,
                Comment    = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment!.Trim(),
                Author     = string.IsNullOrWhiteSpace(dto.Author)  ? null : dto.Author!.Trim()
            };

            _db.CourseReviews.Add(review);
            await _db.SaveChangesAsync(ct);
            return Created($"/api/v1/courses/{id}/reviews/{review.Id}", new { id = review.Id });
        }

        /// <summary>
        /// Удалить отзыв по курсу (только администратор).
        /// </summary>
        [HttpDelete("{id:guid}/reviews/{reviewId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReview(Guid id, Guid reviewId, CancellationToken ct)
        {
            var review = await _db.CourseReviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.CourseId == id, ct);
            if (review is null) return NotFound();

            _db.CourseReviews.Remove(review);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
