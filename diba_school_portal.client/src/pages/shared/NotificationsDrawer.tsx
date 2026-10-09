import { useEffect, useState } from "react";
import { X, Bell, Check, Inbox } from "lucide-react";
import api from "../../lib/api";

interface Notification {
  id: string;
  title: string;
  message: string;
  category: string;
  isRead: boolean;
  createdAt: string;
}

interface Props {
  open: boolean;
  onClose: () => void;
}

export default function NotificationsDrawer({ open, onClose }: Props) {
  const [items, setItems] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    (async () => {
      try {
        const { data } = await api.get<Notification[]>("/notificationlog/my");
        setItems(Array.isArray(data) ? data : []);
      } finally {
        setLoading(false);
      }
    })();
  }, [open]);

  const markRead = async (id: string) => {
    await api.post(`/notificationlog/mark-read/${id}`);
    setItems((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
  };

  const timeAgo = (iso: string) => {
    const diff = Date.now() - new Date(iso).getTime();
    const m = Math.floor(diff / 60000);
    if (m < 1) return "Just now";
    if (m < 60) return `${m}m ago`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  };

  if (!open) return null;

  return (
    <>
      <div className="fixed inset-0 bg-slate-900/40 z-40" onClick={onClose} aria-hidden />
      <aside className="fixed top-0 right-0 h-full w-full max-w-md bg-white z-50 shadow-2xl flex flex-col">
        <header className="h-16 px-5 flex items-center justify-between border-b border-slate-100 shrink-0">
          <div className="flex items-center gap-2">
            <Bell className="w-4 h-4 text-slate-500" />
            <h2 className="text-[14px] font-semibold text-slate-900">Notifications</h2>
          </div>
          <button onClick={onClose} className="p-2 rounded-lg text-slate-400 hover:bg-slate-100">
            <X className="w-4 h-4" />
          </button>
        </header>

        <div className="flex-1 overflow-y-auto">
          {loading ? (
            <div className="p-5 text-center text-[13px] text-slate-400">Loading…</div>
          ) : items.length === 0 ? (
            <div className="flex flex-col items-center justify-center text-center py-16 px-6">
              <div className="w-14 h-14 rounded-2xl bg-slate-50 grid place-items-center mb-4">
                <Inbox className="w-6 h-6 text-slate-400" />
              </div>
              <p className="text-[14px] font-semibold text-slate-800">All caught up</p>
              <p className="text-[12px] text-slate-400 mt-1">No notifications yet.</p>
            </div>
          ) : (
            <div className="divide-y divide-slate-100">
              {items.map((n) => (
                <button
                  key={n.id}
                  type="button"
                  onClick={() => !n.isRead && markRead(n.id)}
                  className={`w-full text-left p-5 hover:bg-slate-50 transition-colors ${
                    !n.isRead ? "bg-emerald-50/30" : ""
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <div className={`w-9 h-9 rounded-xl grid place-items-center shrink-0 ${
                      !n.isRead ? "bg-emerald-100 text-emerald-600" : "bg-slate-100 text-slate-500"
                    }`}>
                      <Bell className="w-4 h-4" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between gap-2">
                        <p className={`text-[13px] ${!n.isRead ? "font-semibold text-slate-900" : "font-medium text-slate-700"}`}>
                          {n.title}
                        </p>
                        {n.isRead && <Check className="w-3 h-3 text-slate-300 shrink-0" />}
                      </div>
                      <p className="text-[12px] text-slate-500 mt-1 leading-relaxed">{n.message}</p>
                      <p className="text-[11px] text-slate-400 mt-2">{timeAgo(n.createdAt)}</p>
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      </aside>
    </>
  );
}