import { dateKey } from '../utils/format';
import type { OccurrenceStatus } from '../types/api';
export type Category = 'home' | 'internet' | 'music' | 'cloud' | 'energy' | 'education';
export interface PaymentPreview { id: string; name: string; subtitle: string; date: string; amount: number; currencyCode: string; category: Category; status: OccurrenceStatus }
export interface SubscriptionPreview { id: string; name: string; provider: string; amount: number; currencyCode: string; category: Category; renewal: string; trialDays?: number }
const anchor = new Date();
export const relativeDay = (offset: number) => { const day = new Date(anchor.getFullYear(), anchor.getMonth(), anchor.getDate() + offset); return dateKey(day.getFullYear(), day.getMonth(), day.getDate()); };
// Deliberately fictional UI fixtures. Never injected into QueryClient or the Axios client.
export const previewPayments: PaymentPreview[] = [
  { id: 'preview-1', name: 'İnternet faturası', subtitle: 'Ev & yaşam', date: relativeDay(1), amount: 899.90, currencyCode: 'TRY', category: 'internet', status: 'Pending' },
  { id: 'preview-2', name: 'Studio Music', subtitle: 'Aylık abonelik', date: relativeDay(2), amount: 199.99, currencyCode: 'TRY', category: 'music', status: 'Pending' },
  { id: 'preview-3', name: 'Ev kirası', subtitle: 'Ev & yaşam', date: relativeDay(3), amount: 8450, currencyCode: 'TRY', category: 'home', status: 'Pending' },
  { id: 'preview-4', name: 'Cloud Space', subtitle: 'Bulut depolama', date: relativeDay(5), amount: 5.99, currencyCode: 'USD', category: 'cloud', status: 'Pending' },
  { id: 'preview-5', name: 'Elektrik faturası', subtitle: 'Ev & yaşam', date: relativeDay(6), amount: 640, currencyCode: 'TRY', category: 'energy', status: 'Pending' },
  { id: 'preview-6', name: 'Dil atölyesi', subtitle: 'Kişisel gelişim', date: relativeDay(-1), amount: 450, currencyCode: 'TRY', category: 'education', status: 'Paid' },
  { id: 'preview-7', name: 'Kitap kulübü', subtitle: 'Kişisel gelişim', date: relativeDay(-2), amount: 229.90, currencyCode: 'TRY', category: 'education', status: 'Skipped' },
];
export const previewSubscriptions: SubscriptionPreview[] = [
  { id: 'subscription-1', name: 'Studio Music', provider: 'Müzik & eğlence', amount: 199.99, currencyCode: 'TRY', category: 'music', renewal: relativeDay(2) },
  { id: 'subscription-2', name: 'Cloud Space', provider: 'Bulut depolama', amount: 5.99, currencyCode: 'USD', category: 'cloud', renewal: relativeDay(5) },
  { id: 'subscription-3', name: 'Focus Pro', provider: 'Üretkenlik', amount: 89.99, currencyCode: 'TRY', category: 'education', renewal: relativeDay(3), trialDays: 3 },
];
