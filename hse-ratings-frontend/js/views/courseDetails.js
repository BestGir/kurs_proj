import { api } from '../api.js';
import { el, escapeHtml, fmtDate, ok, error } from '../utils.js';
import { isAdmin } from '../auth.js';
import { debounce } from '../utils.js';

export async function CourseDetailsView(courseId) {
  const root = el('div');

  let course = null;
  try {
    course = await api.get(`/courses/${courseId}`);
  } catch (e) {
    root.appendChild(error('Не удалось загрузить курс'));
    return root;
  }

  const c = course.course || course;
  const averages = await getCourseAverages(courseId);

  // шапка: слева описание, справа средние; код/год/семестр НЕ показываем
  root.append(
    el('h1', {class:'h1'}, [escapeHtml(c.name || 'Курс')]),
    el('div', {class:'panel'}, [
      el('div', {class:'split'}, [
        el('div', {class:'col-main'}, [
          c.description ? el('div', {class:'multiline'}, [escapeHtml(c.description)]) : el('div', {class:'card-sub'}, ['Описание отсутствует'])
        ]),
        el('div', {class:'col-side'}, [
          el('div', {class:'avgbox'}, [
            el('div', {class:'avg-title'}, ['Средние оценки']),
            averages
              ? el('div', {class:'avglist'}, [
                  avgItem('Итоговая',       averages.overall),
                  avgItem('Лояльность',     averages.leniency),
                  avgItem('Полезность',     averages.usefulness),
                  avgItem('Интерес',        averages.interest),
                  el('span', {class:'avgcount'}, [`${averages.count} отзыв(ов)`]),
                ])
              : el('div', {class:'card-sub'}, ['Пока нет отзывов'])
          ])
        ])
      ])
    ])
  );

  // Преподаватели курса — над формой и отзывами
  root.appendChild(el('h2', {class:'h2'}, ['Преподаватели курса']));
  const teachersPanel = el('div', {class:'panel'});
  root.appendChild(teachersPanel);
  await renderTeachersBlock(teachersPanel, c, courseId);

  if (isAdmin()) {
    const block = el('div', {class:'panel'});
    root.appendChild(block);
    await renderTeacherLinking(block, courseId);
  }

  // Форма отзыва
  root.appendChild(await renderReviewForm(courseId));

  // Отзывы
  root.appendChild(el('h2', {class:'h2'}, ['Отзывы']));
  try {
    const data = await api.get('/CourseReviews', { query: { courseId } });
    const items = Array.isArray(data) ? data : (data.items || []);
    if (!items.length) {
      root.appendChild(el('div', {class:'panel'}, ['Пока отзывов нет']));
    } else {
      const list = el('div', {class:'list'});
      for (const r of items) {
        const row = el('div', {class:'list-item'});
        row.append(
          el('div', {class:'row gap-m'}, [
            el('strong', {}, [escapeHtml(r.author || 'Аноним')]),
            el('span', {class:'tag tag-muted'}, [fmtDate(r.createdAt)]),
            isAdmin() ? el('button', {class:'btn btn-danger btn-sm', style:'margin-left:auto', onclick: async () => {
              if (!confirm('Удалить отзыв?')) return;
              try {
                await api.delete(`/CourseReviews/${r.id}`);
                row.remove();
              } catch (e) {
                row.appendChild(error('Ошибка удаления: ' + (e?.message||e)));
              }
            }}, ['Удалить']) : null
          ]),
          el('div', {}, [
            `Итоговая: ${r.overall} • Лояльность: ${r.leniency} • Полезность: ${r.usefulness} • Интерес: ${r.interest}`
          ]),
          r.comment ? el('div', {class:'comment'}, [escapeHtml(r.comment)]) : null
        );
        list.appendChild(row);
      }
      root.appendChild(list);
    }
  } catch (e) {
    root.appendChild(error('Не удалось загрузить отзывы'));
  }

  return root;
}

function avgItem(label, value) {
  return el('div', {class:'avgitem'}, [
    el('span', {class:'label'}, [label]),
    el('strong', {}, [String(value)])
  ]);
}

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

/* fallback для учителей, если c.teachers пуст */
async function fetchTeachersByCourseFallback(courseId) {
  try {
    const list = await api.get('/teachers');
    const teachers = Array.isArray(list) ? list : (list.items || []);
    const detailed = await Promise.all(
      teachers.map(t => api.get(`/teachers/${t.id}`).catch(() => null))
    );
    const matched = [];
    for (const d of detailed) {
      if (!d) continue;
      const t = d.teacher || d;
      const courses = t.courses || d.courses || d?.teacher?.courses || [];
      if (Array.isArray(courses) && courses.some(c => String(c.id) === String(courseId))) {
        matched.push({ id: t.id, displayName: t.displayName, fullName: t.fullName });
      }
    }
    return matched;
  } catch {
    return [];
  }
}

