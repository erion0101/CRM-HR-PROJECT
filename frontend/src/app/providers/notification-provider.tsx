/* eslint-disable react-refresh/only-export-components -- provider and its colocated hook share one private context */
import { CheckCircle2, CircleAlert, Info, X } from 'lucide-react';
import { createContext, useCallback, useContext, useMemo, useState, type PropsWithChildren } from 'react';

type NotificationTone = 'success' | 'error' | 'info';

interface NotificationInput {
  title: string;
  message?: string;
  tone?: NotificationTone;
}

interface Notification extends NotificationInput {
  id: number;
}

interface NotificationContextValue {
  notify: (notification: NotificationInput) => void;
}

const NotificationContext = createContext<NotificationContextValue | null>(null);

export function NotificationProvider({ children }: PropsWithChildren) {
  const [notifications, setNotifications] = useState<Notification[]>([]);

  const dismiss = useCallback((id: number) => {
    setNotifications((items) => items.filter((item) => item.id !== id));
  }, []);

  const notify = useCallback((input: NotificationInput) => {
    const id = Date.now() + Math.random();
    setNotifications((items) => [...items, { id, tone: 'info', ...input }]);
    window.setTimeout(() => dismiss(id), 5_000);
  }, [dismiss]);

  const value = useMemo(() => ({ notify }), [notify]);

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <div className="toast-region" aria-live="polite" aria-label="Notifications">
        {notifications.map((item) => {
          const Icon = item.tone === 'success' ? CheckCircle2 : item.tone === 'error' ? CircleAlert : Info;
          return (
            <div className={`toast toast--${item.tone}`} key={item.id} role="status">
              <Icon size={20} aria-hidden="true" />
              <div><strong>{item.title}</strong>{item.message ? <p>{item.message}</p> : null}</div>
              <button className="icon-button icon-button--small" onClick={() => dismiss(item.id)} aria-label="Dismiss notification"><X size={16} /></button>
            </div>
          );
        })}
      </div>
    </NotificationContext.Provider>
  );
}

export function useNotification(): NotificationContextValue {
  const context = useContext(NotificationContext);
  if (!context) throw new Error('useNotification must be used inside NotificationProvider.');
  return context;
}
