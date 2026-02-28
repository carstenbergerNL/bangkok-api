export interface LoginRequest {
  email: string;
  password: string;
  applicationId: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  tokenType: string;
  displayName?: string;
  applicationId?: string;
  roles: string[];
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName?: string;
  role?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
