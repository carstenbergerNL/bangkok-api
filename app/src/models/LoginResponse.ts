export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  tokenType: string;
  displayName?: string;
  applicationId?: string;
  role?: string;
}
