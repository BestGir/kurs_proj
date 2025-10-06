namespace Hse.Ratings.Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Образовательная программа».
    /// Связана с факультетом и содержит набор курсов, входящих в программу.
    /// </summary>
    public class StudyProgram : BaseEntity
    {
        /// <summary>
        /// Внешний ключ на факультет, к которому относится программа.
        /// </summary>
        public Guid FacultyId { get; set; }

        /// <summary>
        /// Короткий код программы (уникальный в рамках системы), например «CS-BSc».
        /// </summary>
        public string Code { get; set; } = default!;

        /// <summary>
        /// Полное наименование образовательной программы.
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Навигационное свойство на связанный факультет.
        /// Соответствует внешнему ключу <see cref="FacultyId"/>.
        /// </summary>
        public Faculty? Faculty { get; set; }

        /// <summary>
        /// Навигационное свойство: коллекция курсов, входящих в программу.
        /// </summary>
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}