import { api } from '../api.js';
import { el, escapeHtml, error, debounce } from '../utils.js';
import { isAdmin } from '../auth.js';

/** получить средние оценки преподавателя */
async function getTeacherAverages(teacherId) {
  try {
    const data = await api.get('/TeacherReviews', { query: { teacherId } });
    const items = Array.isArray(data) ? data : (data.items || []);
    if (!items.length) return null;
    const sum = items.reduce((a, r) => ({
      overall: a.overall + (r.overall||0),
      leniency: a.leniency + (r.leniency||0),
      knowledge: a.knowledge + (r.knowledge||0),
      communication: a.communication + (r.communication||0),
    }), {overall:0,leniency:0,knowledge:0,communication:0});
    const n = items.length;
    const round = (x)=> Math.round((x/n)*10)/10;
    return {
      overall: round(sum.overall),
      leniency: round(sum.leniency),
      knowledge: round(sum.knowledge),
      communication: round(sum.communication),
      count: n
    };
  } catch { return null; }
}

export async function TeachersView() {
  const root = el('div');
  root.appendChild(el('h1', {class:'h1'}, ['Преподаватели']));

  // строка поиска
  const searchBox = el('input', {class:'input', placeholder:'Поиск преподавателя (имя, отображаемое имя)...'});
  root.appendChild(el('div', {class:'panel'}, [searchBox]));

  // контейнер для карточек
  const grid = el('div', {class:'grid'});
  root.appendChild(grid);

  let allItems = [];
  try {
    const data = await api.get('/teachers');
    allItems = Array.isArray(data) ? data : (data.items || []);
  } catch (e) {
    root.appendChild(error('Ошибка загрузки преподавателей: ' + (e?.message || e)));
    return root;
  }

  async function render(list) {
    grid.innerHTML = '';
    for (const t of list) {
      const card = el('div', {class:'card'});
      const title = escapeHtml(t.displayName || t.fullName);
      const averagesBox = el('div', {class:'row gap-m', style:'margin-top:6px'});
      // асинхронно подтягиваем средние
      getTeacherAverages(t.id).then(avg => {
        if (!avg) return;
        averagesBox.innerHTML = '';
        averagesBox.append(
          el('span', {class:'tag'}, [`Итог: ${avg.overall}`]),
          el('span', {class:'tag'}, [`Лоял: ${avg.leniency}`]),
          el('span', {class:'tag'}, [`Знания: ${avg.knowledge}`]),
          el('span', {class:'tag'}, [`Комм: ${avg.communication}`]),
          el('span', {class:'tag tag-muted'}, [`${avg.count} отзыв(ов)`]),
        );
      });

      card.append(
        el('div', {class:'card-title'}, [title]),
        averagesBox,
        el('div', {class:'row gap-m', style:'margin-top:8px'}, [
          el('a', {href:`#/teachers/${t.id}`, class:'btn btn-primary'}, ['Подробнее']),
          isAdmin() ? el('button', {class:'btn btn-danger', onclick: async () => {
            if (!confirm('Удалить преподавателя?')) return;
            try {
              await api.delete(`/teachers/${t.id}`);
              card.remove();
            } catch (e) {
              card.appendChild(error('Ошибка удаления: ' + (e?.message||e)));
            }
          }}, ['Удалить']) : null
        ])
      );
      grid.appendChild(card);
    }
  }

  // первичный рендер
  await render(allItems);

  // фильтрация
  const doFilter = debounce(() => {
    const q = searchBox.value.trim().toLowerCase();
    if (!q) return render(allItems);
    const filtered = allItems.filter(t => {
      const name = (t.fullName || '').toLowerCase();
      const dn   = (t.displayName || '').toLowerCase();
      return name.includes(q) || dn.includes(q);
    });
    render(filtered);
  }, 250);

  searchBox.addEventListener('input', doFilter);

  return root;
}
