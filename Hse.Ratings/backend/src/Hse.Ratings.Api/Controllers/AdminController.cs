using System.Reflection;
using Hse.Ratings.Application.DTOs;
using Hse.Ratings.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Hse.Ratings.Api.Controllers.v1
{
    /// <summary>
    /// Админ-операции: создание курса и связь курс↔преподаватель.
    /// Работает поверх текущих моделей через отражение (без жёстких зависимостей на имена свойств).
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        // Контекст базы данных (EF Core)
        private readonly AppDbContext _db;

        // Внедрение зависимостей через конструктор
        public AdminController(AppDbContext db) => _db = db;

        // ======== DTO ========
        
        // DTO для привязки преподавателя к курсу
        public record LinkDto(Guid TeacherId);

        // ======== helpers: EF metadata / reflection ========

        // Поиск свойства по имени без учёта регистра среди public instance свойств
        private static PropertyInfo? FindPropIgnoreCase(Type t, params string[] candidates)
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var name in candidates)
            {
                var p = props.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (p != null) return p;
            }
            return null;
        }

        // Пытается установить значение одному из свойств-«кандидатов»; при необходимости делает простое приведение типов
        private static void TrySet(object obj, string[] candidateNames, object? value)
        {
            var p = FindPropIgnoreCase(obj.GetType(), candidateNames);
            if (p != null && p.CanWrite)
            {
                // простое приведение типов, если нужно
                if (value != null && p.PropertyType != value.GetType())
                {
                    try
                    {
                        value = Convert.ChangeType(value, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);
                    }
                    catch { /* игнорируем и пробуем присвоить как есть */ }
                }
                p.SetValue(obj, value);
            }
        }

        // Создаёт экземпляр сущности по имени DbSet (через отражение) и возвращает тип сущности
        private static object CreateEntityInstance(DbContext db, string dbSetName, out Type entityType)
        {
            var setProp = db.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals(dbSetName, StringComparison.OrdinalIgnoreCase)
                                     && p.PropertyType.IsGenericType
                                     && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            if (setProp == null)
                throw new InvalidOperationException($"DbSet '{dbSetName}' не найден в контексте.");

            entityType = setProp.PropertyType.GetGenericArguments()[0];
            var instance = Activator.CreateInstance(entityType)
                           ?? throw new InvalidOperationException($"Не удалось создать экземпляр типа {entityType.Name}");
            return instance;
        }

        // Возвращает DbSet<object> для указанного типа сущности (через DbContext.Set<T>())
        private static DbSet<object> GetSet(DbContext db, Type entityType)
        {
            var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(entityType);
            var set = method.Invoke(db, null) ?? throw new InvalidOperationException("DbContext.Set вернул null");
            return (DbSet<object>)set;
        }

        // Ищет тип сущности в модели EF, у которого есть указанные ключевые свойства (по имени/типу)
        private static IEntityType? FindEntityTypeWithKeys(DbContext db, params (string Name, Type Type)[] keyProps)
        {
            return db.Model.GetEntityTypes().FirstOrDefault(et =>
            {
                var clr = et.ClrType;
                return keyProps.All(k =>
                {
                    var p = clr.GetProperty(k.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    return p != null && (k.Type.IsAssignableFrom(p.PropertyType) ||
                                         (Nullable.GetUnderlyingType(p.PropertyType) != null &&
                                          k.Type.IsAssignableFrom(Nullable.GetUnderlyingType(p.PropertyType)!)));
                });
            });
        }

        // ======== API ========

        /// <summary>
        /// Создать курс. Поддерживает поля Title/Name, Description, Schedule, ProgramId (если есть в модели).
        /// </summary>
        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto, CancellationToken ct)
        {
            // создаём сущность типа из DbSet "Courses"
            var courseObj = CreateEntityInstance(_db, "Courses", out var courseType);

            // Id
            TrySet(courseObj, new[] { "Id" }, Guid.NewGuid());

            // Title или Name
            var titleValue = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title!.Trim()
                           : !string.IsNullOrWhiteSpace(dto.Name)  ? dto.Name!.Trim()
                           : null;
            if (titleValue != null)
                TrySet(courseObj, new[] { "Title", "Name" }, titleValue);

            // опциональные поля
            if (!string.IsNullOrWhiteSpace(dto.Description))
                TrySet(courseObj, new[] { "Description" }, dto.Description!.Trim());

            if (!string.IsNullOrWhiteSpace(dto.Schedule))
                TrySet(courseObj, new[] { "Schedule", "Time", "Timing" }, dto.Schedule!.Trim());

            if (dto.ProgramId.HasValue)
                TrySet(courseObj, new[] { "ProgramId", "ProgrammeId" }, dto.ProgramId);

            // добавляем
            var set = GetSet(_db, courseType);
            await set.AddAsync(courseObj, ct);
            await _db.SaveChangesAsync(ct);

            // достаём Id обратно (если у модели название Id другое — вернём объект как есть)
            var idProp = FindPropIgnoreCase(courseType, "Id");
            var id = idProp?.GetValue(courseObj);

            return Created($"/api/v1/courses/{id}", new { id });
        }

        /// <summary>
        /// Привязать преподавателя к курсу (courseId ↔ teacherId).
        /// Стратегия:
        /// 1) Ищем join-таблицу (DbSet), где есть свойства CourseId и TeacherId — добавляем строку.
        /// 2) Если нет — пробуем навигацию Course.Teachers или Teacher.Courses (по имени навигации).
        /// </summary>
        [HttpPost("courses/{courseId:guid}/teachers")]
        public async Task<IActionResult> LinkTeacher(Guid courseId, [FromBody] LinkDto dto, CancellationToken ct)
        {
            // 0) проверим, что такие Course и Teacher есть
            var courseExists = await AnyById("Courses", courseId, ct);
            var teacherExists = await AnyById("Teachers", dto.TeacherId, ct);
            if (!courseExists || !teacherExists) return NotFound("Course or Teacher not found");

            // 1) Пытаемся найти join-тип с полями CourseId + TeacherId
            var linkType = FindEntityTypeWithKeys(_db,
                (Name: "CourseId", Type: typeof(Guid)),
                (Name: "TeacherId", Type: typeof(Guid)));

            if (linkType != null)
            {
                // Создаём запись связи и заполняем FK
                var linkObj = Activator.CreateInstance(linkType.ClrType)
                              ?? throw new InvalidOperationException("Не удалось создать сущность связи");

                TrySet(linkObj, new[] { "CourseId" }, courseId);
                TrySet(linkObj, new[] { "TeacherId" }, dto.TeacherId);

                var set = GetSet(_db, linkType.ClrType);
                // не дублировать связь
                var exists = await ExistsLink(set, courseId, dto.TeacherId, ct);
                if (!exists)
                {
                    await set.AddAsync(linkObj, ct);
                    await _db.SaveChangesAsync(ct);
                }
                return NoContent();
            }

            // 2) Fallback через навигации (если настроены) — по строковым именам
            var linked = await TryAttachViaNavigation(courseId, dto.TeacherId, ct);
            if (linked) return NoContent();

            return BadRequest("Не удалось привязать: не найден тип связи (CourseId, TeacherId) и навигации Course.Teachers / Teacher.Courses.");
        }

        /// <summary>
        /// Отвязать преподавателя от курса.
        /// </summary>
        [HttpDelete("courses/{courseId:guid}/teachers/{teacherId:guid}")]
        public async Task<IActionResult> UnlinkTeacher(Guid courseId, Guid teacherId, CancellationToken ct)
        {
            // 1) через join-таблицу (предпочтительно)
            var linkType = FindEntityTypeWithKeys(_db,
                (Name: "CourseId", Type: typeof(Guid)),
                (Name: "TeacherId", Type: typeof(Guid)));

            if (linkType != null)
            {
                var set = GetSet(_db, linkType.ClrType);
                // ищем и удаляем все совпадения (на случай отсутствия PK)
                var toRemove = await QueryLinks(set, courseId, teacherId).ToListAsync(ct);
                if (toRemove.Count == 0) return NotFound();

                set.RemoveRange(toRemove);
                await _db.SaveChangesAsync(ct);
                return NoContent();
            }

            // 2) Fallback через навигации
            var unlinked = await TryDetachViaNavigation(courseId, teacherId, ct);
            if (unlinked) return NoContent();

            return NotFound("Связь не найдена.");
        }

        // ======== internal helpers for link/unlink ========

        // Проверка наличия сущности с указанным Id в заданном DbSet (по имени «Id» через EF.Property)
        private async Task<bool> AnyById(string dbSetName, Guid id, CancellationToken ct)
        {
            var obj = CreateEntityInstance(_db, dbSetName, out var type);
            var set = GetSet(_db, type);

            // WHERE Id == id (по имени "Id")
            var idProp = FindPropIgnoreCase(type, "Id");
            if (idProp == null) throw new InvalidOperationException($"У типа {type.Name} не найдено свойство Id");

            // строим Linq через EF.Property<Guid>(e, "Id")
            var param = System.Linq.Expressions.Expression.Parameter(typeof(object), "e");
            var converted = System.Linq.Expressions.Expression.Convert(param, type);
            var efProperty = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(Guid));
            var idAccess = System.Linq.Expressions.Expression.Call(efProperty, converted,
                System.Linq.Expressions.Expression.Constant(idProp.Name));
            var eq = System.Linq.Expressions.Expression.Equal(idAccess, System.Linq.Expressions.Expression.Constant(id));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(eq, param);

            return await set.AnyAsync(lambda, ct);
        }

        // Формирует запрос к join-таблице по двум FK: CourseId и TeacherId
        private static IQueryable<object> QueryLinks(DbSet<object> set, Guid courseId, Guid teacherId)
        {
            // фильтр Where(x => EF.Property<Guid>(x, "CourseId") == courseId && EF.Property<Guid>(x, "TeacherId") == teacherId)
            var param = System.Linq.Expressions.Expression.Parameter(typeof(object), "x");
            var courseProp = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(Guid));
            var left1 = System.Linq.Expressions.Expression.Call(courseProp, param, System.Linq.Expressions.Expression.Constant("CourseId"));
            var cond1 = System.Linq.Expressions.Expression.Equal(left1, System.Linq.Expressions.Expression.Constant(courseId));

            var teachProp = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(Guid));
            var left2 = System.Linq.Expressions.Expression.Call(teachProp, param, System.Linq.Expressions.Expression.Constant("TeacherId"));
            var cond2 = System.Linq.Expressions.Expression.Equal(left2, System.Linq.Expressions.Expression.Constant(teacherId));

            var and = System.Linq.Expressions.Expression.AndAlso(cond1, cond2);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(and, param);

            return set.Where(lambda);
        }

        // Проверка существования строки связи (для недопущения дублей)
        private static async Task<bool> ExistsLink(DbSet<object> set, Guid courseId, Guid teacherId, CancellationToken ct)
        {
            return await QueryLinks(set, courseId, teacherId).AnyAsync(ct);
        }

        // Пытается привязать через навигационные коллекции Course.Teachers или Teacher.Courses
        private async Task<bool> TryAttachViaNavigation(Guid courseId, Guid teacherId, CancellationToken ct)
        {
            // Попробуем: Course.Teachers (коллекция) — добавить Teacher
            // или Teacher.Courses — добавить Course
            var courseSetProp = _db.GetType().GetProperty("Courses");
            var teacherSetProp = _db.GetType().GetProperty("Teachers");
            if (courseSetProp == null || teacherSetProp == null) return false;

            var courseSet = (IQueryable<object>)courseSetProp.GetValue(_db)!;
            var teacherSet = (IQueryable<object>)teacherSetProp.GetValue(_db)!;

            var course = await FindById(courseSet, courseId);
            var teacher = await FindById(teacherSet, teacherId);
            if (course == null || teacher == null) return false;

            // ищем коллекции навигаций
            var cTeachers = course.GetType().GetProperties()
                .FirstOrDefault(p => typeof(System.Collections.IList).IsAssignableFrom(p.PropertyType) &&
                                     p.Name.Equals("Teachers", StringComparison.OrdinalIgnoreCase));
            if (cTeachers != null)
            {
                var list = (System.Collections.IList?)cTeachers.GetValue(course);
                if (list != null && !list.Contains(teacher))
                {
                    list.Add(teacher);
                    await _db.SaveChangesAsync(ct);
                    return true;
                }
            }

            var tCourses = teacher.GetType().GetProperties()
                .FirstOrDefault(p => typeof(System.Collections.IList).IsAssignableFrom(p.PropertyType) &&
                                     p.Name.Equals("Courses", StringComparison.OrdinalIgnoreCase));
            if (tCourses != null)
            {
                var list = (System.Collections.IList?)tCourses.GetValue(teacher);
                if (list != null && !list.Contains(course))
                {
                    list.Add(course);
                    await _db.SaveChangesAsync(ct);
                    return true;
                }
            }

            return false;
        }

        // Пытается отвязать через навигационные коллекции Course.Teachers или Teacher.Courses
        private async Task<bool> TryDetachViaNavigation(Guid courseId, Guid teacherId, CancellationToken ct)
        {
            var courseSetProp = _db.GetType().GetProperty("Courses");
            var teacherSetProp = _db.GetType().GetProperty("Teachers");
            if (courseSetProp == null || teacherSetProp == null) return false;

            var courseSet = (IQueryable<object>)courseSetProp.GetValue(_db)!;
            var teacherSet = (IQueryable<object>)teacherSetProp.GetValue(_db)!;

            var course = await FindById(courseSet, courseId);
            var teacher = await FindById(teacherSet, teacherId);
            if (course == null || teacher == null) return false;

            var cTeachers = course.GetType().GetProperties()
                .FirstOrDefault(p => typeof(System.Collections.IList).IsAssignableFrom(p.PropertyType) &&
                                     p.Name.Equals("Teachers", StringComparison.OrdinalIgnoreCase));
            if (cTeachers != null)
            {
                var list = (System.Collections.IList?)cTeachers.GetValue(course);
                if (list != null && list.Contains(teacher))
                {
                    list.Remove(teacher);
                    await _db.SaveChangesAsync(ct);
                    return true;
                }
            }

            var tCourses = teacher.GetType().GetProperties()
                .FirstOrDefault(p => typeof(System.Collections.IList).IsAssignableFrom(p.PropertyType) &&
                                     p.Name.Equals("Courses", StringComparison.OrdinalIgnoreCase));
            if (tCourses != null)
            {
                var list = (System.Collections.IList?)tCourses.GetValue(teacher);
                if (list != null && list.Contains(course))
                {
                    list.Remove(course);
                    await _db.SaveChangesAsync(ct);
                    return true;
                }
            }

            return false;
        }

        // Нахождение объекта по Id через EF.Property<Guid>(e, "Id")
        private static async Task<object?> FindById(IQueryable<object> set, Guid id)
        {
            // set.FirstOrDefault(e => EF.Property<Guid>(e, "Id") == id)
            var param = System.Linq.Expressions.Expression.Parameter(typeof(object), "e");
            var efProperty = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(Guid));
            var idAccess = System.Linq.Expressions.Expression.Call(efProperty, param,
                System.Linq.Expressions.Expression.Constant("Id"));
            var eq = System.Linq.Expressions.Expression.Equal(idAccess, System.Linq.Expressions.Expression.Constant(id));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(eq, param);

            return await set.FirstOrDefaultAsync(lambda);
        }
    }
}
