export interface User {
  id: string;
  email: string;
  displayName?: string;
  roles: string[];
  isActive: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string;
  isDeleted?: boolean;
  deletedAtUtc?: string;
}
