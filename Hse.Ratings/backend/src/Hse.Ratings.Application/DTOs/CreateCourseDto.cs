namespace Hse.Ratings.Application.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) для создания нового курса.
    /// Используется при передаче данных от клиента к API для добавления записи о курсе.
    /// </summary>
    /// <param name="Title">Название курса (может дублировать Name, зависит от контекста фронтенда).</param>
    /// <param name="Name">Короткое имя или кодовое обозначение курса.</param>
    /// <param name="Description">Описание курса (опционально).</param>
    /// <param name="Schedule">Расписание или дополнительная информация о времени проведения (опционально).</param>
    /// <param name="ProgramId">Идентификатор образовательной программы, к которой относится курс (опционально).</param>
    public record CreateCourseDto(
        string? Title,
        string? Name,
        string? Description,
        string? Schedule,
        Guid? ProgramId
    );
}