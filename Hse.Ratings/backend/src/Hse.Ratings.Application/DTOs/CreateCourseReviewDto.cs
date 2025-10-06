namespace Hse.Ratings.Application.DTOs
{
    /// <summary>
    /// DTO (Data Transfer Object) для создания нового отзыва о курсе.
    /// Используется при передаче данных от клиента к API для добавления отзыва студентом.
    /// </summary>
    /// <param name="CourseId">Идентификатор курса, к которому относится отзыв.</param>
    /// <param name="Overall">Общая оценка курса.</param>
    /// <param name="Leniency">Оценка лояльности/строгости оценивания.</param>
    /// <param name="Usefulness">Оценка полезности курса.</param>
    /// <param name="Interest">Оценка интереса/увлекательности курса.</param>
    /// <param name="Comment">Текстовый комментарий автора отзыва (опционально).</param>
    /// <param name="Author">Имя или идентификатор автора отзыва (опционально).</param>
    public record CreateCourseReviewDto(
        Guid CourseId,
        int Overall,
        int Leniency,
        int Usefulness,
        int Interest,
        string? Comment,
        string? Author
    );
}