import { addRoute, startRouter } from './router.js';
import { CoursesPage, TeachersPage, CourseDetailsPage, TeacherDetailsPage } from './pages-public.js';
import { LoginPage, AdminDashboardPage, AdminTeachersPage, AdminCoursesPage, renderAuthControls } from './pages-admin.js';
import { whoAmI } from './api.js';

const app = document.getElementById('app');
const authArea = document.getElementById('authArea');
const adminLink = document.getElementById('adminLink');

addRoute('/', CoursesPage);
addRoute('/courses', CoursesPage);
addRoute('/courses/:id', CourseDetailsPage);
addRoute('/teachers', TeachersPage);
addRoute('/teachers/:id', TeacherDetailsPage);
addRoute('/login', LoginPage);
addRoute('/admin', AdminDashboardPage);
addRoute('/admin/teachers', AdminTeachersPage);
addRoute('/admin/courses', AdminCoursesPage);

startRouter(app);

renderAuthControls(authArea);
whoAmI().then(u => {
  if (u?.role === 'Admin') adminLink.classList.remove('hidden');
}).catch(()=>{});
