import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', data: { section: 'dashboard' }, loadComponent: () => import('./route-outlet.component').then((module) => module.RouteOutletComponent) },
  { path: 'systems', data: { section: 'systems' }, loadComponent: () => import('./route-outlet.component').then((module) => module.RouteOutletComponent) },
  { path: 'connectors', data: { section: 'connectors' }, loadComponent: () => import('./route-outlet.component').then((module) => module.RouteOutletComponent) },
  { path: 'workflows', data: { section: 'workflows' }, loadComponent: () => import('./route-outlet.component').then((module) => module.RouteOutletComponent) },
  { path: '**', redirectTo: 'dashboard' }
];
