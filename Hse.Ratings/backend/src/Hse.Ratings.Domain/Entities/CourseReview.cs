namespace Hse.Ratings.Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Отзыв о курсе».
    /// Содержит агрегированные оценки по нескольким критериям, текстовый комментарий и автора.
    /// </summary>
    public class CourseReview : BaseEntity
    {
        /// <summary>
        /// Внешний ключ на курс, к которому относится отзыв.
        /// </summary>
        public Guid CourseId { get; set; }

        /// <summary>
        /// Итоговая (общая) оценка курса.
        /// Диапазон не задан на уровне модели, контролируется на уровне валидации/БД.
        /// </summary>
        public int Overall { get; set; }

        /// <summary>
        /// Лояльность/строгость оценивания (насколько легко получить высокую оценку).
        /// </summary>
        public int Leniency { get; set; }

        /// <summary>
        /// Полезность курса (практическая ценность знаний).
        /// </summary>
        public int Usefulness { get; set; }

        /// <summary>
        /// Интерес к курсу (насколько материал увлекателен).
        /// </summary>
        public int Interest { get; set; }

        /// <summary>
        /// Дополнительный текстовый комментарий автора отзыва (опционально).
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Имя/идентификатор автора отзыва (опционально).
        /// Хранится в свободной форме, без связи с сущностью пользователя.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Навигационное свойство на связанный курс.
        /// Соответствует внешнему ключу <see cref="CourseId"/>.
        /// </summary>
        public Course? Course { get; set; }
    }
}