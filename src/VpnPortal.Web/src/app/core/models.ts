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
  active: boolean;
  maxDevices: number;
  platformGuides: VpnOnboardingInstruction[];
  devices: TrustedDevice[];
  sessions: VpnSession[];
}

export interface VpnOnboardingInstruction {
  platform: string;
  title: string;
  summary: string;
  steps: string[];
  credentialLabel: string;
}

export interface TrustedDevice {
  id: number;
  deviceName: string;
  status: string;
  vpnUsername: string | null;
  credentialStatus: string | null;
  credentialRotatedAt: string | null;
  boundIpAddress: string | null;
  boundIpLastSeenAt: string | null;
  firstSeenAt: string;
  lastSeenAt: string | null;
}

export interface IssuedVpnDeviceCredential {
  deviceId: number;
  deviceName: string;
  vpnUsername: string;
  vpnPassword: string;
  onboarding: VpnOnboardingInstruction;
  message: string;
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
