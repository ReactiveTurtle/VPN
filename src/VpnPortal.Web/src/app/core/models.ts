export interface AppStatus {
  name: string;
  version: string;
  databaseConfigured: boolean;
  developmentMode: boolean;
}

export interface VpnRequest {
  id: number;
  email: string;
  name: string | null;
  status: string;
  adminComment: string | null;
  submittedAt: string;
  processedAt: string | null;
  activationExpiresAt: string | null;
  activationLink: string | null;
}

export interface ActivationTokenStatus {
  valid: boolean;
  used: boolean;
  email: string | null;
  expiresAt: string | null;
  message: string;
}

export interface ActivationCompleted {
  userId: number;
  email: string;
  username: string;
  message: string;
}

export interface SessionUser {
  id: number;
  login: string;
  role: string;
  email: string | null;
}

export interface UserDashboard {
  id: number;
  email: string;
  username: string;
  active: boolean;
  maxDevices: number;
  devices: TrustedDevice[];
  trustedIps: TrustedIp[];
  pendingIpConfirmations: IpChangeConfirmation[];
  sessions: VpnSession[];
}

export interface TrustedDevice {
  id: number;
  deviceName: string;
  deviceType: string;
  platform: string;
  status: string;
  firstSeenAt: string;
  lastSeenAt: string | null;
}

export interface VpnSession {
  id: number;
  sourceIp: string;
  assignedVpnIp: string | null;
  deviceName: string | null;
  startedAt: string;
  lastSeenAt: string | null;
  active: boolean;
  authorized: boolean;
}

export interface TrustedIp {
  id: number;
  ipAddress: string;
  status: string;
  firstSeenAt: string;
  lastSeenAt: string | null;
  approvedAt: string | null;
}

export interface IpChangeConfirmation {
  id: number;
  requestedIp: string;
  status: string;
  expiresAt: string;
  createdAt: string;
  confirmationLink: string | null;
}

export interface IpConfirmationRequestResult {
  confirmationId: number;
  requestedIp: string;
  expiresAt: string;
  confirmationLink: string;
  message: string;
}

export interface AdminSession {
  id: number;
  userId: number;
  username: string;
  deviceName: string | null;
  sourceIp: string;
  assignedVpnIp: string | null;
  startedAt: string;
  lastSeenAt: string | null;
  active: boolean;
  authorized: boolean;
}

export interface AuditLogEntry {
  id: number;
  actorType: string;
  actorId: number | null;
  action: string;
  entityType: string;
  entityId: string;
  ipAddress: string | null;
  detailsJson: string | null;
  createdAt: string;
}

export interface AdminUser {
  id: number;
  email: string;
  username: string;
  active: boolean;
  emailConfirmed: boolean;
  maxDevices: number;
  deviceCount: number;
  createdAt: string;
  lastLoginAt: string | null;
}
