import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ActivationCompleted, ActivationTokenStatus, AdminSession, AdminUser, AppStatus, AuditLogEntry, IpConfirmationRequestResult, IssuedVpnDeviceCredential, SessionUser, UserDashboard, VpnRequest } from './models';

@Injectable({ providedIn: 'root' })
export class PortalApiService {
  private readonly http = inject(HttpClient);
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
    return this.http.post<VpnRequest>(`/api/admin/requests/${requestId}/approve`, { comment: comment ?? null }, this.authOptions);
  }

  rejectRequest(requestId: number, comment?: string | null): Observable<VpnRequest> {
    return this.http.post<VpnRequest>(`/api/admin/requests/${requestId}/reject`, { comment: comment ?? null }, this.authOptions);
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
    return this.http.post<void>('/api/auth/logout', {}, this.authOptions);
  }

  revokeDevice(deviceId: number): Observable<void> {
    return this.http.delete<void>(`/api/me/devices/${deviceId}`, this.authOptions);
  }

  issueDeviceCredential(payload: { deviceName: string; deviceType: string; platform: string }): Observable<IssuedVpnDeviceCredential> {
    return this.http.post<IssuedVpnDeviceCredential>('/api/me/devices', payload, this.authOptions);
  }

  rotateDeviceCredential(deviceId: number): Observable<IssuedVpnDeviceCredential> {
    return this.http.post<IssuedVpnDeviceCredential>(`/api/me/devices/${deviceId}/rotate-credential`, {}, this.authOptions);
  }

  requestIpConfirmation(payload: { requestedIp: string; deviceId?: number | null }): Observable<IpConfirmationRequestResult> {
    return this.http.post<IpConfirmationRequestResult>('/api/me/ip-confirmations/request', payload, this.authOptions);
  }

  confirmIp(token: string): Observable<void> {
    return this.http.post<void>(`/api/me/ip-confirmations/${encodeURIComponent(token)}/confirm`, {}, this.authOptions);
  }

  getAdminSessions(): Observable<AdminSession[]> {
    return this.http.get<AdminSession[]>('/api/admin/sessions', this.authOptions);
  }

  disconnectAdminSession(sessionId: number): Observable<void> {
    return this.http.post<void>(`/api/admin/sessions/${sessionId}/disconnect`, {}, this.authOptions);
  }

  getAuditLog(): Observable<AuditLogEntry[]> {
    return this.http.get<AuditLogEntry[]>('/api/admin/audit', this.authOptions);
  }

  getAdminUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>('/api/admin/users', this.authOptions);
  }

  updateAdminUser(userId: number, payload: { maxDevices: number }): Observable<AdminUser> {
    return this.http.patch<AdminUser>(`/api/admin/users/${userId}`, payload, this.authOptions);
  }

  setAdminUserStatus(userId: number, active: boolean): Observable<AdminUser> {
    return this.http.post<AdminUser>(`/api/admin/users/${userId}/status`, { active }, this.authOptions);
  }
}
