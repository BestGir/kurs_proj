import { api } from './api.js';
import { el, ok, error } from './utils.js';
import { isAdmin } from './auth.js';

export async function AdminPage() {
  const root = el('div');
  root.appendChild(el('h1', {class:'h1'}, ['Админ']));

  if (!isAdmin()) {
    root.appendChild(el('div', {class:'panel'}, [
      el('div', {}, ['У вас нет прав администратора']),
      el('div', {class:'card-sub', style:'margin-top:8px'}, ['Чтобы войти как админ — нажмите «Войти» в правом верхнем углу и введите пароль.'])
    ]));
    return root;
  }

  // широкая сетка админки
  const grid = el('div', {class:'admin-grid'});

  // --- Форма преподавателя
  const teacherCard = el('div', {class:'card'});
  teacherCard.appendChild(el('div', {class:'h2'}, ['Создать преподавателя']));
  const tForm = el('form', {class:'form-grid'});
  tForm.append(
    el('input',  {class:'input col-2', name:'fullName',    placeholder:'Полное имя*',       required:true}),
    el('input',  {class:'input col-2', name:'displayName', placeholder:'Отображаемое имя*', required:true}),
    el('textarea',{class:'textarea col-2', name:'bio',     placeholder:'Информация'}),
    el('div', {class:'col-2 row'}, [
      el('button', {type:'submit', class:'btn btn-primary'}, ['Создать'])
    ])
  );
  tForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const fd = new FormData(tForm);
    const dto = {
      FullName: (fd.get('fullName') || '').trim(),
      DisplayName: (fd.get('displayName') || '').trim(),
      Bio: (fd.get('bio') || '').trim() || null,
    };
    try {
      await api.post('/teachers', dto);
      teacherCard.appendChild(ok('Преподаватель создан'));
      tForm.reset();
    } catch (err) {
      teacherCard.appendChild(error('Ошибка: ' + (err?.message || err)));
    }
  });
  teacherCard.appendChild(tForm);
  grid.appendChild(teacherCard);

  // --- Форма курса (только название, описание, программа опц.)
  const courseCard = el('div', {class:'card'});
  courseCard.appendChild(el('div', {class:'h2'}, ['Создать курс']));
  const cForm = el('form', {class:'form-grid'});
  cForm.append(
    el('input',  {class:'input col-2', name:'name',        placeholder:'Название*', required:true}),
    el('textarea',{class:'textarea col-2', name:'description', placeholder:'Описание'}),
    el('input',  {class:'input col-2', name:'programId', placeholder:'ID программы (опц.)'}),
    el('div', {class:'col-2 row'}, [
      el('button', {type:'submit', class:'btn btn-primary'}, ['Создать'])
    ])
  );
  cForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const fd = new FormData(cForm);
    const dto = {
      Name: (fd.get('name') || '').trim(),
      Description: (fd.get('description') || '').trim() || null,
    };
    const programIdRaw = (fd.get('programId') || '').trim();
    if (programIdRaw) dto.ProgramId = programIdRaw;

    try {
      await api.post('/courses', dto);
      courseCard.appendChild(ok('Курс создан'));
      cForm.reset();
    } catch (err) {
      courseCard.appendChild(error('Ошибка: ' + (err?.message || err)));
    }
  });
  courseCard.appendChild(cForm);
  grid.appendChild(courseCard);

  root.appendChild(grid);
  return root;
}
