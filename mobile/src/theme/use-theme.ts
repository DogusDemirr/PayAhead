import { useColorScheme } from 'react-native';
import { useUiStore } from '../store/ui';
import { dark, light } from './tokens';

export function useTheme() {
  const system = useColorScheme();
  const preference = useUiStore((state) => state.theme);
  const scheme = preference === 'system' ? system ?? 'light' : preference;
  return { colors: scheme === 'dark' ? dark : light, scheme };
}
