import { t, useLocaleRevision } from "../../i18n/runtime";
import { useEffect } from 'react';
import { AppState } from 'react-native';
import { router } from 'expo-router';
import { useSessionStore } from '../../store/session';
import { session, apiClient } from '../../services/auth-runtime';
import { queryClient } from '../../services/query-client';
import { pushAvailability, connectPush, receiveTarget, usePushState, loadNotifications } from './push-runtime';
export function PushBootstrap() {
    useLocaleRevision();
    const status = useSessionStore(s => s.status);
    const pending = usePushState(s => s.pending);
    useEffect(() => {
        if (pushAvailability())
            return;
        let disposed = false;
        const subscriptions: {
            remove(): void;
        }[] = [];
        void Promise.resolve().then(async () => {
            const n = loadNotifications();
            if (disposed)
                return;
            n.setNotificationHandler({ handleNotification: async () => ({ shouldShowBanner: session.getSnapshot().status === 'authenticated', shouldShowList: session.getSnapshot().status === 'authenticated', shouldPlaySound: false, shouldSetBadge: false }) });
            const receive = (response: import('expo-notifications').NotificationResponse) => { if (response.actionIdentifier === n.DEFAULT_ACTION_IDENTIFIER)
                receiveTarget(response.notification.request.content.data); void n.clearLastNotificationResponseAsync().catch(() => { }); };
            subscriptions.push(n.addNotificationResponseReceivedListener(receive));
            // The listener returns a native token: re-fetch the Expo token rather than sending FCM directly.
            subscriptions.push(n.addPushTokenListener(() => { void connectPush(); }));
            subscriptions.push(n.addNotificationReceivedListener(() => { const owner = session.getSnapshot().user; const epoch = session.credential()?.epoch; if (owner)
                void queryClient.invalidateQueries({ queryKey: ['finance', owner.id, epoch] }); }));
            const last = await n.getLastNotificationResponseAsync();
            if (!disposed && last)
                receive(last);
        }).catch(() => usePushState.setState({ error: t("reminders:m_4239abd5c0fe") }));
        const active = AppState.addEventListener('change', state => { if (state === 'active')
            void connectPush(); });
        return () => { disposed = true; subscriptions.forEach(s => s.remove()); active.remove(); };
    }, []);
    useEffect(() => { if (status === 'authenticated')
        void connectPush();
    else
        usePushState.setState({ registered: false }); }, [status]);
    useEffect(() => {
        if (!pending || status === 'restoring' || status === 'authenticating')
            return;
        if (status === 'anonymous') {
            router.replace('/(auth)/login');
            return;
        }
        const epoch = session.credential()?.epoch;
        let cancelled = false;
        const request = { headers: {}, sessionEpoch: epoch };
        void apiClient.get(`/api/${pending.kind === 'payment' ? 'payments' : 'subscriptions'}/${pending.resourceId}`, request).then(() => {
            if (!cancelled && session.credential()?.epoch === epoch) {
                usePushState.setState({ pending: null });
                router.push({ pathname: pending.kind === 'payment' ? '/payment' : '/subscription', params: { id: pending.resourceId } });
            }
        }).catch(error => {
            if (!cancelled && session.credential()?.epoch === epoch) {
                usePushState.setState({ pending: null });
                router.push({ pathname: '/information', params: { title: t("reminders:m_449704e67047"), message: error?.status === 404 ? t("reminders:m_1065fb378e00") : t("reminders:m_af7041d035cb") } });
            }
        });
        return () => { cancelled = true; };
    }, [pending, status]);
    return null;
}
