import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { ActivationCompleted, ActivationTokenStatus, AdminSession, AdminUser, AppStatus, AuditLogEntry, IssuedVpnDeviceCredential, SessionUser, UserDashboard, VpnRequest } from './models';
import { CsrfService } from './csrf.service';

@Injectable({ providedIn: 'root' })
export class PortalApiService {
  private readonly http = inject(HttpClient);
  private readonly csrf = inject(CsrfService);
  private readonly authOptions = { withCredentials: true };

  getStatus(): Observable<AppStatus> {
    return this.http.get<AppStatus>('/api/system/status');
  }

  submitRequest(payload: { email: string; name?: string | null }): Observable<VpnRequest> {
    return this.http.post<VpnRequest>('/api/requests', payload);
  }

  getAdminRequests(): Observable<VpnRequest[]> {
    return this.http.get<VpnRequest[]>('/api/admin/requests', this.authOptions);
  }

  approveRequest(requestId: number, comment?: string | null): Observable<VpnRequest> {
    return this.withCsrf((headers) => this.http.post<VpnRequest>(`/api/admin/requests/${requestId}/approve`, { comment: comment ?? null }, { ...this.authOptions, headers }));
  }

  rejectRequest(requestId: number, comment?: string | null): Observable<VpnRequest> {
    return this.withCsrf((headers) => this.http.post<VpnRequest>(`/api/admin/requests/${requestId}/reject`, { comment: comment ?? null }, { ...this.authOptions, headers }));
  }

  getDashboard(): Observable<UserDashboard> {
    return this.http.get<UserDashboard>('/api/me/dashboard', this.authOptions);
  }

  getActivationStatus(token: string): Observable<ActivationTokenStatus> {
    return this.http.get<ActivationTokenStatus>(`/api/account/activate?token=${encodeURIComponent(token)}`);
  }

  activateAccount(payload: { token: string; password: string }): Observable<ActivationCompleted> {
    return this.http.post<ActivationCompleted>('/api/account/activate', payload);
  }

  loginUser(payload: { login: string; password: string }): Observable<SessionUser> {
    return this.http.post<SessionUser>('/api/auth/login', payload, this.authOptions);
  }

  loginAdmin(payload: { login: string; password: string }): Observable<SessionUser> {
    return this.http.post<SessionUser>('/api/auth/admin/login', payload, this.authOptions);
  }

  getCurrentSession(): Observable<SessionUser> {
    return this.http.get<SessionUser>('/api/auth/me', this.authOptions);
  }

  logout(): Observable<void> {
    return this.withCsrf((headers) => this.http.post<void>('/api/auth/logout', {}, { ...this.authOptions, headers }));
  }

  revokeDevice(deviceId: number): Observable<void> {
    return this.withCsrf((headers) => this.http.delete<void>(`/api/me/devices/${deviceId}`, { ...this.authOptions, headers }));
  }

  issueDeviceCredential(payload: { deviceName: string }): Observable<IssuedVpnDeviceCredential> {
    return this.withCsrf((headers) => this.http.post<IssuedVpnDeviceCredential>('/api/me/devices', payload, { ...this.authOptions, headers }));
  }

  rotateDeviceCredential(deviceId: number): Observable<IssuedVpnDeviceCredential> {
    return this.withCsrf((headers) => this.http.post<IssuedVpnDeviceCredential>(`/api/me/devices/${deviceId}/rotate-credential`, {}, { ...this.authOptions, headers }));
  }

  unbindDeviceIp(deviceId: number): Observable<void> {
    return this.withCsrf((headers) => this.http.delete<void>(`/api/me/devices/${deviceId}/trusted-ip`, { ...this.authOptions, headers }));
  }

  getAdminSessions(): Observable<AdminSession[]> {
    return this.http.get<AdminSession[]>('/api/admin/sessions', this.authOptions);
  }

  disconnectAdminSession(sessionId: number): Observable<void> {
    return this.withCsrf((headers) => this.http.post<void>(`/api/admin/sessions/${sessionId}/disconnect`, {}, { ...this.authOptions, headers }));
  }

  getAuditLog(): Observable<AuditLogEntry[]> {
    return this.http.get<AuditLogEntry[]>('/api/admin/audit', this.authOptions);
  }

  getAdminUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>('/api/admin/users', this.authOptions);
  }

  updateAdminUser(userId: number, payload: { maxDevices: number }): Observable<AdminUser> {
    return this.withCsrf((headers) => this.http.patch<AdminUser>(`/api/admin/users/${userId}`, payload, { ...this.authOptions, headers }));
  }

  setAdminUserStatus(userId: number, active: boolean): Observable<AdminUser> {
    return this.withCsrf((headers) => this.http.post<AdminUser>(`/api/admin/users/${userId}/status`, { active }, { ...this.authOptions, headers }));
  }

  private withCsrf<T>(request: (headers: HttpHeaders) => Observable<T>): Observable<T> {
    return this.csrf.ensureToken().pipe(
      switchMap((token) => request(new HttpHeaders({ 'X-XSRF-TOKEN': token })))
    );
  }
}
