using Hse.Ratings.Application.DTOs;
using Hse.Ratings.Domain.Entities;
using Hse.Ratings.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Api.Controllers
{
    /// <summary>
    /// Контроллер для управления отзывами о курсах:
    /// - список/пагинация,
    /// - получение одного отзыва,
    /// - создание,
    /// - удаление (только админ).
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CourseReviewsController(AppDbContext db) : ControllerBase
    {
        /// <summary>
        /// Получить список отзывов с пагинацией и опциональной фильтрацией по CourseId.
        /// Пример: GET /api/v1/course-reviews?courseId={guid}&page=1&pageSize=50
        /// </summary>
        /// <param name="courseId">Фильтр по идентификатору курса (опционально).</param>
        /// <param name="page">Номер страницы (минимум 1).</param>
        /// <param name="pageSize">Размер страницы (1..200).</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Объект с полями total (всего записей) и items (элементы текущей страницы).</returns>
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] Guid? courseId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            // Нормализация параметров пагинации
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            // Базовый запрос без трекинга (только чтение)
            var q = db.CourseReviews.AsNoTracking();

            // Фильтрация по курсу при наличии courseId
            if (courseId.HasValue && courseId.Value != Guid.Empty)
                q = q.Where(r => r.CourseId == courseId.Value);

            // Подсчёт общего количества и выборка текущей страницы (по убыванию Id)
            var total = await q.CountAsync(ct);
            var items = await q.OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Ok(new { total, items });
        }

        /// <summary>
        /// Получить один отзыв по идентификатору.
        /// Пример: GET /api/v1/course-reviews/{id}
        /// </summary>
        /// <param name="id">Идентификатор отзыва.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOne(Guid id, CancellationToken ct)
        {
            var review = await db.CourseReviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
            return review is null ? NotFound() : Ok(review);
        }

        /// <summary>
        /// Создать новый отзыв о курсе (требуется авторизация).
        /// Валидируются: наличие CourseId и диапазон оценок 1..10.
        /// Пример: POST /api/v1/course-reviews
        /// </summary>
        /// <param name="dto">Данные нового отзыва.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpPost]
        [Authorize] // обычный пользователь может оставлять отзывы
        public async Task<IActionResult> Create([FromBody] CreateCourseReviewDto dto, CancellationToken ct)
        {
            if (dto.CourseId == Guid.Empty) return BadRequest("CourseId is required");

            // Простая валидация диапазона 1..10
            string? rangeErr = ValidateRange(dto.Overall, dto.Leniency, dto.Usefulness, dto.Interest);
            if (rangeErr is not null) return BadRequest(rangeErr);

            // Проверяем, что курс существует
            var existsCourse = await db.Courses.AnyAsync(c => c.Id == dto.CourseId, ct);
            if (!existsCourse) return NotFound("Course not found");

            // Маппинг DTO -> сущность
            var review = new CourseReview
            {
                Id         = Guid.NewGuid(),
                CourseId   = dto.CourseId,
                Overall    = dto.Overall,
                Leniency   = dto.Leniency,
                Usefulness = dto.Usefulness,
                Interest   = dto.Interest,
                Comment    = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment!.Trim(),
                Author     = string.IsNullOrWhiteSpace(dto.Author)  ? null : dto.Author!.Trim()
            };

            db.CourseReviews.Add(review);
            await db.SaveChangesAsync(ct);
            return Created($"/api/v1/course-reviews/{review.Id}", new { id = review.Id });
        }

        /// <summary>
        /// Удалить отзыв (только администратор).
        /// Пример: DELETE /api/v1/course-reviews/{id}
        /// </summary>
        /// <param name="id">Идентификатор отзыва.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var review = await db.CourseReviews.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (review is null) return NotFound();

            db.CourseReviews.Remove(review);
            await db.SaveChangesAsync(ct);
            return NoContent();
        }

        /// <summary>
        /// Валидация, что все числовые поля оценок лежат в диапазоне 1..10.
        /// Возвращает строку-ошибку или null при корректных значениях.
        /// </summary>
        private static string? ValidateRange(params int[] values)
        {
            foreach (var v in values)
            {
                if (v < 1 || v > 10) return "All rating fields must be in range 1..10";
            }
            return null;
        }
    }
}
