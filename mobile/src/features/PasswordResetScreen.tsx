import { t, useLocaleRevision } from "../i18n/runtime";
import { useRef, useState } from 'react';
import { View } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { AppScreen, AppHeader, AppText, AppInput, AppButton, AppCard, Brand } from '../components/ui';
import { passwordResetApi, forgotSchema, resetSchema, extractResetToken } from '../services/password-reset';
import { ApiError } from '../services/api-error';
import { session } from '../services/auth-runtime';
export default function PasswordResetScreen({ reset = false }: {
    reset?: boolean;
}) {
    useLocaleRevision();
    const params = useLocalSearchParams<{
        token?: string;
    }>();
    const [token, setToken] = useState(typeof params.token === 'string' && params.token.length <= 2048 ? params.token : '');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmation, setConfirmation] = useState('');
    const [busy, setBusy] = useState(false);
    const gate = useRef(false);
    const [message, setMessage] = useState('');
    const [done, setDone] = useState(false);
    const [sourceToken, setSourceToken] = useState(params.token);
    if (typeof params.token === 'string' && params.token.length <= 2048 && params.token !== sourceToken) {
        setSourceToken(params.token);
        setToken(params.token);
        setPassword('');
        setConfirmation('');
        setDone(false);
        setMessage('');
    }
    async function submit() {
        if (gate.current || done)
            return;
        const parsed = reset ? resetSchema.safeParse({ token: extractResetToken(token), password, confirmation }) : forgotSchema.safeParse({ email: email.trim() });
        if (!parsed.success) {
            setMessage(parsed.error.issues[0].message);
            return;
        }
        gate.current = true;
        setBusy(true);
        setMessage('');
        try {
            if (reset) {
                await passwordResetApi.reset(extractResetToken(token), password);
                setToken('');
                setPassword('');
                setConfirmation('');
                router.setParams({ token: undefined });
                await session.invalidate(t("auth:m_b2a75aba2a9a"));
            }
            else
                await passwordResetApi.forgot(email.trim());
            setDone(true);
        }
        catch (error) {
            setMessage(error instanceof ApiError ? error.message : t("auth:m_f39a90606360"));
        }
        finally {
            gate.current = false;
            setBusy(false);
        }
    }
    return <AppScreen keyboard><View style={{ marginBottom: 28 }}><Brand /></View><AppHeader title={reset ? t("auth:m_48cc793fec26") : t("auth:m_907f977177d1")} subtitle={reset ? t("auth:m_17901d377b68") : t("auth:m_fd3f0daece3c")} back/>
    <View style={{ gap: 18 }}>{done ? <AppCard><AppText tone="success" accessibilityRole="alert">{reset ? t("auth:m_b2a75aba2a9a") : t("auth:m_1ab5f8dd3652")}</AppText>{!reset && <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{t("auth:m_bd00063bca49")}</AppText>}</AppCard> : reset ? <><AppInput label={t("auth:m_d00c902689a9")} value={token} onChangeText={setToken} autoCapitalize="none" autoCorrect={false} secureTextEntry maxLength={2048} editable={!busy}/><AppText variant="caption" tone="muted">{t("auth:m_7b15b84ff5be")}</AppText><AppInput label={t("auth:m_fefced24bd71")} value={password} onChangeText={setPassword} secureTextEntry autoComplete="new-password" maxLength={128} editable={!busy}/><AppInput label={t("auth:m_df7cb2aa8f92")} value={confirmation} onChangeText={setConfirmation} secureTextEntry autoComplete="new-password" maxLength={128} editable={!busy}/></> : <AppInput label={t("auth:m_e1081b69d80a")} value={email} onChangeText={setEmail} keyboardType="email-address" autoCapitalize="none" autoCorrect={false} maxLength={254} editable={!busy}/>}
      {!!message && <AppCard><AppText tone="danger" accessibilityRole="alert">{message}</AppText></AppCard>}
      {!done && <AppButton title={reset ? t("auth:m_24c83f2c27f8") : t("auth:m_d3ca60d80048")} onPress={() => { void submit(); }} loading={busy}/>}
      {!reset && <AppButton title={t("auth:m_c7e7daf09eac")} variant="secondary" onPress={() => router.push('/reset-password')}/>}
      {reset && !done && <AppButton title={t("auth:m_5064114b875a")} variant="secondary" onPress={() => router.replace('/forgot-password')}/>}
      <AppButton title={t("auth:m_9ff2fd4e9f6d")} variant="ghost" onPress={() => router.replace('/(auth)/login')}/>
    </View></AppScreen>;
}
