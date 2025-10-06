import { API_BASE } from './config.js';

/**
 * Универсальный запрос к API.
 * - path указывать начиная с '/', например: '/teachers'
 * - withAuth: если токен есть в localStorage, положим в Authorization
 */
async function request(method, path, { query = null, body = null, withAuth = true } = {}) {
  // Соберём URL без двойных /api/v1/api/v1
  const base = API_BASE.replace(/\/+$/,'');
  const cleanPath = ('/' + String(path || '').replace(/^\/+/, '')).replace(/\/{2,}/g, '/');
  const url = new URL(base + cleanPath);

  if (query && typeof query === 'object') {
    Object.entries(query).forEach(([k,v]) => {
      if (v !== undefined && v !== null && v !== '') url.searchParams.set(k, String(v));
    });
  }

  const headers = { 'Accept': 'application/json' };
  const token = localStorage.getItem('auth_token');
  if (withAuth && token) headers['Authorization'] = `Bearer ${token}`;
  let bodySerialized = undefined;
  if (body !== null && body !== undefined) {
    headers['Content-Type'] = 'application/json; charset=utf-8';
    bodySerialized = JSON.stringify(body);
  }

  console.debug('[api]', method, url.toString(), body || '');
  const resp = await fetch(url, { method, headers, body: bodySerialized });

  if (!resp.ok) {
    const txt = await resp.text().catch(()=>'');
    console.error('[api] !error', resp.status, txt);
    const err = new Error(`HTTP ${resp.status}`);
    err.status = resp.status;
    err.payload = txt;
    throw err;
  }

  const ct = resp.headers.get('content-type') || '';
  if (ct.includes('application/json')) return await resp.json();
  return await resp.text();
}

export const api = {
  get:    (path, opts) => request('GET',    path, opts),
  post:   (path, body, opts={}) => request('POST', path, {...opts, body}),
  delete: (path, opts) => request('DELETE', path, opts),
};
