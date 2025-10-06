import { api } from '../api.js';
import { el, escapeHtml, fmtDate, ok, error, debounce, isBlank } from '../utils.js';
import { isAdmin } from '../auth.js';

export async function TeacherDetailsView(teacherId) {
  const root = el('div');

  let teacher;
  try {
    teacher = await api.get(`/teachers/${teacherId}`);
  } catch (e) {
    root.appendChild(error('Не удалось загрузить преподавателя'));
    return root;
  }

  const t = teacher.teacher || teacher;
  const averages = await getTeacherAverages(teacherId);

  // шапка: слева информация и связанные курсы, справа средние
  root.append(
    el('h1', {class:'h1'}, [escapeHtml(t.displayName || t.fullName)]),
    el('div', {class:'panel'}, [
      el('div', {class:'split'}, [
        el('div', {class:'col-main'}, [
          t.bio
            ? el('div', {class:'multiline', style:'white-space:pre-wrap'}, [escapeHtml(t.bio)])
            : el('div', {class:'card-sub'}, ['Информация отсутствует']),
          (teacher.courses?.length || teacher?.teacher?.courses?.length)
            ? el('div', {style:'margin-top:14px'}, [
                el('div', {class:'row gap-m'}, [
                  ...(teacher.courses || teacher.teacher?.courses || [])
                    .map(c => el('a', {href:`#/courses/${c.id}`, class:'tag'}, [escapeHtml(c.name)]))
                ])
              ])
            : el('div', {style:'margin-top:10px'}, ['Курсы пока не привязаны'])
        ]),
        el('div', {class:'col-side'}, [
          el('div', {class:'avgbox'}, [
            el('div', {class:'avg-title'}, ['Средние оценки']),
            averages
              ? el('div', {class:'avglist'}, [
                  avgItem('Итоговая',     averages.overall),
                  avgItem('Лояльность',   averages.leniency),
                  avgItem('Знания',       averages.knowledge),
                  avgItem('Коммуникация', averages.communication),
                  el('span', {class:'avgcount'}, [`${averages.count} отзыв(ов)`]),
                ])
              : el('div', {class:'card-sub'}, ['Пока нет отзывов'])
          ])
        ])
      ])
    ])
  );

  // Форма отзыва
  root.appendChild(await renderReviewForm(teacherId));

  // Отзывы (комментарий рисуем только если НЕ пустой)
  root.appendChild(el('h2', {class:'h2'}, ['Отзывы']));
  try {
    const data = await api.get('/TeacherReviews', { query: { teacherId } });
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
            isAdmin() ? el('button', {
              class:'btn btn-danger btn-sm',
              style:'margin-left:auto',
              onclick: async () => {
                if (!confirm('Удалить отзыв?')) return;
                try {
                  await api.delete(`/TeacherReviews/${r.id}`);
                  row.remove();
                } catch (e) {
                  row.appendChild(error('Ошибка удаления: ' + (e?.message||e)));
                }
              }
            }, ['Удалить']) : null
          ]),
          el('div', {}, [
            `Итоговая: ${r.overall} • Лояльность: ${r.leniency} • Знания: ${r.knowledge} • Коммуникация: ${r.communication}`
          ]),
          !isBlank(r.comment)
            ? el('div', {class:'comment', style:'margin-top:6px; white-space:pre-wrap; overflow-wrap:anywhere'}, [escapeHtml(r.comment)])
            : null
        );
        list.appendChild(row);
      }
      root.appendChild(list);
    }
  } catch (e) {
    root.appendChild(error('Не удалось загрузить отзывы'));
  }

  // Привязка курса по имени
  if (isAdmin()) {
    root.appendChild(el('h2', {class:'h2'}, ['Привязать курс по имени']));
    const panel = el('div', {class:'panel'});
    root.appendChild(panel);

    let allCourses = [];
    try {
      const data = await api.get('/courses', { query: { page:1, pageSize:1000 } });
      allCourses = Array.isArray(data) ? data : (data.items || []);
    } catch {
      panel.appendChild(error('Не удалось загрузить список курсов'));
      return root;
    }

    const search = el('input', {class:'input', placeholder:'Начните вводить название курса...'});
    const results = el('div', {class:'list', style:'margin-top:10px'});

    const renderResults = (q='') => {
      results.innerHTML = '';
      const query = q.trim().toLowerCase();
      if (!query) return;
      const found = allCourses
        .filter(c => (c.name||'').toLowerCase().includes(query))
        .slice(0, 10);
      if (!found.length) {
        results.appendChild(el('div', {class:'list-item'}, ['Ничего не найдено']));
        return;
      }
      for (const c of found) {
        const row = el('div', {class:'list-item row gap-m'});
        row.append(
          el('div', {class:'card-sub'}, [c.name]),
          el('button', {class:'btn btn-primary', style:'margin-left:auto', onclick: async () => {
            try {
              await api.post(`/courses/${c.id}/teachers`, { TeacherId: teacherId });
              panel.appendChild(ok('Связь добавлена'));
              setTimeout(()=> location.reload(), 600);
            } catch (e) {
              panel.appendChild(error('Ошибка привязки: ' + (e?.message||e)));
            }
          }}, ['Привязать'])
        );
        results.appendChild(row);
      }
    };

    const deb = debounce(() => renderResults(search.value), 250);
    search.addEventListener('input', deb);

    panel.append(search, results);
  }

  return root;
}

function avgItem(label, value) {
  return el('div', {class:'avgitem'}, [
    el('span', {class:'label'}, [label]),
    el('strong', {}, [String(value)])
  ]);
}

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

async function renderReviewForm(teacherId) {
  const form = el('form', {class:'panel'});
  form.append(
    el('h2', {class:'h2'}, ['Оставить отзыв']),
    el('div', {class:'form-grid'}, [
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Итоговая (1–10)', name:'overall', required:true}),
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Лояльность (1–10)', name:'leniency', required:true}),
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Знания (1–10)', name:'knowledge', required:true}),
      el('input', {class:'input', type:'number', min:'1', max:'10', placeholder:'Коммуникация (1–10)', name:'communication', required:true}),
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
      TeacherId: teacherId,
      Overall: Number(fd.get('overall')),
      Leniency: Number(fd.get('leniency')),
      Knowledge: Number(fd.get('knowledge')),
      Communication: Number(fd.get('communication')),
      Comment: isBlank(fd.get('comment')) ? null : String(fd.get('comment')).trim(),
      Author:  isBlank(fd.get('author'))  ? null : String(fd.get('author')).trim(),
    };
    try {
      await api.post(`/teachers/${teacherId}/reviews`, dto, { withAuth: false });
      form.replaceWith(ok('Спасибо! Отзыв отправлен.'));
      setTimeout(()=> location.reload(), 700);
    } catch (err) {
      form.appendChild(error('Ошибка отправки: ' + (err?.message || err)));
    }
  });

  return form;
}
