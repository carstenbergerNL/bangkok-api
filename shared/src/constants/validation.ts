/**
 * Validation limits. Must match backend DTOs and business rules.
 */
export const VALIDATION = {
  PROFILE: {
    FIRST_NAME_MAX_LENGTH: 100,
    MIDDLE_NAME_MAX_LENGTH: 100,
    LAST_NAME_MAX_LENGTH: 100,
    PHONE_NUMBER_MAX_LENGTH: 30,
    AVATAR_MAX_BYTES: 2 * 1024 * 1024, // 2MB
    AVATAR_ACCEPT_TYPES: ['image/jpeg', 'image/png'] as const,
  },
  AUTH: {
    PASSWORD_MIN_LENGTH: 8,
  },
  USER: {
    DISPLAY_NAME_MAX_LENGTH: 256,
    EMAIL_MAX_LENGTH: 256,
  },
} as const;
