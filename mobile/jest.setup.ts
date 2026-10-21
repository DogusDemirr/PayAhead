import mockSafeAreaContext from 'react-native-safe-area-context/jest/mock';
import { jest } from '@jest/globals';
jest.mock('react-native-safe-area-context', () => mockSafeAreaContext);
jest.mock('react-native-worklets', () => require('react-native-worklets/src/mock'));
jest.mock('react-native-reanimated', () => ({ ...require('react-native-reanimated/mock'), useReducedMotion: () => true }));
jest.mock('expo-router', () => ({ router: { push: jest.fn(), replace: jest.fn(), back: jest.fn(), canGoBack: () => true }, useLocalSearchParams: () => ({}) }));
process.env.EXPO_PUBLIC_DESIGN_PREVIEW = 'true';
