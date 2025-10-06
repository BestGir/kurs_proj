import { qs } from './utils.js';
import { initRouter, renderRoute } from './router.js';
import { loginFlow, logout, isAdmin, isLoggedIn } from './auth.js';

const app = qs('#app');
const btnLogin  = qs('#btnLogin');
const btnLogout = qs('#btnLogout');
const roleBadge = qs('#authRoleBadge');

function refreshAuthUI() {
  const logged = isLoggedIn();
  btnLogin.hidden  = logged;
  btnLogout.hidden = !logged;

  roleBadge.hidden = !logged;
  roleBadge.textContent = isAdmin() ? 'admin' : 'user';
}

// кнопка Войти
btnLogin.addEventListener('click', async () => {
  try {
    const ok = await loginFlow();
    if (ok) {
      refreshAuthUI();
      location.hash = '#/admin'; // после логина — на админ
      await renderRoute(app);
    }
  } catch (e) {
    alert('Не удалось войти: ' + (e?.message || e));
  }
});

// кнопка Выйти
btnLogout.addEventListener('click', async () => {
  logout();
  refreshAuthUI();
  location.hash = '#/teachers'; // после выхода — на преподавателей
  await renderRoute(app);
});

// первый рендер
refreshAuthUI();
initRouter(app);
