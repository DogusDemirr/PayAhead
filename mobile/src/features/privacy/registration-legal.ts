import { create } from 'zustand';
import { legalVersion } from './documents';
import type { RegistrationLegal } from '../../services/auth-api';
export type DocumentId = 'notice' | 'privacy' | 'terms';
export const documentIds: DocumentId[] = ['notice', 'privacy', 'terms'];
let sequence = 0;
// Registration-only memory. Never persisted, and never inferred from a previous account.
export const useRegistrationLegal = create<{
  attempt: string | null; selections: Partial<Record<DocumentId, string>>;
  begin(): string; end(attempt: string): void; toggle(attempt: string, id: DocumentId): void;
}>(set => ({
  attempt: null, selections: {},
  begin() { const attempt = String(++sequence); set({ attempt, selections: {} }); return attempt; },
  end(attempt) { set(state => state.attempt === attempt ? { attempt: null, selections: {} } : {}); },
  toggle(attempt, id) { set(state => state.attempt === attempt ? { selections: { ...state.selections, [id]: state.selections[id] === legalVersion ? undefined : legalVersion } } : {}); },
}));
export function registrationLegalPayload(): RegistrationLegal | null {
  const { attempt, selections } = useRegistrationLegal.getState();
  if (!attempt || documentIds.some(id => selections[id] !== legalVersion)) return null;
  return { noticeVersion: legalVersion, noticeAcknowledged: true, privacyVersion: legalVersion, privacyAcknowledged: true, termsVersion: legalVersion, termsAccepted: true };
}
