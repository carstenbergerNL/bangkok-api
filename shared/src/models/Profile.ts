export interface Profile {
  id: string;
  userId: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  dateOfBirth: string;
  phoneNumber?: string;
  avatarBase64?: string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
}

export interface CreateProfileRequest {
  userId: string;
  firstName: string;
  middleName?: string;
  lastName: string;
  dateOfBirth: string;
  phoneNumber?: string;
  avatarBase64?: string;
}

export interface UpdateProfileRequest {
  firstName?: string;
  middleName?: string;
  lastName?: string;
  dateOfBirth?: string;
  phoneNumber?: string;
  avatarBase64?: string;
}
