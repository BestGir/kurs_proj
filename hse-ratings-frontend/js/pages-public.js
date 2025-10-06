(() => {
  // Рендер публичной домашней страницы (если пользователь не авторизован).
  // Если пользователь авторизован — показываем приветственный блок и ссылки на разделы.
  function render(root){
    const logged = !!auth.getUser(); // авторизован ли пользователь (по наличию токена/пользователя)
    // Переключаем видимость секции логина (если такая есть в layout)
    document.getElementById('loginSection').style.display = logged ? 'none' : 'block';
    // Вставляем контент: для авторизованного — приветствие, иначе — пусто (экран логина снаружи)
    root.innerHTML = logged ? `
      <section class="container">
        <div class="card">
          <h2>Добро пожаловать!</h2>
          <p class="muted">Вы авторизованы. Перейдите в раздел <a href="#/courses">Курсы</a> или <a href="#/teachers">Преподаватели</a>.</p>
        </div>
      </section>` : ``;
  }

  // Хук очистки (если бы навешивались обработчики — тут бы снимали)
  function destroy(){}

  // Регистрируем вью в глобальном реестре views (для роутера)
  window.views = window.views || {};
  window.views.publicHome = { render, destroy };
})();
