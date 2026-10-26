import { t } from "../i18n/runtime";
import { dateKey } from '../utils/format';
import type { OccurrenceStatus } from '../types/api';
export type Category = 'home' | 'internet' | 'music' | 'cloud' | 'energy' | 'education';
export interface PaymentPreview {
    id: string;
    name: string;
    subtitle: string;
    date: string;
    amount: number;
    currencyCode: string;
    category: Category;
    status: OccurrenceStatus;
}
export interface SubscriptionPreview {
    id: string;
    name: string;
    provider: string;
    amount: number;
    currencyCode: string;
    category: Category;
    renewal: string;
    trialDays?: number;
}
const anchor = new Date();
export const relativeDay = (offset: number) => { const day = new Date(anchor.getFullYear(), anchor.getMonth(), anchor.getDate() + offset); return dateKey(day.getFullYear(), day.getMonth(), day.getDate()); };
// Deliberately fictional UI fixtures. Never injected into QueryClient or the Axios client.
export const previewPayments: PaymentPreview[] = [
    { id: 'preview-1', get name() {
            return t("common:m_1a8415416e0e");
        }, get subtitle() {
            return t("common:m_a81cab46eebf");
        }, date: relativeDay(1), amount: 899.90, currencyCode: 'TRY', category: 'internet', status: 'Pending' },
    { id: 'preview-2', get name() {
            return t("common:m_cde9b289b121");
        }, get subtitle() {
            return t("common:m_ad57a5c86aa1");
        }, date: relativeDay(2), amount: 199.99, currencyCode: 'TRY', category: 'music', status: 'Pending' },
    { id: 'preview-3', get name() {
            return t("common:m_8ea3b2614264");
        }, get subtitle() {
            return t("common:m_a81cab46eebf");
        }, date: relativeDay(3), amount: 8450, currencyCode: 'TRY', category: 'home', status: 'Pending' },
    { id: 'preview-4', get name() {
            return t("common:m_782916f8f2c5");
        }, get subtitle() {
            return t("common:m_80f230efe8c4");
        }, date: relativeDay(5), amount: 5.99, currencyCode: 'USD', category: 'cloud', status: 'Pending' },
    { id: 'preview-5', get name() {
            return t("common:m_654c3d5d92af");
        }, get subtitle() {
            return t("common:m_a81cab46eebf");
        }, date: relativeDay(6), amount: 640, currencyCode: 'TRY', category: 'energy', status: 'Pending' },
    { id: 'preview-6', get name() {
            return t("common:m_fd914780f3f4");
        }, get subtitle() {
            return t("common:m_41d28f6e4e7b");
        }, date: relativeDay(-1), amount: 450, currencyCode: 'TRY', category: 'education', status: 'Paid' },
    { id: 'preview-7', get name() {
            return t("common:m_8471ab6843ff");
        }, get subtitle() {
            return t("common:m_41d28f6e4e7b");
        }, date: relativeDay(-2), amount: 229.90, currencyCode: 'TRY', category: 'education', status: 'Skipped' },
];
export const previewSubscriptions: SubscriptionPreview[] = [
    { id: 'subscription-1', get name() {
            return t("common:m_cde9b289b121");
        }, get provider() {
            return t("common:m_c1579f388f19");
        }, amount: 199.99, currencyCode: 'TRY', category: 'music', renewal: relativeDay(2) },
    { id: 'subscription-2', get name() {
            return t("common:m_782916f8f2c5");
        }, get provider() {
            return t("common:m_80f230efe8c4");
        }, amount: 5.99, currencyCode: 'USD', category: 'cloud', renewal: relativeDay(5) },
    { id: 'subscription-3', get name() {
            return t("common:m_1f748b0ca081");
        }, get provider() {
            return t("common:m_9dd69ea580be");
        }, amount: 89.99, currencyCode: 'TRY', category: 'education', renewal: relativeDay(3), trialDays: 3 },
];
