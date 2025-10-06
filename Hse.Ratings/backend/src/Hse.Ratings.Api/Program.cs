using System.Text;
using Hse.Ratings.Infrastructure;
using Hse.Ratings.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ===== DI инфраструктуры (DbContext и т.п.) =====
builder.Services.AddInfrastructure(builder.Configuration);

// ===== Controllers + Swagger (c Bearer) =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите токен вида: Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===== CORS: разрешаем фронт на 5173 =====
const string CorsFrontend = "CorsFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsFrontend, p =>
        p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()
    );
});

// ===== JWT Auth =====
var issuer  = "Hse.Ratings";
var audience = "Hse.Ratings";
var key = builder.Configuration["Jwt:Key"]
          ?? "super_secret_development_key_please_change";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer   = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ===== Dev helpers =====
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// В проде можно включить редирект на HTTPS, локально мешает front'у на http://localhost:5173
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Порядок важен: CORS -> Auth -> Authorization -> MapControllers
app.UseCors(CorsFrontend);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ===== Сидер БД =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

app.Run();
