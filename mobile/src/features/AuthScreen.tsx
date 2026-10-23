import { useRef, useState } from 'react';
import { View, Pressable } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { ArrowRight, Layers3 } from 'lucide-react-native';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { isCancel } from 'axios';
import { authApi, session } from '../services/auth-runtime';
import { ApiError } from '../services/api-error';
import { useSessionStore } from '../store/session';
import { loginSchema, registerSchema, type AuthFields } from './auth-validation';
import { AppScreen, AppText, AppInput, AppButton, Brand, AppCard, Assurance, styles } from '../components/ui';
import { useTheme } from '../theme/use-theme';


export default function AuthScreen({ register = false }: { register?: boolean }) {
  const { colors } = useTheme(); const busy = useRef(false); const [submitting, setSubmitting] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const sessionNotice = useSessionStore((state) => state.notice);
  const { registered } = useLocalSearchParams<{ registered?: string }>();
  const { control, handleSubmit, setError, resetField, formState: { errors } } = useForm<AuthFields>({ resolver: zodResolver(register ? registerSchema : loginSchema), defaultValues: { email: '', password: '', displayName: '' } });
  const submit = () => handleSubmit(async ({ email, password, displayName }) => {
    if (busy.current) return;
    busy.current = true; setSubmitting(true); setNotice(null);
    try {
      if (register) {
        await authApi.register({ email, password, displayName });
        resetField('password');
        router.replace({ pathname: '/(auth)/login', params: { registered: 'true' } });
      } else {
        await session.login({ email, password });
        resetField('password');
        // Stack.Protected removes auth history and opens the dashboard via index.
      }
    } catch (error) {
      if (!isCancel(error)) {
        setNotice(error instanceof ApiError ? error.message : 'Güvenli oturum kurulamadı. Cihaz depolamasını ve bağlantını kontrol et.');
        if (error instanceof ApiError && error.errors) {
          for (const field of ['email', 'password', 'displayName'] as const) {
            const messages = error.errors[field] ?? error.errors[field[0].toUpperCase() + field.slice(1)];
            if (Array.isArray(messages) && messages.length) setError(field, { type: 'server', message: messages.join(' ') });
          }
        }
      }
    } finally { busy.current = false; setSubmitting(false); }
  })();
  return <AppScreen keyboard><View style={{ paddingTop: 20, paddingBottom: 28 }}><Brand large /></View>
    {!register && registered === 'true' && <AppCard style={{ marginBottom: 20 }}><AppText tone="accentText" accessibilityRole="alert">Hesabın oluşturuldu. Şimdi giriş yapabilirsin.</AppText></AppCard>}
    <AppText variant="display" accessibilityRole="header">{register ? 'Daha sakin bir\nyarınla tanış.' : 'Finansal hayatına\nbiraz ferahlık.'}</AppText>
    <AppText tone="muted" style={{ marginTop: 14, marginBottom: 28, maxWidth: 350 }}>{register ? 'Ödemelerin ve aboneliklerin için tek bir yer. Kontrolü eline al.' : 'Ödemelerini önceden gör. Aboneliklerini takip et. Zihninde yer aç.'}</AppText>
    {!register && <AppCard style={{ marginBottom: 28, backgroundColor: colors.accentSoft, padding: 18 }}><View style={[styles.row, { gap: 12, justifyContent: 'flex-start' }]}><Layers3 color={colors.accentText} size={25} /><View style={{ flex: 1 }}><AppText variant="label">Bir bakışta, her şey net.</AppText><AppText variant="caption" tone="accentText" style={{ marginTop: 4 }}>Ödemeler · Abonelikler · Denemeler</AppText></View></View></AppCard>}
    <View style={{ gap: 18 }}>{register && <Controller control={control} name="displayName" render={({ field }) => <AppInput label="Adın" placeholder="Sana nasıl hitap edelim?" autoComplete="name" value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors.displayName?.message} />} />}
      <Controller control={control} name="email" render={({ field }) => <AppInput label="E-posta" placeholder="ornek@eposta.com" keyboardType="email-address" autoCapitalize="none" autoCorrect={false} autoComplete="email" value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors.email?.message} />} />
      <Controller control={control} name="password" render={({ field }) => <AppInput label="Şifre" placeholder={register ? 'En az 12 karakter' : 'Şifren'} secureTextEntry autoComplete={register ? 'new-password' : 'current-password'} value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors.password?.message} returnKeyType="done" onSubmitEditing={submit} />} />
      <AppText variant="caption" tone="muted">Bilgilerin yalnızca PayAhead sunucusuna gönderilir. Şifren bu cihazda saklanmaz.</AppText>
      {(notice || sessionNotice) && <AppCard style={{ backgroundColor: colors.warningSoft, padding: 16 }}><AppText tone="warning" accessibilityRole="alert">{notice || sessionNotice}</AppText></AppCard>}
      {!register && <AppButton title="Şifremi unuttum" variant="ghost" disabled={submitting} onPress={() => router.push('/forgot-password')} />}<AppButton title={register ? 'Hesap oluştur' : 'Giriş yap'} icon={ArrowRight} loading={submitting} disabled={submitting} onPress={submit} />
    </View>
    <View style={{ marginTop: 18 }}><Pressable disabled={submitting} accessibilityRole="link" onPress={() => router.replace(register ? '/(auth)/login' : '/(auth)/register')} style={{ minHeight: 48, justifyContent: 'center' }}><AppText style={{ textAlign: 'center' }} tone="muted">{register ? 'Zaten hesabın var mı? ' : 'PayAhead’de yeni misin? '}<AppText variant="label" tone="accentText">{register ? 'Giriş yap' : 'Hesap oluştur'}</AppText></AppText></Pressable></View>
    <Assurance text="Senin verin, senin kontrolün." />
  </AppScreen>;
}
