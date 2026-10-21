import { create } from 'zustand';
import type { SessionSnapshot } from '../services/session-manager';
// Tokens remain private to SessionManager/SecureStore; only public UI state is observable.
export const useSessionStore = create<SessionSnapshot>(() => ({ status: 'restoring', user: null, notice: null }));
