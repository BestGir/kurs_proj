// Утилиты выборки DOM-элементов
export const qs = (sel, root = document) => root.querySelector(sel);                 // первый элемент по селектору
export const qsa = (sel, root = document) => Array.from(root.querySelectorAll(sel)); // все элементы по селектору (в массив)

// Экранирование HTML-спецсимволов (для безопасного вывода пользовательского ввода в HTML)
export function escapeHtml(input) {
  if (input === null || input === undefined) return '';
  return String(input)
    .replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;')
    .replace(/"/g,'&quot;').replace(/'/g,'&#39;').replace(/`/g,'&#96;');
}

// Форматирование даты из ISO-строки в локальный формат (ru-RU), c fallback при ошибке
export function fmtDate(iso) {
  if (!iso) return '';
  try {
    const d = new Date(iso);
    return d.toLocaleString('ru-RU', {year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'});
  } catch { return String(iso); }
}

// Создание DOM-узла с атрибутами и дочерними элементами
// attrs: поддержка 'class', 'text', а также обработчиков событий вида onClick/onInput...
export function el(tag, attrs = {}, children = []) {
  const node = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (k === 'class') node.className = v;
    else if (k === 'text') node.textContent = v;
    else if (k.startsWith('on') && typeof v === 'function') node.addEventListener(k.slice(2), v);
    else node.setAttribute(k, v);
  }
  for (const c of [].concat(children)) {
    if (c == null) continue;
    node.appendChild(typeof c === 'string' ? document.createTextNode(c) : c);
  }
  return node;
}

// Готовые элементы уведомлений (успех/ошибка)
export const ok    = (t) => el('div', {class:'alert alert-ok'}, [t]);
export const error = (t) => el('div', {class:'alert'}, [t]);

// Debounce: откладывает вызов fn до тишины ms миллисекунд
export function debounce(fn, ms = 300) {
  let t; return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), ms); };
}

/** Проверка на "пустое" значение (null/undefined/пустая строка/пробелы/"null"/"undefined") */
export function isBlank(v) {
  if (v === null || v === undefined) return true;
  const s = String(v).trim().toLowerCase();
  return s === '' || s === 'null' || s === 'undefined';
}
