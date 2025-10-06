// js/views/admin.js
(() => {
  console.log('[admin] view loaded');

  const h = (s) => s.replace(/[&<>"']/g, m => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[m]));

  function needAdmin(root) {
    const u = auth.getUser();
    if (!u || u.role !== 'Admin') {
      root.innerHTML = `
        <section class="page">
          <h1>Админ</h1>
          <div class="alert error">Доступ только для администратора. Войдите как админ.</div>
        </section>`;
      return true;
    }
    return false;
  }

  async function createTeacher(payload) {
    const token = auth.getToken();
    const resp = await fetch(`${config.apiBase}/admin/teachers`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify(payload)
    });
    if (!resp.ok) throw new Error(`${resp.status} ${resp.statusText}: ${await resp.text()}`);
    return resp.json();
  }

  async function getAnyProgramId() {
    try {
      const list = await api.get(`${config.apiBase}/programs/options`);
      if (Array.isArray(list) && list.length) return list[0].value;
    } catch {}
    return null;
  }

  async function createCourse(payload) {
    if (!payload.programId) payload.programId = await getAnyProgramId();
    const token = auth.getToken();
    const resp = await fetch(`${config.apiBase}/admin/courses`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      },
      body: JSON.stringify(payload)
    });
    if (!resp.ok) throw new Error(`${resp.status} ${resp.statusText}: ${await resp.text()}`);
    return resp.json();
  }

  function formTeacher() {
    return `
      <form id="form-teacher" class="form card">
        <div class="form-title">Создать преподавателя</div>

        <div class="form-row">
          <label>Полное имя<span class="req">*</span></label>
          <input name="fullName" required placeholder="Иванов Иван Иванович" />
        </div>

        <div class="form-row">
          <label>Отображаемое имя<span class="req">*</span></label>
          <input name="displayName" required placeholder="Иванов И.И." />
        </div>

        <div class="form-row">
          <label>Био</label>
          <textarea name="bio" rows="4" placeholder="Краткая информация о преподавателе"></textarea>
        </div>

        <div class="form-actions">
          <button class="btn primary" type="submit">Создать</button>
        </div>
      </form>
    `;
  }

  function formCourse() {
    return `
      <form id="form-course" class="form card">
        <div class="form-title">Создать курс</div>

        <div class="form-row">
          <label>Код</label>
          <input name="code" placeholder="CS101" />
        </div>

        <div class="form-row">
          <label>Название<span class="req">*</span></label>
          <input name="name" required placeholder="Введение в программирование" />
        </div>

        <div class="form-row">
          <label>Описание</label>
          <textarea name="description" rows="6" placeholder="Короткое описание курса"></textarea>
        </div>

        <div class="form-grid">
          <div class="form-row">
            <label>Год</label>
            <input name="year" type="number" min="2000" max="2100" placeholder="2025" />
          </div>
          <div class="form-row">
            <label>Семестр</label>
            <input name="semester" type="number" min="1" max="12" placeholder="1" />
          </div>
        </div>

        <div class="form-row">
          <label>Преподаватели (GUID через запятую, опционально)</label>
          <input name="teacherIds" placeholder="guid1, guid2" />
        </div>

        <div class="form-actions">
          <button class="btn primary" type="submit">Создать</button>
        </div>
      </form>
    `;
  }

  function attachHandlers(root) {
    const ft = root.querySelector('#form-teacher');
    ft.addEventListener('submit', async (e) => {
      e.preventDefault();
      const fd = new FormData(ft);
      const payload = {
        fullName: (fd.get('fullName') || '').trim(),
        displayName: (fd.get('displayName') || '').trim(),
        bio: (fd.get('bio') || '').trim(),
      };
      ft.classList.add('loading');
      try {
        const res = await createTeacher(payload);
        ft.reset();
        alert(`✅ Преподаватель создан (id: ${res.id})`);
      } catch (err) {
        alert(`❌ Ошибка создания преподавателя: ${err.message}`);
      } finally {
        ft.classList.remove('loading');
      }
    });

    const fc = root.querySelector('#form-course');
    fc.addEventListener('submit', async (e) => {
      e.preventDefault();
      const fd = new FormData(fc);
      const teacherIdsRaw = (fd.get('teacherIds') || '').trim();
      const payload = {
        code: (fd.get('code') || '').trim() || null,
        name: (fd.get('name') || '').trim(),
        description: (fd.get('description') || '').trim() || null,
        year: fd.get('year') ? Number(fd.get('year')) : null,
        semester: fd.get('semester') ? Number(fd.get('semester')) : null,
        programId: null,
        teacherIds: teacherIdsRaw
          ? teacherIdsRaw.split(',').map(x => x.trim()).filter(Boolean)
          : []
      };

      if (!payload.name) {
        alert('Укажите название курса');
        return;
      }

      fc.classList.add('loading');
      try {
        const res = await createCourse(payload);
        fc.reset();
        alert(`✅ Курс создан (id: ${res.id})`);
      } catch (err) {
        alert(`❌ Ошибка создания курса: ${err.message}`);
      } finally {
        fc.classList.remove('loading');
      }
    });
  }

  async function render(root) {
    root.innerHTML = `
      <section class="page">
        <h1>Админ</h1>
        <div class="muted" id="guard">Проверка прав…</div>
      </section>
    `;
    if (needAdmin(root)) return;

    root.innerHTML = `
      <section class="page">
        <h1>Админ</h1>
        <div class="admin-grid">
          ${formTeacher()}
          ${formCourse()}
        </div>
      </section>
    `;
    attachHandlers(root);
  }

  window.views = window.views || {};
  window.views.admin = { render };
})();
