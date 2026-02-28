export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  error?: { code?: string; message?: string };
  errors?: Array<{ code?: string; message?: string }>;
  correlationId?: string;
}
