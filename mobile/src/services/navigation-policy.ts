import type { SessionStatus } from './session-manager';
export function navigationAccess(status: SessionStatus) {
  return { ready: status !== 'restoring', protected: status === 'authenticated', auth: status !== 'authenticated' && status !== 'restoring' };
}
