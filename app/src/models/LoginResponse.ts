export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  tokenType: string;
  displayName?: string;
  applicationId?: string;
  roles: string[];
  /** Permission names (e.g. ViewAdminSettings). Omitted in older API versions. */
  permissions?: string[];
}
