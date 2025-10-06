using Hse.Ratings.Application.DTOs;
using Hse.Ratings.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hse.Ratings.Api.Controllers
{
    /// <summary>
    /// Контроллер для работы со списком образовательных программ.
    /// Предоставляет удобный эндпоинт для получения опций (Value/Label) с необязательной фильтрацией по коду факультета.
    /// </summary>
    [ApiController]
    [Route("api/v1/programs")]
    public class ProgramsController : ControllerBase
    {
        // Контекст базы данных (EF Core)
        private readonly AppDbContext _db;

        // Внедрение контекста через конструктор
        public ProgramsController(AppDbContext db) => _db = db;

        /// <summary>
        /// Получить список программ в формате <see cref="OptionDto"/> для выпадающих списков на UI.
        /// Поддерживается фильтрация по коду факультета (facultyCode).
        /// Пример: GET /api/v1/programs/options?facultyCode=FCS
        /// </summary>
        /// <param name="facultyCode">
        /// Необязательный код факультета (например, "FCS"). Если указан, возвращаются только программы данного факультета.
        /// </param>
        /// <returns>
        /// Коллекция элементов, где Value = <c>Id</c> программы (в строковом виде),
        /// а Label = комбинация "<c>Faculty.Code/Program.Code — Program.Name</c>".
        /// </returns>
        [HttpGet("options")]
        public async Task<ActionResult<IEnumerable<OptionDto>>> Options([FromQuery] string? facultyCode = null)
        {
            // Базовый запрос по программам без трекинга. Подтягиваем Faculty, чтобы иметь код факультета в Label.
            IQueryable<Domain.Entities.StudyProgram> q = _db.StudyPrograms
                .AsNoTracking()
                .Include(p => p.Faculty);   // чтобы взять код факультета

            // Опциональная фильтрация по коду факультета
            if (!string.IsNullOrWhiteSpace(facultyCode))
                q = q.Where(p => p.Faculty.Code == facultyCode);

            // Сортируем по коду программы и проецируем в OptionDto.
            // В качестве Value используем строковое представление Id, в Label — "FacultyCode/ProgramCode — ProgramName".
            var items = await q
                .OrderBy(p => p.Code)
                .Select(p => new OptionDto(
                    p.Id.ToString(),                          // Value
                    p.Faculty.Code + "/" + p.Code + " — " + p.Name  // Label (без интерполяции)
                ))
                .ToListAsync();

            return Ok(items);
        }
    }
}
