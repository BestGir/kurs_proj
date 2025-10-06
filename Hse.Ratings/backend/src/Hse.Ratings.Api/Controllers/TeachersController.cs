
using Hse.Ratings.Application.DTOs;
using Hse.Ratings.Domain.Entities;
using Hse.Ratings.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Api.Controllers.v1
{
    /// <summary>
    /// Контроллер управления преподавателями:
    /// - список и детали (безопасные проекции без циклов),
    /// - создание/обновление/удаление (для администратора),
    /// - получение и управление отзывами по преподавателю.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TeachersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TeachersController(AppDbContext db) => _db = db;

        // ================== LIST ==================
        /// <summary>
        /// Получить список преподавателей (проекция полей через EF.Property).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetList(CancellationToken ct)
        {
            var items = await _db.Teachers
                .AsNoTracking()
                .Select(t => new
                {
                    t.Id,
                    // Читаем сконфигурированные в модели поля по строковому имени
                    FullName = EF.Property<string>(t, "FullName"),
                    DisplayName = EF.Property<string>(t, "DisplayName"),
                    Bio = EF.Property<string?>(t, "Bio")
                })
                .ToListAsync(ct);

            return Ok(items);
        }

        // ================== DETAILS (safe projection: no cycles) ==================
        /// <summary>
        /// Получить детали преподавателя, а также связанные курсы и отзывы (проекции без навигационных циклов).
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOne(Guid id, CancellationToken ct)
        {
            var teacher = await _db.Teachers
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    FullName = EF.Property<string>(t, "FullName"),
                    DisplayName = EF.Property<string>(t, "DisplayName"),
                    Bio = EF.Property<string?>(t, "Bio")
                })
                .FirstOrDefaultAsync(ct);

            if (teacher is null) return NotFound();

            // Курсы, которые ведёт преподаватель
            var courses = await _db.Courses
                .AsNoTracking()
                .Where(c => c.Teachers.Any(t => t.Id == id))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    c.Year,
                    c.Semester
                })
                .ToListAsync(ct);

            // Отзывы о преподавателе
            var reviews = await _db.TeacherReviews
                .AsNoTracking()
                .Where(r => r.TeacherId == id)
                .OrderByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Overall,
                    r.Leniency,
                    r.Knowledge,
                    r.Communication,
                    r.Comment,
                    r.Author,
                    r.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(new { teacher, courses, reviews });
        }

        // ================== CREATE / UPDATE ==================
        /// <summary>
        /// DTO для создания преподавателя.
        /// </summary>
        public record CreateTeacherDto(string FullName, string DisplayName, string? Bio);
        /// <summary>
        /// DTO для обновления преподавателя (частичные обновления).
        /// </summary>
        public record UpdateTeacherDto(string? FullName, string? DisplayName, string? Bio);

        /// <summary>
        /// Создать преподавателя (только администратор).
        /// Поля выставляются через Entry(...).Property("Name").CurrentValue, т.к. они сконфигурированы в OnModelCreating.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateTeacherDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))    return BadRequest("FullName is required");
            if (string.IsNullOrWhiteSpace(dto.DisplayName)) return BadRequest("DisplayName is required");

            // Создаём экземпляр типа, сопоставленного с DbSet<Teachers>
            var entity = Activator.CreateInstance(_db.Teachers.EntityType.ClrType)
                         ?? throw new InvalidOperationException("Cannot create Teacher instance");

            // Заполняем свойства через API отслеживания EF (по строковым именам свойств)
            _db.Entry(entity).Property("Id").CurrentValue = Guid.NewGuid();
            _db.Entry(entity).Property("FullName").CurrentValue = dto.FullName.Trim();
            _db.Entry(entity).Property("DisplayName").CurrentValue = dto.DisplayName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Bio))
                _db.Entry(entity).Property("Bio").CurrentValue = dto.Bio.Trim();

            _db.Attach(entity).State = EntityState.Added;
            await _db.SaveChangesAsync(ct);

            var id = (Guid)_db.Entry(entity).Property("Id").CurrentValue!;
            return Created($"/api/v1/teachers/{id}", new { id });
        }

        /// <summary>
        /// Обновить преподавателя (только администратор). Поддерживаются частичные обновления.
        /// </summary>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeacherDto dto, CancellationToken ct)
        {
            var entity = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (entity is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.FullName))
                _db.Entry(entity).Property("FullName").CurrentValue = dto.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.DisplayName))
                _db.Entry(entity).Property("DisplayName").CurrentValue = dto.DisplayName.Trim();
            if (dto.Bio is not null)
                _db.Entry(entity).Property("Bio").CurrentValue = dto.Bio?.Trim();

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // ================== DELETE ==================
        /// <summary>
        /// Удалить преподавателя (только администратор). Перед удалением чистим связанные отзывы.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var entity = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (entity is null) return NotFound();

            // Удаляем связанные отзывы, чтобы не осталось «сирот»
            var reviews = _db.TeacherReviews.Where(r => r.TeacherId == id);
            _db.TeacherReviews.RemoveRange(reviews);

            _db.Teachers.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        // ================== REVIEWS (Teacher) ==================
        /// <summary>
        /// Получить отзывы о преподавателе (по убыванию Id).
        /// </summary>
        [HttpGet("{id:guid}/reviews")]
        public async Task<IActionResult> GetReviews(Guid id, CancellationToken ct)
        {
            var list = await _db.TeacherReviews
                .Where(r => r.TeacherId == id)
                .OrderByDescending(r => r.Id)
                .AsNoTracking()
                .Select(r => new
                {
                    r.Id,
                    r.Overall,
                    r.Leniency,
                    r.Knowledge,
                    r.Communication,
                    r.Comment,
                    r.Author,
                    r.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(list);
        }

        /// <summary>
        /// Создать отзыв о преподавателе (временно AllowAnonymous для локальной проверки без JWT).
        /// </summary>
        [HttpPost("{id:guid}/reviews")]
        [AllowAnonymous] // <— временно для локальной проверки без JWT
        public async Task<IActionResult> CreateReview(Guid id, [FromBody] CreateTeacherReviewDto dto, CancellationToken ct)
        {
            // Согласование пути и тела запроса + проверка существования преподавателя
            if (dto.TeacherId != id) return BadRequest("TeacherId mismatch");
            if (!await _db.Teachers.AnyAsync(t => t.Id == id, ct)) return NotFound("Teacher not found");

            var review = new TeacherReview
            {
                Id            = Guid.NewGuid(),
                TeacherId     = id,
                Overall       = dto.Overall,
                Leniency      = dto.Leniency,
                Knowledge     = dto.Knowledge,
                Communication = dto.Communication,
                Comment       = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment!.Trim(),
                Author        = string.IsNullOrWhiteSpace(dto.Author)  ? null : dto.Author!.Trim()
            };

            _db.TeacherReviews.Add(review);
            await _db.SaveChangesAsync(ct);
            return Created($"/api/v1/teachers/{id}/reviews/{review.Id}", new { id = review.Id });
        }

        /// <summary>
        /// Удалить отзыв о преподавателе (только администратор).
        /// </summary>
        [HttpDelete("{id:guid}/reviews/{reviewId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReview(Guid id, Guid reviewId, CancellationToken ct)
        {
            var review = await _db.TeacherReviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.TeacherId == id, ct);
            if (review is null) return NotFound();

            _db.TeacherReviews.Remove(review);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }
    }
}
