import { useState } from 'react';
import { View } from 'react-native';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { AppScreen, AppHeader, AppCard, AppText, AppInput, AppButton } from '../components/ui';
const schema = z.object({ name: z.string().trim().min(1, 'Ödeme adı yaz.').max(150, 'En fazla 150 karakter kullan.'), amount: z.string().regex(/^\d+(,\d{1,4})?$/, 'Tutarı 899,90 biçiminde yaz.').refine((v) => Number(v.replace(',', '.')) > 0, 'Tutar sıfırdan büyük olmalı.') });
type Fields = z.infer<typeof schema>;
export default function AddPayment() {
  const [notice, setNotice] = useState(false); const { control, handleSubmit, formState: { errors } } = useForm<Fields>({ resolver: zodResolver(schema), defaultValues: { name: '', amount: '' } });
  return <AppScreen keyboard><AppHeader title="Yeni ödeme" subtitle="Gelecek planlarına küçük bir adım." back /><View style={{ gap: 20 }}><Controller control={control} name="name" render={({ field }) => <AppInput label="Ödeme adı" placeholder="Örn. İnternet faturası" value={field.value} onChangeText={field.onChange} error={errors.name?.message} />} /><Controller control={control} name="amount" render={({ field }) => <AppInput label="Tutar · TRY" placeholder="0,00" keyboardType="decimal-pad" value={field.value} onChangeText={field.onChange} error={errors.amount?.message} />} /><AppCard><AppText variant="label">Aylık ödeme</AppText><AppText tone="muted" style={{ marginTop: 8 }}>Tarih, tekrar ve hatırlatma seçenekleri hesap bağlantısı hazır olduğunda düzenlenebilecek.</AppText></AppCard><AppText variant="caption" tone="muted">Bu form bir tasarım önizlemesidir. Gerçek kayıt oluşturulmaz.</AppText>{notice && <AppText accessibilityRole="alert" tone="accentText">Önizleme kontrol edildi. Ödeme kaydı oluşturulmadı.</AppText>}<AppButton title="Önizlemeyi kontrol et" onPress={handleSubmit(() => setNotice(true))} /></View></AppScreen>;
}
