import { create } from 'zustand';

export type ThemePreference = 'system' | 'light' | 'dark';
export type PreviewState = 'demo' | 'empty' | 'loading' | 'error';
interface UiState {
  theme: ThemePreference;
  preview: PreviewState;
  setTheme: (theme: ThemePreference) => void;
  setPreview: (preview: PreviewState) => void;
}
// UI preferences only. No JWT, fabricated login state or server data lives here.
export const useUiStore = create<UiState>((set) => ({
  theme: 'system', preview: 'demo',
  setTheme: (theme) => set({ theme }), setPreview: (preview) => set({ preview }),
}));
