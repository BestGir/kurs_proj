import { api } from '../api.js';
import { el, escapeHtml, error, debounce } from '../utils.js';
import { isAdmin } from '../auth.js';

async function getCourseAverages(courseId) {
  try {
    const data = await api.get('/CourseReviews', { query: { courseId } });
    const items = Array.isArray(data) ? data : (data.items || []);
    if (!items.length) return null;
    const sum = items.reduce((a, r) => ({
      overall: a.overall + (r.overall||0),
      leniency: a.leniency + (r.leniency||0),
      usefulness: a.usefulness + (r.usefulness||0),
      interest: a.interest + (r.interest||0),
    }), {overall:0,leniency:0,usefulness:0,interest:0});
    const n = items.length;
    const round = (x)=> Math.round((x/n)*10)/10;
    return {
      overall: round(sum.overall),
      leniency: round(sum.leniency),
      usefulness: round(sum.usefulness),
      interest: round(sum.interest),
      count: n
    };
  } catch { return null; }
}

export async function CoursesView() {
  const root = el('div');
  root.appendChild(el('h1', {class:'h1'}, ['Курсы']));

  // поиск
  const searchBox = el('input', {class:'input', placeholder:'Поиск курса (название)...'});
  root.appendChild(el('div', {class:'panel'}, [searchBox]));

  const grid = el('div', {class:'grid'});
  root.appendChild(grid);

  let allItems = [];
  try {
    const data = await api.get('/courses', { query: { page: 1, pageSize: 1000 } });
    allItems = Array.isArray(data) ? data : (data.items || []);
  } catch (e) {
    root.appendChild(error('Ошибка загрузки курсов: ' + (e?.message || e)));
    return root;
  }

  async function render(list) {
    grid.innerHTML = '';
    if (!list.length) {
      grid.appendChild(el('div', {class:'card'}, ['Ничего не найдено']));
      return;
    }

    for (const c of list) {
      const card = el('div', {class:'card'});
      const title = escapeHtml(c.name);

      const meta = el('div', {class:'row gap-m'}, []); // код/год/семестр больше не показываем

      const averagesBox = el('div', {class:'row gap-m', style:'margin-top:6px'});

      getCourseAverages(c.id).then(avg => {
        if (!avg) return;
        averagesBox.innerHTML = '';
        averagesBox.append(
          el('span', {class:'tag'}, [`Итог: ${avg.overall}`]),
          el('span', {class:'tag'}, [`Лоял: ${avg.leniency}`]),
          el('span', {class:'tag'}, [`Польза: ${avg.usefulness}`]),
          el('span', {class:'tag'}, [`Интерес: ${avg.interest}`]),
          el('span', {class:'tag tag-muted'}, [`${avg.count} отзыв(ов)`]),
        );
      });

      card.append(
        el('div', {class:'card-title'}, [title]),
        meta,
        averagesBox,
        el('div', {class:'row gap-m', style:'margin-top:8px'}, [
          el('a', {href:`#/courses/${c.id}`, class:'btn btn-primary'}, ['Подробнее']),
          isAdmin() ? el('button', {class:'btn btn-danger', onclick: async () => {
            if (!confirm('Удалить курс?')) return;
            try {
              await api.delete(`/courses/${c.id}`);
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

  await render(allItems);

  const doFilter = debounce(() => {
    const q = (searchBox.value || '').trim().toLowerCase();
    if (!q) return render(allItems);
    const filtered = allItems.filter(c => (c.name || '').toLowerCase().includes(q));
    render(filtered);
  }, 250);

  searchBox.addEventListener('input', doFilter);

  return root;
}
