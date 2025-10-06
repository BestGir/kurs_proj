using Hse.Ratings.Application.DTOs;
using Hse.Ratings.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Api.Controllers
{
    /// <summary>
    /// Контроллер для работы со справочником факультетов.
    /// Предоставляет эндпоинты для получения списков, пригодных для выпадающих списков (options) на фронтенде.
    /// </summary>
    [ApiController]
    [Route("api/v1/faculties")]
    public class FacultiesController : ControllerBase
    {
        // Контекст базы данных (EF Core)
        private readonly AppDbContext _db;

        // Внедрение контекста через конструктор
        public FacultiesController(AppDbContext db) => _db = db;

        /// <summary>
        /// Получить список факультетов в формате OptionDto (Value/Label) для UI-компонентов.
        /// GET /api/v1/faculties/options
        /// </summary>
        /// <returns>Коллекция элементов (Value = код факультета, Label = название факультета).</returns>
        [HttpGet("options")]
        public async Task<ActionResult<IEnumerable<OptionDto>>> Options()
        {
            // Без трекинга, сортировка по имени, проекция в OptionDto.
            var items = await _db.Faculties
                .AsNoTracking()
                .OrderBy(f => f.Name)
                .Select(f => new OptionDto(
                    f.Code,          // Value — короткий код факультета
                    f.Name           // Label — человекочитаемое название
                ))
                .ToListAsync();

            return Ok(items);
        }
    }
}