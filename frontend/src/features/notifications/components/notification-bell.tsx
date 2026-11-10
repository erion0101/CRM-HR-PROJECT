import { useMutation, useQuery } from '@tanstack/react-query';
import { Bell } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router';

import { queryClient } from '@/app/providers/query-provider';

import { getMyNotifications, markAllNotificationsRead, markNotificationRead } from '../api/notifications-api';
import type { AppNotification } from '../types';
import { timeAgo } from '../utils';

const queryKey = ['notifications', 'mine'];

export function NotificationBell() {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const query = useQuery({
    queryKey,
    queryFn: getMyNotifications,
    refetchInterval: 60_000,
    refetchOnWindowFocus: true
  });
  const refresh = () => queryClient.invalidateQueries({ queryKey });
  const markRead = useMutation({ mutationFn: markNotificationRead, onSuccess: refresh });
  const markAll = useMutation({ mutationFn: markAllNotificationsRead, onSuccess: refresh });

  useEffect(() => {
    if (!open) return;
    const onPointer = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false);
    };
    document.addEventListener('pointerdown', onPointer);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('pointerdown', onPointer);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const unread = query.data?.unreadCount ?? 0;
  const items = query.data?.items ?? [];
  const openNotification = (notification: AppNotification) => {
    if (!notification.readAtUtc) markRead.mutate(notification.id);
    setOpen(false);
    navigate(notification.linkPath);
  };

  return (
    <div className="notification-bell" ref={containerRef}>
      <button
        className="notification-bell__trigger"
        aria-label={unread ? `Notifications, ${unread} unread` : 'Notifications'}
        aria-expanded={open}
        onClick={() => setOpen((value) => !value)}
      >
        <Bell size={19} />
        {unread ? <span className="notification-bell__count">{unread > 99 ? '99+' : unread}</span> : null}
      </button>
      {open ? (
        <div className="notification-bell__panel" role="dialog" aria-label="Notifications">
          <header>
            <strong>Notifications</strong>
            {unread ? (
              <button className="notification-bell__link" disabled={markAll.isPending} onClick={() => markAll.mutate()}>
                Mark all as read
              </button>
            ) : null}
          </header>
          {items.length ? (
            <ul>
              {items.map((notification) => (
                <li key={notification.id}>
                  <button
                    className={`notification-item ${notification.readAtUtc ? '' : 'is-unread'}`}
                    onClick={() => openNotification(notification)}
                  >
                    <strong>{notification.title}</strong>
                    <span>{notification.message}</span>
                    <small>
                      {notification.readAtUtc ? '' : 'New · '}
                      {timeAgo(notification.createdAtUtc)}
                    </small>
                  </button>
                </li>
              ))}
            </ul>
          ) : (
            <p className="notification-bell__empty">You are all caught up.</p>
          )}
        </div>
      ) : null}
    </div>
  );
}
