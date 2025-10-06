// views/login.js — страница входа
import { api } from '../api.js';

export function LoginView() {
  const wrap = document.createElement('div');
  wrap.className = 'page';

  wrap.innerHTML = `
    <div class="page-head">
      <h1>Вход</h1>
      <p class="muted">Введите пароль администратора. Обычные пользователи могут не входить — отзывы доступно оставлять без входа.</p>
    </div>

    <div class="maxw-520">
      <form id="login-form" class="form">
        <label>Пароль администратора
          <input type="password" name="password" placeholder="например, admin123" required />
        </label>
        <button class="btn primary" type="submit">Войти</button>
      </form>
      <div id="msg" class="mt-8 muted"></div>
    </div>
  `;

  wrap.querySelector('#login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const p = (new FormData(e.currentTarget).get('password') || '').trim();
    const msg = wrap.querySelector('#msg');
    msg.textContent = 'Проверяем...';
    try {
      const { token } = await api.login(p);
      if (!token) throw new Error('Токен не получен');
      auth.setToken(token);
      auth.setRole('Admin');        // после удачного логина становимся админом
      msg.textContent = 'Успешно! Переходим в админ-панель...';
      location.hash = '#/admin';
    } catch (err) {
      msg.textContent = 'Ошибка входа: ' + err.message;
    }
  });

  return wrap;
}
