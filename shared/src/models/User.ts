export interface User {
  id: string;
  email: string;
  displayName?: string;
  role: string;
  isActive: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  isDeleted?: boolean;
  deletedAtUtc?: string;
}
