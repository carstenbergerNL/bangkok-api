export interface Role {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
}
