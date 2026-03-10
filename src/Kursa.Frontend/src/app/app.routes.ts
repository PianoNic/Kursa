import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: 'onboarding',
    loadComponent: () =>
      import('./features/onboarding/onboarding.component').then((m) => m.OnboardingComponent),
  },
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
      {
        path: 'chat',
        loadComponent: () =>
          import('./features/chat/chat.component').then((m) => m.ChatComponent),
      },
      {
        path: 'quizzes',
        loadComponent: () =>
          import('./features/quizzes/quizzes.component').then((m) => m.QuizzesComponent),
      },
      {
        path: 'flashcards',
        loadComponent: () =>
          import('./features/flashcards/flashcards.component').then((m) => m.FlashcardsComponent),
      },
      {
        path: 'study',
        loadComponent: () =>
          import('./features/study/study.component').then((m) => m.StudyComponent),
      },
    ],
  },
];
