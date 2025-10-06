using Hse.Ratings.Application.DTOs;
using Hse.Ratings.Domain.Entities;
using Hse.Ratings.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Api.Controllers.v1
{
    /// <summary>
    /// Контроллер отзывов о преподавателях:
    /// - список/пагинация (с фильтром по TeacherId);
    /// - получение одного отзыва;
    /// - создание (для авторизованных пользователей);
    /// - удаление (только администратор).
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class TeacherReviewsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TeacherReviewsController(AppDbContext db) => _db = db;

        /// <summary>
        /// Получить список отзывов с пагинацией и опциональной фильтрацией по преподавателю.
        /// Пример: GET /api/v1/teacher-reviews?teacherId={guid}&page=1&pageSize=50
        /// </summary>
        /// <param name="teacherId">Идентификатор преподавателя (опционально).</param>
        /// <param name="page">Номер страницы, минимум 1.</param>
        /// <param name="pageSize">Размер страницы, диапазон 1..200.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Объект с total и items.</returns>
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] Guid? teacherId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            // Нормализуем значения пагинации
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            // Базовый запрос без трекинга
            var q = _db.TeacherReviews.AsNoTracking();

            // Фильтр по преподавателю
            if (teacherId.HasValue && teacherId.Value != Guid.Empty)
                q = q.Where(r => r.TeacherId == teacherId.Value);

            // Считаем общее количество и выбираем страницу (по убыванию Id)
            var total = await q.CountAsync(ct);
            var items = await q.OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Ok(new { total, items });
        }

        /// <summary>
        /// Получить один отзыв по его идентификатору.
        /// Пример: GET /api/v1/teacher-reviews/{id}
        /// </summary>
        /// <param name="id">Идентификатор отзыва.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>Отзыв или 404.</returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOne(Guid id, CancellationToken ct)
        {
            var review = await _db.TeacherReviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
            return review is null ? NotFound() : Ok(review);
        }

        /// <summary>
        /// Создать новый отзыв о преподавателе (нужна авторизация).
        /// Проверяется: заполненность TeacherId и диапазон оценок (1..10).
        /// Пример: POST /api/v1/teacher-reviews
        /// </summary>
        /// <param name="dto">Данные нового отзыва.</param>
        /// <param name="ct">Токен отмены.</param>
        /// <returns>201 с Id созданного отзыва или ошибка.</returns>
        [HttpPost]
        [Authorize] // обычный пользователь может оставлять отзывы
        public async Task<IActionResult> Create([FromBody] CreateTeacherReviewDto dto, CancellationToken ct)
        {
            if (dto.TeacherId == Guid.Empty) return BadRequest("TeacherId is required");

            // Валидация диапазона 1..10
            string? rangeErr = ValidateRange(dto.Overall, dto.Leniency, dto.Knowledge, dto.Communication);
            if (rangeErr is not null) return BadRequest(rangeErr);

            // Проверка существования преподавателя
            var existsTeacher = await _db.Teachers.AnyAsync(t => t.Id == dto.TeacherId, ct);
            if (!existsTeacher) return NotFound("Teacher not found");

            // Маппинг DTO -> сущность
            var review = new TeacherReview
            {
                Id            = Guid.NewGuid(),
                TeacherId     = dto.TeacherId,
                Overall       = dto.Overall,
                Leniency      = dto.Leniency,
                Knowledge     = dto.Knowledge,
                Communication = dto.Communication,
                Comment       = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment!.Trim(),
                Author        = string.IsNullOrWhiteSpace(dto.Author)  ? null : dto.Author!.Trim()
            };

            _db.TeacherReviews.Add(review);
            await _db.SaveChangesAsync(ct);
            return Created($"/api/v1/teacher-reviews/{review.Id}", new { id = review.Id });
        }

        /// <summary>
        /// Удалить отзыв (только администратор).
        /// Пример: DELETE /api/v1/teacher-reviews/{id}
        /// </summary>
        /// <param name="id">Идентификатор отзыва.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var review = await _db.TeacherReviews.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (review is null) return NotFound();

            _db.TeacherReviews.Remove(review);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        /// <summary>
        /// Проверка, что все оценки находятся в диапазоне 1..10.
        /// Возвращает строку ошибки или null при корректных значениях.
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
