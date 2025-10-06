import { el, error } from './utils.js';
import { CoursesView } from './views/courses.js';
import { CourseDetailsView } from './views/courseDetails.js';
import { TeachersView } from './views/teachers.js';
import { TeacherDetailsView } from './views/teacherDetails.js';
import { AdminPage } from './pages-admin.js';

/**
 * Таблица маршрутов SPA.
 * Каждый маршрут задаётся регулярным выражением для hash и фабрикой, создающей DOM-узел (view).
 * Если в regex есть группы (например, {id}), они прокидываются фабрике как аргументы.
 */
const routes = [
  { re: /^#\/?$/,                   make: () => CoursesView() },                 // главная → список курсов
  { re: /^#\/courses$/,             make: () => CoursesView() },                 // /courses → список курсов
  { re: /^#\/courses\/([0-9a-f-]+)$/i, make: (_, id) => CourseDetailsView(id) }, // /courses/{id} → детали курса

  { re: /^#\/teachers$/,            make: () => TeachersView() },                // /teachers → список преподавателей
  { re: /^#\/teachers\/([0-9a-f-]+)$/i, make: (_, id) => TeacherDetailsView(id) }, // /teachers/{id} → детали преподавателя

  { re: /^#\/admin$/,               make: () => AdminPage() },                   // /admin → админ-страница
];

/**
 * Рендерит текущий маршрут в переданный контейнер.
 * 1) Берёт location.hash (если пусто — '#/').
 * 2) Ищет первый совпавший маршрут по regex.
 * 3) Вызывает фабрику view (может быть async), очищает контейнер и вставляет полученный узел.
 * 4) Если произошла ошибка — показывает alert с текстом ошибки.
 * 5) Если ни один маршрут не подошёл — «Страница не найдена».
 */
export async function renderRoute(container) {
  const hash = location.hash || '#/';
  for (const r of routes) {
    const m = hash.match(r.re);
    if (m) {
      try {
        const node = await r.make(...m);
        container.innerHTML = '';
        container.appendChild(node);
        return;
      } catch (e) {
        console.error('route error', e);
        container.innerHTML = '';
        container.appendChild(error('Ошибка маршрута: ' + (e?.message || e)));
        return;
      }
    }
  }
  container.innerHTML = '';
  container.appendChild(error('Страница не найдена'));
}

/**
 * Инициализация роутера:
 * - подписывается на событие 'hashchange'
 * - первоначально рендерит маршрут на основе текущего hash
 */
export function initRouter(container) {
  window.addEventListener('hashchange', () => renderRoute(container));
  renderRoute(container);
}
