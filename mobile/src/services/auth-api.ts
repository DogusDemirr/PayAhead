import type { AxiosInstance } from 'axios';
import type { AuthResponse, UserResponse } from '../types/api';
export interface LoginRequest { email: string; password: string }
export interface RegistrationLegal {
  noticeVersion: string; noticeAcknowledged: boolean;
  privacyVersion: string; privacyAcknowledged: boolean;
  termsVersion: string; termsAccepted: boolean;
}
export interface RegisterRequest extends LoginRequest { displayName: string; legalVersion?: string; legal: RegistrationLegal }
export function createAuthApi(client: AxiosInstance) {
  return {
    register: async (request: RegisterRequest) => (await client.post<UserResponse>('/api/auth/register', request)).data,
    login: async (request: LoginRequest) => (await client.post<AuthResponse>('/api/auth/login', request)).data,
    refresh: async (refreshToken: string) => (await client.post<AuthResponse>('/api/auth/refresh', { refreshToken })).data,
    logout: async (refreshToken: string) => { await client.post('/api/auth/logout', { refreshToken }); },
  };
}
export type AuthApi = ReturnType<typeof createAuthApi>;
