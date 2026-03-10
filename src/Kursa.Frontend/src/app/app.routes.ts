import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'courses',
        loadComponent: () =>
          import('./features/courses/courses.component').then((m) => m.CoursesComponent),
      },
      {
        path: 'courses/:courseId',
        loadComponent: () =>
          import('./features/courses/course-detail.component').then((m) => m.CourseDetailComponent),
      },
      {
        path: 'pinned',
        loadComponent: () =>
          import('./features/pinned/pinned.component').then((m) => m.PinnedComponent),
      },
    ],
  },
];
