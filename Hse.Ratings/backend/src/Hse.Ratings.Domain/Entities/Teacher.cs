namespace Hse.Ratings.Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Преподаватель».
    /// Содержит основную информацию о преподавателе, включая ФИО, отображаемое имя, биографию
    /// и связи с курсами и отзывами.
    /// </summary>
    public class Teacher : BaseEntity
    {
        /// <summary>
        /// Полное имя преподавателя (например, «Иванов Иван Иванович»).
        /// </summary>
        public string FullName { get; set; } = default!;

        /// <summary>
        /// Отображаемое имя (может быть сокращённым или предпочтительным вариантом для UI),
        /// например, «Иванов И. И.».
        /// </summary>
        public string DisplayName { get; set; } = default!;

        /// <summary>
        /// Краткая биография или описание преподавателя (опционально).
        /// </summary>
        public string? Bio { get; set; }

        /// <summary>
        /// Навигационное свойство: коллекция курсов, которые ведёт преподаватель.
        /// </summary>
        public ICollection<Course> Courses { get; set; } = new List<Course>();

        /// <summary>
        /// Навигационное свойство: коллекция отзывов, оставленных студентами о преподавателе.
        /// </summary>
        public ICollection<TeacherReview> Reviews { get; set; } = new List<TeacherReview>();
    }
}