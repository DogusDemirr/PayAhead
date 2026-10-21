export const semanticPalette = { success: '#16A34A', warning: '#F59E0B', error: '#EF4444' } as const;
export const light = {
  background: '#F5F7FA', surface: '#FFFFFF', elevated: '#EDF2F7', text: '#101D35',
  muted: '#64748B', border: '#E5EAF0', accent: '#14B8A6', accentText: '#087C70',
  accentSoft: '#E5F7F3', navy: '#101D35', onNavy: '#F8FAFC', onNavyMuted: '#B7C8DB',
  success: '#15803D', successSoft: '#EAF6EE', warning: '#926000', warningSoft: '#FFF4D9',
  danger: '#C53030', dangerSoft: '#FDECEC', lavender: '#7367B7', lavenderSoft: '#F0EDFB',
  blue: '#426FB3', blueSoft: '#EAF1FC', decoration: '#1D3A50', white: '#FFFFFF',
};
export type Colors = { [K in keyof typeof light]: string };
export const dark: Colors = {
  background: '#0B1220', surface: '#151F30', elevated: '#1C2A40', text: '#F8FAFC',
  muted: '#94A3B8', border: '#29374A', accent: '#2DD4BF', accentText: '#5EEAD4',
  accentSoft: '#153C3C', navy: '#101D35', onNavy: '#F8FAFC', onNavyMuted: '#B7C8DB',
  success: '#86EFAC', successSoft: '#173C2B', warning: '#FCD34D', warningSoft: '#3D321D',
  danger: '#FCA5A5', dangerSoft: '#48252C', lavender: '#C4B5FD', lavenderSoft: '#302B4D',
  blue: '#93C5FD', blueSoft: '#20344F', decoration: '#1D3A50', white: '#FFFFFF',
};
export const space = { xs: 4, sm: 8, md: 12, lg: 16, xl: 20, xxl: 24, xxxl: 32, huge: 40 };
export const radius = { input: 14, button: 14, card: 20, small: 12, pill: 100 };
export const typography = {
  display: { fontSize: 34, lineHeight: 42, fontFamily: 'Inter_700Bold', letterSpacing: -1.3 },
  amount: { fontSize: 34, lineHeight: 44, fontFamily: 'Inter_600SemiBold', letterSpacing: -1.3 },
  title: { fontSize: 28, lineHeight: 36, fontFamily: 'Inter_700Bold', letterSpacing: -0.9 },
  section: { fontSize: 19, lineHeight: 27, fontFamily: 'Inter_600SemiBold', letterSpacing: -0.4 },
  body: { fontSize: 15, lineHeight: 23, fontFamily: 'Inter_400Regular', letterSpacing: -0.15 },
  label: { fontSize: 15, lineHeight: 22, fontFamily: 'Inter_600SemiBold', letterSpacing: -0.2 },
  caption: { fontSize: 12, lineHeight: 18, fontFamily: 'Inter_500Medium', letterSpacing: 0 },
} as const;