async function renderTeachersBlock(panel, c, courseId) {
  panel.innerHTML = '';
  let teachers = c.teachers || [];
  if (!teachers || !teachers.length) teachers = await fetchTeachersByCourseFallback(courseId);

  const wrap = el('div', {class:'list'});
  if (!teachers.length) {
    wrap.append(el('div', {class:'list-item'}, ['Пока никто не привязан']));
  } else {
    for (const t of teachers) {
      const row = el('div', {class:'list-item row gap-m'});
      row.append(
        el('a', {href:`#/teachers/${t.id}`, class:'tag'}, [escapeHtml(t.displayName || t.fullName)]),
        isAdmin() ? el('button', {
          class:'btn btn-danger btn-sm',
          style:'margin-left:auto',
          onclick: async (e) => {
            e.preventDefault();
            if (!confirm('Отвязать преподавателя от курса?')) return;
            try {
              await api.delete(`/courses/${courseId}/teachers/${t.id}`);
              row.remove();
            } catch (err) {
              row.appendChild(error('Не удалось отвязать: ' + (err?.message||err)));
            }
          }
        }, ['Отвязать']) : null
      );
      wrap.append(row);
    }
  }
  panel.append(wrap);
}

async function renderReviewForm(courseId) {
  const form = el('form', {class:'panel'});
  form.append(
    el('h2', {class:'h2'}, ['Оставить отзыв']),
    el('div', {class:'form-grid'}, [
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Итоговая (1–10)', name:'overall', required:true}),
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Лояльность (1–10)', name:'leniency', required:true}),
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Полезность (1–10)', name:'usefulness', required:true}),
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Интерес (1–10)', name:'interest', required:true}),
      el('textarea', {class:'textarea col-2', placeholder:'Комментарий (опц.)', name:'comment'}),
      el('input', {class:'input col-2', placeholder:'Автор (опц.)', name:'author'}),
    ]),
    el('div', {class:'row', style:'margin-top:8px'}, [
      el('button', {type:'submit', class:'btn btn-primary'}, ['Отправить'])
    ])
  );

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    const fd = new FormData(form);
    const dto = {
      CourseId: courseId,
      Overall: Number(fd.get('overall')),
      Leniency: Number(fd.get('leniency')),
      Usefulness: Number(fd.get('usefulness')),
      Interest: Number(fd.get('interest')),
      Comment: (fd.get('comment') || '').trim() || null,
      Author: (fd.get('author') || '').trim() || null,
    };
    try {
      await api.post(`/courses/${courseId}/reviews`, dto, { withAuth: false });
      form.replaceWith(ok('Спасибо! Отзыв отправлен.'));
      setTimeout(() => location.reload(), 700);
    } catch (err) {
      form.appendChild(error('Ошибка отправки: ' + (err?.message || err)));
    }
  });

  return form;
}

async function renderTeacherLinking(block, courseId) {
  block.innerHTML = '';
  const title = el('div', {class:'h2'}, ['Привязать преподавателя по имени']);

  let allTeachers = [];
  try {
    const data = await api.get('/teachers');
    allTeachers = Array.isArray(data) ? data : (data.items || []);
  } catch (e) {
    block.appendChild(error('Не удалось загрузить список преподавателей'));
    return;
  }

  const search = el('input', {class:'input', placeholder:'Начните вводить имя/отображаемое имя...'});
  const results = el('div', {class:'list', style:'margin-top:10px'});

  const renderResults = (q='') => {
    results.innerHTML = '';
    const query = q.trim().toLowerCase();
    if (!query) return;
    const found = allTeachers
      .filter(t => (t.fullName||'').toLowerCase().includes(query) || (t.displayName||'').toLowerCase().includes(query))
      .slice(0, 10);
    if (!found.length) {
      results.appendChild(el('div', {class:'list-item'}, ['Ничего не найдено']));
      return;
    }
    for (const t of found) {
      const row = el('div', {class:'list-item row gap-m'});
      row.append(
        el('div', {class:'card-sub'}, [escapeHtml(t.displayName || t.fullName)]),
        el('button', {class:'btn btn-primary', style:'margin-left:auto', onclick: async () => {
          try {
            await api.post(`/courses/${courseId}/teachers`, { TeacherId: t.id });
            block.appendChild(ok('Преподаватель привязан'));
            setTimeout(()=> location.reload(), 600);
          } catch (e) {
            block.appendChild(error('Ошибка привязки: ' + (e?.message || e)));
          }
        }}, ['Привязать'])
      );
      results.appendChild(row);
    }
  };

  const deb = debounce(() => renderResults(search.value), 250);
  search.addEventListener('input', deb);

  block.append(title, search, results);
}
