"use client";

import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import { useCentrifugo, type CentrifugoEvent } from "@/hooks/useCentrifugo";
import { X } from "lucide-react";

interface NotificationToast {
  id: string;
  message: string;
  type: "success" | "info" | "warning";
}

interface NotificationContextType {
  toasts: NotificationToast[];
  addToast: (toast: Omit<NotificationToast, "id">) => void;
  removeToast: (id: string) => void;
}

const NotificationContext = createContext<NotificationContextType>({
  toasts: [],
  addToast: () => {},
  removeToast: () => {},
});

export const useNotifications = () => useContext(NotificationContext);

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<NotificationToast[]>([]);

  const addToast = useCallback((toast: Omit<NotificationToast, "id">) => {
    const id = crypto.randomUUID();
    setToasts((prev) => [...prev, { ...toast, id }]);
    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, 5000);
  }, []);

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const handleCentrifugoEvent = useCallback(
    (event: CentrifugoEvent) => {
      switch (event.type) {
        case "InviteReceived":
          addToast({
            message: `${event.inviterName || "Someone"} sent you an invite!`,
            type: "info",
          });
          break;
        case "InviteAccepted":
          addToast({
            message: `${event.userName || "Your partner"} accepted your invite! 🎉`,
            type: "success",
          });
          break;
        case "InviteDeclined":
          addToast({
            message: `${event.userName || "Someone"} declined your invite.`,
            type: "warning",
          });
          break;
        case "InviteCancelled":
          addToast({
            message: `An invite was cancelled.`,
            type: "info",
          });
          break;
        case "MemberLeft":
          addToast({
            message: `Your partner left the group.`,
            type: "warning",
          });
          break;
      }
    },
    [addToast]
  );

  useCentrifugo(handleCentrifugoEvent);

  return (
    <NotificationContext.Provider value={{ toasts, addToast, removeToast }}>
      {children}
      <div className="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-sm">
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={`
              px-4 py-3 rounded-xl shadow-lg border backdrop-blur-sm
              flex items-center justify-between gap-3
              animate-[slideIn_0.3s_ease-out]
              ${
                toast.type === "success"
                  ? "bg-emerald-50/90 border-emerald-200 text-emerald-900 dark:bg-emerald-950/90 dark:border-emerald-800 dark:text-emerald-100"
                  : toast.type === "warning"
                    ? "bg-amber-50/90 border-amber-200 text-amber-900 dark:bg-amber-950/90 dark:border-amber-800 dark:text-amber-100"
                    : "bg-paper/90 border-line text-ink"
              }
            `}
          >
            <span className="text-sm font-medium">{toast.message}</span>
            <button
              onClick={() => removeToast(toast.id)}
              className="p-1 rounded-full hover:bg-black/10 dark:hover:bg-white/10 transition-colors shrink-0"
            >
              <X size={14} />
            </button>
          </div>
        ))}
      </div>
    </NotificationContext.Provider>
  );
}
