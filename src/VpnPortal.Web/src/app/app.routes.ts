import { Routes } from '@angular/router';
import { ActivateAccountPage } from './pages/activate-account.page';
import { AdminLoginPage } from './pages/admin-login.page';
import { AdminRequestsPage } from './pages/admin-requests.page';
import { adminGuard, userGuard } from './core/auth.guards';
import { DashboardPage } from './pages/dashboard.page';
import { LandingPage } from './pages/landing.page';
import { NotFoundPage } from './pages/not-found.page';
import { UserLoginPage } from './pages/user-login.page';

export const routes: Routes = [
  { path: '', component: LandingPage },
  { path: 'login', component: UserLoginPage },
  { path: 'admin/login', component: AdminLoginPage },
  { path: 'dashboard', component: DashboardPage, canActivate: [userGuard] },
  { path: 'admin', component: AdminRequestsPage, canActivate: [adminGuard] },
  { path: 'activate/:token', component: ActivateAccountPage },
  { path: '**', component: NotFoundPage }
];
