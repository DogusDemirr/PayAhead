import { useRef, useState } from 'react';
import { View } from 'react-native';
import { router, useLocalSearchParams } from 'expo-router';
import { AppScreen, AppHeader, AppText, AppInput, AppButton, AppCard, Brand } from '../components/ui';
import { passwordResetApi, forgotSchema, resetSchema, extractResetToken } from '../services/password-reset';
import { ApiError } from '../services/api-error';
import { session } from '../services/auth-runtime';
export default function PasswordResetScreen({ reset = false }: { reset?: boolean }) {
  const params = useLocalSearchParams<{ token?: string }>();
  const [token, setToken] = useState(typeof params.token === 'string' && params.token.length <= 2048 ? params.token : '');
  const [email, setEmail] = useState(''); const [password, setPassword] = useState(''); const [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false); const gate = useRef(false); const [message, setMessage] = useState(''); const [done, setDone] = useState(false);
  const [sourceToken, setSourceToken] = useState(params.token);
  if (typeof params.token === 'string' && params.token.length <= 2048 && params.token !== sourceToken) {
    setSourceToken(params.token); setToken(params.token); setPassword(''); setConfirmation(''); setDone(false); setMessage('');
  }
  async function submit() {
    if (gate.current || done) return;
    const parsed = reset ? resetSchema.safeParse({ token: extractResetToken(token), password, confirmation }) : forgotSchema.safeParse({ email: email.trim() });
    if (!parsed.success) { setMessage(parsed.error.issues[0].message); return; }
    gate.current = true; setBusy(true); setMessage('');
    try {
      if (reset) { await passwordResetApi.reset(extractResetToken(token), password); setToken(''); setPassword(''); setConfirmation(''); router.setParams({ token: undefined }); await session.invalidate('Şifren yenilendi. Yeni şifrenle giriş yap.'); }
      else await passwordResetApi.forgot(email.trim());
      setDone(true);
    } catch (error) { setMessage(error instanceof ApiError ? error.message : 'İşlem tamamlanamadı. Bağlantını kontrol edip tekrar dene.'); }
    finally { gate.current = false; setBusy(false); }
  }
  return <AppScreen keyboard><View style={{ marginBottom: 28 }}><Brand /></View><AppHeader title={reset ? 'Yeni şifren' : 'Şifreni yenile'} subtitle={reset ? 'Hesabına güvenli bir başlangıç.' : 'E-posta adresinle devam et.'} back />
    <View style={{ gap: 18 }}>{done ? <AppCard><AppText tone="success" accessibilityRole="alert">{reset ? 'Şifren yenilendi. Yeni şifrenle giriş yap.' : 'Adres kayıtlıysa şifre yenileme bağlantısı gönderilecektir. Gelen kutunu ve spam klasörünü kontrol et.'}</AppText>{!reset && <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>Birkaç dakika içinde ulaşmazsa adresini kontrol edip yeni bir bağlantı isteyebilirsin.</AppText>}</AppCard> : reset ? <><AppInput label="E-postadaki bağlantı veya kod" value={token} onChangeText={setToken} autoCapitalize="none" autoCorrect={false} secureTextEntry maxLength={2048} editable={!busy} /><AppText variant="caption" tone="muted">Bağlantı otomatik açılmadıysa e-postadaki bağlantının tamamını buraya yapıştır. Tek kullanımlıktır ve kısa sürede sona erer.</AppText><AppInput label="Yeni şifre" value={password} onChangeText={setPassword} secureTextEntry autoComplete="new-password" maxLength={128} editable={!busy} /><AppInput label="Yeni şifre tekrar" value={confirmation} onChangeText={setConfirmation} secureTextEntry autoComplete="new-password" maxLength={128} editable={!busy} /></> : <AppInput label="E-posta" value={email} onChangeText={setEmail} keyboardType="email-address" autoCapitalize="none" autoCorrect={false} maxLength={254} editable={!busy} />}
      {!!message && <AppCard><AppText tone="danger" accessibilityRole="alert">{message}</AppText></AppCard>}
      {!done && <AppButton title={reset ? 'Şifreyi yenile' : 'Yenileme bağlantısı iste'} onPress={() => { void submit(); }} loading={busy} />}
      {!reset && <AppButton title="Bağlantım var, yeni şifre belirle" variant="secondary" onPress={() => router.push('/reset-password')} />}
      {reset && !done && <AppButton title="Yeni bağlantı iste" variant="secondary" onPress={() => router.replace('/forgot-password')} />}
      <AppButton title="Girişe dön" variant="ghost" onPress={() => router.replace('/(auth)/login')} />
    </View></AppScreen>;
}
