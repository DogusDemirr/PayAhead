import { useEffect, useRef } from 'react';
import { AppState } from 'react-native';
import { apiClient, session } from '../services/auth-runtime';
import { useSessionStore } from '../store/session';
import { isLanguage } from './languages';
import { currentLanguage, languagePreference, restoreLanguage, useLanguageStore } from './runtime';

export function LanguageBootstrap() {
  const ready = useLanguageStore(s => s.ready); const language = useLanguageStore(s => s.language);
  const user = useSessionStore(s => s.user?.id); const status = useSessionStore(s => s.status); const owner = useRef<string | undefined>(undefined);
  const writes = useRef<Promise<void>>(Promise.resolve());
  useEffect(() => { void restoreLanguage(); }, []);
  useEffect(() => {
    if (!ready) return;
    if (owner.current !== user) { owner.current = user; languagePreference.accountChanged(); }
    if (status !== 'authenticated' || !user) return;
    const credential = session.credential(); if (!credential) return;
    let cancelled = false; const revision = languagePreference.revision; const selected = languagePreference.manual;
    const current = () => !cancelled && session.credential()?.epoch === credential.epoch && languagePreference.revision === revision;
    async function sync() {
      try {
        if (selected) {
          writes.current = writes.current.catch(() => undefined).then(async () => {
            if (!current() || currentLanguage() !== selected) return;
            await apiClient.put('/api/users/me/language', { languageCode: selected }, { headers: {}, ...{ sessionEpoch: credential!.epoch } });
            if (current()) useLanguageStore.setState({ syncPending: false });
          });
          await writes.current;
        } else {
          const response = await apiClient.get('/api/users/me/language', { headers: {}, ...{ sessionEpoch: credential!.epoch } });
          if (current() && isLanguage(response.data?.languageCode)) languagePreference.acceptAccount(response.data.languageCode, revision);
        }
      } catch { if (current()) useLanguageStore.setState({ syncPending: !!selected }); }
    }
    void sync();
    const appState = AppState.addEventListener('change', state => { if (state === 'active') void sync(); });
    return () => { cancelled = true; appState.remove(); };
  }, [ready, user, status, language]);
  return null;
}
