import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import type { LanguageStorage } from './preference';
const key = 'payahead.language.v1';
// Device UI preference is deliberately separate from login/token storage.
export const languageStorage: LanguageStorage = {
  async read() { return Platform.OS === 'web' ? globalThis.localStorage?.getItem(key) : SecureStore.getItemAsync(key); },
  async write(language) { if (Platform.OS === 'web') globalThis.localStorage?.setItem(key, language); else await SecureStore.setItemAsync(key, language); },
};
