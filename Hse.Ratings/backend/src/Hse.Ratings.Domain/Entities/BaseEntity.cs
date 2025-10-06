namespace Hse.Ratings.Domain.Entities
{
    /// <summary>
    /// Базовая сущность доменной модели.
    /// Содержит общий идентификатор и метку времени создания.
    /// Наследуется всеми конкретными сущностями домена.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Уникальный идентификатор сущности (GUID).
        /// Заполняется на уровне приложения/БД при создании записи.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Дата и время создания сущности в формате UTC.
        /// Значение по умолчанию — текущее время UTC на момент инициализации объекта.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}