using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hse.Ratings.Infrastructure
{
    /// <summary>
    /// Расширения для DI-контейнера приложения (регистрация инфраструктурных сервисов).
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Регистрирует инфраструктуру (в т.ч. DbContext) в DI, читая строку подключения из:
        /// 1) Configuration["ConnectionStrings:Default"],
        /// 2) переменной окружения HSE_CONNECTION,
        /// 3) дефолтного значения (локальный Postgres).
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            // Определяем строку подключения в порядке приоритета
            var cs =
                config.GetConnectionString("Default")
                ?? Environment.GetEnvironmentVariable("HSE_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=hse_ratings;Username=app;Password=app";

            // Регистрируем AppDbContext c провайдером Npgsql (PostgreSQL)
            services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(cs));

            return services;
        }
    }
}