import { t } from "../i18n/runtime";
import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { storedSessionSchema, type SessionStorage } from './session-model';
export type { StoredSession } from './session-model';
const key = 'payahead.session.v1';
// One atomic native record: a rotated access token can never pair with an old refresh token.
export const tokenStorage: SessionStorage = {
    async read() {
        if (Platform.OS === 'web')
            return null;
        const value = await SecureStore.getItemAsync(key);
        if (!value)
            return null;
        try {
            const parsed = storedSessionSchema.safeParse(JSON.parse(value));
            if (parsed.success)
                return parsed.data;
        }
        catch { /* clear malformed or Phase 1 records */ }
        await this.clear();
        return null;
    },
    async write(session) {
        if (Platform.OS === 'web')
            throw new Error(t("common:m_50fbcc5ae0d0"));
        await SecureStore.setItemAsync(key, JSON.stringify(storedSessionSchema.parse(session)), { keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY });
    },
    async clear() {
        if (Platform.OS === 'web')
            return;
        try {
            await SecureStore.deleteItemAsync(key);
        }
        catch {
            await SecureStore.setItemAsync(key, 'null', { keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY });
        }
    },
};
