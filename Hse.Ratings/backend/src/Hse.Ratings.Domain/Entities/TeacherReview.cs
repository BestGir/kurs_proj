namespace Hse.Ratings.Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Отзыв о преподавателе».
    /// Включает критерии оценивания, текстовый комментарий и автора.
    /// </summary>
    public class TeacherReview : BaseEntity
    {
        /// <summary>
        /// Внешний ключ на преподавателя, к которому относится отзыв.
        /// </summary>
        public Guid TeacherId { get; set; }

        /// <summary>
        /// Итоговая (общая) оценка преподавателя.
        /// </summary>
        public int Overall { get; set; }

        /// <summary>
        /// Лояльность/строгость оценивания (насколько легко получить высокую оценку).
        /// </summary>
        public int Leniency { get; set; }

        /// <summary>
        /// Компетентность/уровень знаний преподавателя.
        /// </summary>
        public int Knowledge { get; set; }

        /// <summary>
        /// Коммуникация/навыки донесения материала.
        /// </summary>
        public int Communication { get; set; }

        /// <summary>
        /// Текстовый комментарий к отзыву (опционально).
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Имя/идентификатор автора отзыва (опционально).
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Навигационное свойство на связанного преподавателя.
        /// Соответствует внешнему ключу <see cref="TeacherId"/>.
        /// </summary>
        public Teacher? Teacher { get; set; }
    }
}