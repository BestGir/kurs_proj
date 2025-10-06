namespace Hse.Ratings.Domain.Entities
{
    /// <summary>
    /// Доменная сущность «Факультет».
    /// Содержит код и наименование факультета, а также набор связанных образовательных программ.
    /// </summary>
    public class Faculty : BaseEntity
    {
        /// <summary>
        /// Короткий код факультета (уникальный в рамках системы), например «FKN».
        /// </summary>
        public string Code { get; set; } = default!;

        /// <summary>
        /// Полное название факультета.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Навигационное свойство: коллекция образовательных программ,
        /// относящихся к данному факультету.
        /// </summary>
        public ICollection<StudyProgram> Programs { get; set; } = new List<StudyProgram>();
    }
}