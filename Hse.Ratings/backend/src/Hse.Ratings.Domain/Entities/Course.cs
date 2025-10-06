namespace Hse.Ratings.Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Курс».
    /// Описывает учебный курс, его принадлежность к программе, базовые атрибуты и навигационные связи.
    /// </summary>
    public class Course : BaseEntity
    {
        /// <summary>
        /// Идентификатор учебной программы, к которой относится курс (опционально).
        /// Может быть null, если курс не привязан к конкретной программе.
        /// </summary>
        public Guid? ProgramId { get; set; }

        /// <summary>
        /// Внутренний/учебный код курса (опционально), например «MATH101».
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Название курса (обязательное поле).
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Краткое описание курса (опционально).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Учебный год, к которому относится курс (например, 2024).
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// Номер семестра в рамках учебного года (например, 1 или 2).
        /// </summary>
        public int Semester { get; set; }

        /// <summary>
        /// Навигационное свойство: учебная программа, к которой относится курс.
        /// Соответствует внешнему ключу <see cref="ProgramId"/>.
        /// </summary>
        public StudyProgram? Program { get; set; }

        /// <summary>
        /// Навигационное свойство: коллекция преподавателей, ведущих данный курс.
        /// </summary>
        public ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

        /// <summary>
        /// Навигационное свойство: отзывы/оценки по данному курсу.
        /// </summary>
        public ICollection<CourseReview> Reviews { get; set; } = new List<CourseReview>();
    }
}