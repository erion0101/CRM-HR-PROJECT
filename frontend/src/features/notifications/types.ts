export type NotificationType =
  | 'JourneyAssigned'
  | 'QuizUnlocked'
  | 'QuizFailed'
  | 'QuizPassed'
  | 'JourneyCompleted'
  | 'ReviewRequested'
  | 'StepOverdue';

export interface AppNotification {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  /** App-relative path to open when the notification is clicked. */
  linkPath: string;
  createdAtUtc: string;
  readAtUtc?: string;
}

export interface NotificationList {
  items: AppNotification[];
  unreadCount: number;
}
