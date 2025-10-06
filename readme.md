# HSE Ratings

Веб-приложение для учёта и отображения рейтингов преподавателей и курсов.  
Проект реализован как клиент–серверная система: **.NET 8 backend + JavaScript frontend**.

---

## Стек технологий

### Backend (.NET 8)
- ASP.NET Core Web API  
- Entity Framework Core  
- JWT-аутентификация  
- Слои:
  - **Domain** — сущности (Teacher, Course, Rating, User)  
  - **Application** — DTO, сервисы, интерфейсы  
  - **Infrastructure** — реализация репозиториев, DbContext  
  - **Api** — контроллеры и точки входа

### Frontend (Vanilla JS)
- Чистый JavaScript (без фреймворков)
- Маршрутизация через `router.js`
- REST-запросы через `api.js`
- Хранение токена авторизации в `localStorage`
- Разделение на публичные и административные страницы

---

## Структура проекта

```

СОП 2.0/
├── hse-ratings-frontend/
│   ├── index.html
│   ├── styles.css
│   └── js/
│       ├── config.js
│       ├── api.js
│       ├── app.js
│       ├── router.js
│       ├── auth.js
│       ├── pages-public.js
│       ├── pages-admin.js
│       └── views/
│           ├── teachers.js
│           ├── teacherDetails.js
│           ├── courses.js
│           ├── courseDetails.js
│           ├── login.js
│           └── admin.js
│
└── Hse.Ratings/
├── Hse.Ratings.sln
├── docker-compose.yml
└── backend/src/
├── Hse.Ratings.Api/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── TeacherController.cs
│   │   ├── CourseController.cs
│   │   └── RatingController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── appsettings.Development.json
├── Hse.Ratings.Domain/
│   └── Entities/
│       ├── Teacher.cs
│       ├── Course.cs
│       ├── Rating.cs
│       └── User.cs
├── Hse.Ratings.Application/
└── Hse.Ratings.Infrastructure/
├── HseRatingsDbContext.cs
└── Repositories/

```

---

## Конфигурация

### Backend
Основные настройки:
```

Hse.Ratings/backend/src/Hse.Ratings.Api/appsettings.json
Hse.Ratings/backend/src/Hse.Ratings.Api/appsettings.Development.json

```

Пример строки подключения:
```

"ConnectionStrings": {
"DefaultConnection": "Server=<...>;Database=<...>;User Id=<...>;Password=<...>;"
}

````

JWT:
```json
"Jwt": {
  "Issuer": "HseRatings",
  "Audience": "HseRatingsUsers",
  "SecretKey": "<секретный ключ>"
}
````

### Frontend

Файл `hse-ratings-frontend/js/config.js` содержит адрес API:

```js
export const API_BASE = "http://localhost:5000/api";
```

---


## Основные эндпоинты API

| Метод  | Маршрут              | Описание                        |
| ------ | -------------------- | ------------------------------- |
| POST   | `/api/auth/login`    | Авторизация пользователя        |
| POST   | `/api/auth/register` | Регистрация                     |
| GET    | `/api/teachers`      | Получение списка преподавателей |
| GET    | `/api/teachers/{id}` | Подробности преподавателя       |
| GET    | `/api/courses`       | Получение списка курсов         |
| GET    | `/api/courses/{id}`  | Подробности курса               |
| GET    | `/api/ratings`       | Получение рейтингов             |
| POST   | `/api/ratings`       | Добавление оценки               |
| DELETE | `/api/ratings/{id}`  | Удаление оценки                 |
| GET    | `/api/users`         | Список пользователей (админ)    |


---

## 1) Предварительно

- Установите **Docker Desktop** и запустите его.
- Установите **.NET SDK 8.0+**.

> База работает в Docker, API и фронтенд идут локально на `localhost`.

---

## 2) Поднять базу данных (Docker)

Из папки проекта:
СОП 2.0/СОП 2.0/Hse.Ratings

выполнить:
```bash
docker start hse-pg
docker compose up -d
```
Это поднимет PostgreSQL 16 со следующими параметрами (см. docker-compose.yml):

порт: 5432

база: hse_ratings

пользователь: app

пароль: app

Проверить контейнер:

```bash
docker ps
```
# должен быть контейнер hse-pg
3) Настроить строку подключения (если нужно)
По умолчанию API читает строку подключения так (приоритет):

ConnectionStrings:Default из appsettings.json

переменная окружения HSE_CONNECTION

дефолт:
```
Host=localhost;Port=5432;Database=hse_ratings;Username=app;Password=app
```

В архиве уже есть корректный appsettings.json:

```json
"ConnectionStrings": {
  "Default": "Host=127.0.0.1;Port=5432;Database=hse_ratings;Username=app;Password=app;Pooling=true"
}
```
Хотите переопределить без правки файла — экспортируйте переменную окружения:

```bash
# пример
setx HSE_CONNECTION "Host=127.0.0.1;Port=5432;Database=hse_ratings;Username=app;Password=app"
```
4) Запустить API (ASP.NET Core)
Перейти в:

Hse.Ratings/backend/src/Hse.Ratings.Api

Выполнить:

```bash
dotnet restore
dotnet run
```

5) Запустить фронтенд

перейдите \hse-ratings-frontend\'
```bash
cd '.\hse-ratings-frontend\'
python -m http.server 5173 --bind 127.0.0.1
```

6) Пройдите http://127.0.0.1:5173/

Админ-пароль (seed): admin123.


## Итог

* **Backend:** ASP.NET Core (.NET 8), Entity Framework Core, JWT
* **Frontend:** чистый JavaScript
* **БД:** PostgreSQL / SQL Server
* **Документация API:** Swagger
* **Запуск:** Docker Compose или вручную
* **CI/CD:** пример через GitHub Actions

---

 Автор: *Санников Григорий*


