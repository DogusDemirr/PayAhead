import { useUiStore } from '../store/ui';
import { canUseDesignPreview } from '../services/preview-policy';
import { useSessionStore } from '../store/session';

export function usePreview() {
  const state = useUiStore((ui) => ui.preview);
  const authenticated = useSessionStore((session) => session.status === 'authenticated');
  const enabled = authenticated && canUseDesignPreview(__DEV__, process.env.EXPO_PUBLIC_DESIGN_PREVIEW);
  return { enabled, state: enabled ? state : 'empty' as const };
}
