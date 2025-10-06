namespace Hse.Ratings.Application.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) для создания нового отзыва о преподавателе.
    /// Используется при передаче данных от клиента к API для добавления отзыва студентом.
    /// </summary>
    /// <param name="TeacherId">Идентификатор преподавателя, к которому относится отзыв.</param>
    /// <param name="Overall">Общая оценка преподавателя.</param>
    /// <param name="Leniency">Оценка лояльности/строгости преподавателя.</param>
    /// <param name="Knowledge">Оценка уровня знаний преподавателя.</param>
    /// <param name="Communication">Оценка коммуникабельности и умения доносить материал.</param>
    /// <param name="Comment">Текстовый комментарий автора отзыва (опционально).</param>
    /// <param name="Author">Имя или идентификатор автора отзыва (опционально).</param>
    public record CreateTeacherReviewDto(
        Guid TeacherId,
        int Overall,
        int Leniency,
        int Knowledge,
        int Communication,
        string? Comment,
        string? Author
    );
}