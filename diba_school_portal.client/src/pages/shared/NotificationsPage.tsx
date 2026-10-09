import { useEffect, useState } from "react";
import { Bell, Check, Inbox } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Spinner, Badge } from "../../components/ui";
import { formatDateTime } from "../../lib/format";
import api from "../../lib/api";

interface Notification {
  id: string;
  title: string;
  message: string;
  category: string;
  isRead: boolean;
  createdAt: string;
}

export default function NotificationsPage() {
  const [items, setItems] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<Notification[]>("/notificationlog/my");
        setItems(Array.isArray(data) ? data : []);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const markRead = async (id: string) => {
    await api.post(`/notificationlog/mark-read/${id}`);
    setItems((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
  };

  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Inbox" title="Notifications" description="Every alert sent to your account." />
      <Card padding="none">
        {loading ? (
          <div className="py-16 flex justify-center"><Spinner size="lg" label="Loading…" /></div>
        ) : items.length === 0 ? (
          <EmptyState icon={Inbox} title="You're all caught up" description="No notifications yet." />
        ) : (
          <div className="divide-y divide-slate-100">
            {items.map((n) => (
              <button
                key={n.id}
                type="button"
                onClick={() => !n.isRead && markRead(n.id)}
                className={`w-full text-left p-5 hover:bg-slate-50 ${!n.isRead ? "bg-emerald-50/30" : ""}`}
              >
                <div className="flex items-start gap-4">
                  <div className={`w-9 h-9 rounded-xl grid place-items-center shrink-0 ${
                    !n.isRead ? "bg-emerald-100 text-emerald-600" : "bg-slate-100 text-slate-500"
                  }`}>
                    <Bell className="w-4 h-4" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className={`text-[13px] ${!n.isRead ? "font-semibold text-slate-900" : "font-medium text-slate-700"}`}>
                        {n.title}
                      </p>
                      {n.category && <Badge variant="neutral">{n.category}</Badge>}
                      {n.isRead && <Check className="w-3 h-3 text-slate-300" />}
                    </div>
                    <p className="text-[12px] text-slate-500 mt-1 leading-relaxed">{n.message}</p>
                    <p className="text-[11px] text-slate-400 mt-2">{formatDateTime(n.createdAt)}</p>
                  </div>
                </div>
              </button>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}