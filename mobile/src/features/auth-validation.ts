import { z } from 'zod';
const email = z.string().trim().max(254, 'En fazla 254 karakter kullan.').pipe(z.email('Geçerli bir e-posta adresi yaz.'));
export const loginSchema = z.object({ email, password: z.string().min(1, 'Şifreni yaz.').max(128, 'En fazla 128 karakter kullan.'), displayName: z.string() });
export const registerSchema = loginSchema.extend({
  password: z.string().min(12, 'En az 12 karakter kullan.').max(128, 'En fazla 128 karakter kullan.'),
  displayName: z.string().trim().min(1, 'Adını yaz.').max(100, 'En fazla 100 karakter kullan.'),
});
export type AuthFields = z.infer<typeof loginSchema>;
