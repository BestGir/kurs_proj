namespace Hse.Ratings.Application.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) для обновления информации о курсе.
    /// Используется при передаче данных от клиента к API для редактирования существующего курса.
    /// Все поля являются опциональными, что позволяет обновлять только нужные свойства.
    /// </summary>
    /// <param name="Name">Новое название курса (опционально).</param>
    /// <param name="Description">Новое описание курса (опционально).</param>
    /// <param name="Year">Учебный год курса (опционально).</param>
    /// <param name="Semester">Номер семестра (опционально).</param>
    /// <param name="ProgramId">Идентификатор образовательной программы, к которой привязан курс (опционально).</param>
    /// <param name="Code">Код курса (опционально).</param>
    public record UpdateCourseDto(
        string? Name,
        string? Description,
        int? Year,
        int? Semester,
        Guid? ProgramId,
        string? Code
    );
}