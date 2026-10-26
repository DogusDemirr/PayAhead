import type { ConfigContext, ExpoConfig } from 'expo/config';
// Actual project/FCM values are supplied locally or by EAS, never invented here.
export default ({ config }: ConfigContext): ExpoConfig => {
  const projectId = process.env.EXPO_PUBLIC_EAS_PROJECT_ID;
  const googleServicesFile = process.env.GOOGLE_SERVICES_JSON;
  if (projectId && !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(projectId)) throw new Error('Geçerli EAS projectId gerekli.');
  return { ...config, name: config.name ?? 'PayAhead', slug: config.slug ?? 'payahead',
    plugins: [...(config.plugins ?? []), ['expo-localization', { supportsRTL: true, supportedLocales: { ios: ['tr', 'en', 'de', 'fr', 'es', 'it', 'pt-BR', 'nl', 'ru', 'ar', 'zh-Hans', 'ja', 'ko', 'hi'], android: ['tr', 'en', 'de', 'fr', 'es', 'it', 'pt-BR', 'nl', 'ru', 'ar', 'zh-Hans', 'ja', 'ko', 'hi'] } }], ['expo-build-properties', { android: { usesCleartextTraffic: process.env.EAS_BUILD_PROFILE === 'development' } }]],
    android: { ...config.android, ...(googleServicesFile ? { googleServicesFile } : {}) },
    extra: { ...config.extra, supportsRTL: true, ...(projectId ? { eas: { projectId } } : {}) },
  };
};
