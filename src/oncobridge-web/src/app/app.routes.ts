import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'OncoBridge · Import a FHIR Bundle',
    loadComponent: () => import('./import/import-page').then((m) => m.ImportPage),
  },
  {
    path: 'imports/:importBatchId/timeline',
    title: 'OncoBridge · Patient timeline',
    loadComponent: () => import('./timeline/timeline-page').then((m) => m.TimelinePage),
  },
  {
    path: 'imports/:importBatchId',
    title: 'OncoBridge · Import inspector',
    loadComponent: () => import('./inspector/inspector-page').then((m) => m.InspectorPage),
  },
  { path: '**', redirectTo: '' },
];
