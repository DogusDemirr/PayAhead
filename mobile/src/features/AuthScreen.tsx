import { t, useLocaleRevision } from "../i18n/runtime";
import { useRef, useState } from 'react';
import { View, Pressable } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { ArrowRight, Layers3 } from 'lucide-react-native';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { isCancel } from 'axios';
import { authApi, session } from '../services/auth-runtime';
import { ApiError } from '../services/api-error';
import { useSessionStore } from '../store/session';
import { loginSchema, registerSchema, type AuthFields, type AuthValues } from './auth-validation';
import { AppScreen, AppText, AppInput, AppButton, Brand, AppCard, Assurance, styles } from '../components/ui';
import { useTheme } from '../theme/use-theme';
import { LegalLinks } from './privacy/PrivacyScreen';
import { legalVersion } from './privacy/documents';
import { PasswordStrength } from './privacy/PasswordStrength';
export default function AuthScreen({ register = false }: {
    register?: boolean;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    const busy = useRef(false);
    const [submitting, setSubmitting] = useState(false);
    const [notice, setNotice] = useState<string | null>(null);
    const sessionNotice = useSessionStore((state) => state.notice);
    const { registered } = useLocalSearchParams<{
        registered?: string;
    }>();
    const { control, handleSubmit, setError, resetField, formState: { errors } } = useForm<AuthFields, unknown, AuthValues>({ resolver: zodResolver(register ? registerSchema : loginSchema), defaultValues: { email: '', password: '', displayName: '', confirmation: '' } });
    const passwordValue = useWatch({ control, name: 'password' });
    const submit = () => handleSubmit(async ({ email, password, displayName }) => {
        if (busy.current)
            return;
        busy.current = true;
        setSubmitting(true);
        setNotice(null);
        try {
            if (register) {
                await authApi.register({ email, password, displayName, legalVersion });
                resetField('password');
                resetField('confirmation');
                router.replace({ pathname: '/(auth)/login', params: { registered: 'true' } });
            }
            else {
                await session.login({ email, password });
                resetField('password');
                // Stack.Protected removes auth history and opens the dashboard via index.
            }
        }
        catch (error) {
            if (!isCancel(error)) {
                setNotice(error instanceof ApiError ? error.message : t("auth:m_8c8b07e2aba8"));
                if (error instanceof ApiError && error.errors) {
                    for (const field of ['email', 'password', 'displayName'] as const) {
                        const messages = error.errors[field] ?? error.errors[field[0].toUpperCase() + field.slice(1)];
                        if (Array.isArray(messages) && messages.length)
                            setError(field, { type: 'server', message: messages.join(' ') });
                    }
                }
            }
        }
        finally {
            busy.current = false;
            setSubmitting(false);
        }
    })();
    return <AppScreen keyboard><View style={{ paddingTop: 20, paddingBottom: 28 }}><Brand large/></View>
    {!register && registered === 'true' && <AppCard style={{ marginBottom: 20 }}><AppText tone="accentText" accessibilityRole="alert">{t("auth:m_819ad30ff99b")}</AppText></AppCard>}
    <AppText variant="display" accessibilityRole="header">{register ? t("auth:m_e8d8f98d769d") : t("auth:m_53c03a9fee45")}</AppText>
    <AppText tone="muted" style={{ marginTop: 14, marginBottom: 28, maxWidth: 350 }}>{register ? t("auth:m_fe08d685f51c") : t("auth:m_e002f77215ca")}</AppText>
    {!register && <AppCard style={{ marginBottom: 28, backgroundColor: colors.accentSoft, padding: 18 }}><View style={[styles.row, { gap: 12, justifyContent: 'flex-start' }]}><Layers3 color={colors.accentText} size={25}/><View style={{ flex: 1 }}><AppText variant="label">{t("auth:m_ff138299a73e")}</AppText><AppText variant="caption" tone="accentText" style={{ marginTop: 4 }}>{t("auth:m_f82e21c5a4e0")}</AppText></View></View></AppCard>}
    <View style={{ gap: 18 }}>{register && <Controller control={control} name="displayName" render={({ field }) => <AppInput label={t("auth:m_c5176ef33f7c")} placeholder={t("auth:m_899e435d76b8")} autoComplete="name" value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors.displayName?.message}/>}/>}
      <Controller control={control} name="email" render={({ field }) => <AppInput label={t("auth:m_e1081b69d80a")} placeholder={t("auth:m_b9c454be10ce")} keyboardType="email-address" autoCapitalize="none" autoCorrect={false} autoComplete="email" value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors.email?.message}/>}/>
      <Controller control={control} name="password" render={({ field }) => <AppInput label={t("auth:m_e1ac97fff826")} placeholder={register ? t("auth:m_7f510bd4744d") : t("auth:m_d247f5085a6c")} secureTextEntry autoComplete={register ? 'new-password' : 'current-password'} value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors.password?.message} returnKeyType="done" onSubmitEditing={submit}/>}/>
      {register && <><Controller control={control} name="confirmation" render={({ field }) => <AppInput label={t('settings:profile.repeatPassword')} value={field.value ?? ''} onChangeText={field.onChange} secureTextEntry autoComplete="new-password" error={errors.confirmation?.message} />} /><PasswordStrength password={passwordValue} /><AppCard><AppText variant="caption" tone="warning">{t('settings:legal.registration')}</AppText><LegalLinks /></AppCard></>}<AppText variant="caption" tone="muted">{t("auth:m_fea4c6044718")}</AppText>
      {(notice || sessionNotice) && <AppCard style={{ backgroundColor: colors.warningSoft, padding: 16 }}><AppText tone="warning" accessibilityRole="alert">{notice || sessionNotice}</AppText></AppCard>}
      {!register && <AppButton title={t("auth:m_14411176800f")} variant="ghost" disabled={submitting} onPress={() => router.push('/forgot-password')}/>}<AppButton title={register ? t("auth:m_bc5c818f2f46") : t("auth:m_b872b116e8f6")} icon={ArrowRight} loading={submitting} disabled={submitting} onPress={submit}/>
    </View>
    <View style={{ marginTop: 18 }}><Pressable disabled={submitting} accessibilityRole="link" onPress={() => router.replace(register ? '/(auth)/login' : '/(auth)/register')} style={{ minHeight: 48, justifyContent: 'center' }}><AppText style={{ textAlign: 'center' }} tone="muted">{register ? t("auth:m_2d479812c86b") : t("auth:m_1056f070e7ea")}<AppText variant="label" tone="accentText">{register ? t("auth:m_b872b116e8f6") : t("auth:m_bc5c818f2f46")}</AppText></AppText></Pressable></View>
    <Assurance text={t("auth:m_aa6bc6b1c20b")}/>
  </AppScreen>;
}
