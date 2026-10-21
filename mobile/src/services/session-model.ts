import { z } from 'zod';
export const storedSessionSchema = z.object({
  accessToken: z.string().min(1).max(16384), refreshToken: z.string().regex(/^[A-Za-z0-9_-]{64}$/),
  accessTokenExpiresAtUtc: z.iso.datetime({ offset: true }), refreshTokenExpiresAtUtc: z.iso.datetime({ offset: true }),
});
export type StoredSession = z.infer<typeof storedSessionSchema>;
export interface SessionStorage { read(): Promise<StoredSession | null>; write(session: StoredSession): Promise<void>; clear(): Promise<void> }
