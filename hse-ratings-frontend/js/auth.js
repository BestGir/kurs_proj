import { api } from './api.js';

const TOKEN_KEY = 'auth_token';
const ROLE_KEY  = 'auth_role'; // 'admin' | null

export function isLoggedIn() {
  return !!localStorage.getItem(TOKEN_KEY);
}
export function isAdmin() {
  return localStorage.getItem(ROLE_KEY) === 'admin' && isLoggedIn();
}

export function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

export function logout() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(ROLE_KEY);
}

/** Диалог входа: простой prompt -> /auth/login -> сохранить токен */
export async function loginFlow() {
  const pwd = window.prompt('Пароль администратора:');
  if (pwd === null) return false;
  const resp = await api.post('/auth/login', { password: pwd }, { withAuth: false });
  // ожидаем { token: "...", role: "admin" }
  if (!resp || !resp.token) throw new Error('Неверный ответ auth/login');
  localStorage.setItem(TOKEN_KEY, resp.token);
  localStorage.setItem(ROLE_KEY, resp.role || 'admin');
  return true;
}
