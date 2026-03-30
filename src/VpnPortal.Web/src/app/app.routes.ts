import { Routes } from '@angular/router';
import { ActivateAccountPage } from './pages/activate-account.page';
import { AdminAuditPage } from './pages/admin-audit.page';
import { AdminLoginPage } from './pages/admin-login.page';
import { AdminRequestsPage } from './pages/admin-requests.page';
import { AdminRequestHistoryPage } from './pages/admin-request-history.page';
import { AdminSessionsPage } from './pages/admin-sessions.page';
import { AdminUsersPage } from './pages/admin-users.page';
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
  { path: 'admin', redirectTo: 'admin/requests/pending', pathMatch: 'full' },
  { path: 'admin/requests/pending', component: AdminRequestsPage, canActivate: [adminGuard] },
  { path: 'admin/requests/history', component: AdminRequestHistoryPage, canActivate: [adminGuard] },
  { path: 'admin/audit', component: AdminAuditPage, canActivate: [adminGuard] },
  { path: 'admin/sessions', component: AdminSessionsPage, canActivate: [adminGuard] },
  { path: 'admin/accounts', component: AdminUsersPage, canActivate: [adminGuard] },
  { path: 'activate/:token', component: ActivateAccountPage },
  { path: '**', component: NotFoundPage }
];
