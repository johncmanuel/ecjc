"use client";

import { useApi } from "@/hooks/useApi";
import { PendingInviteResponse } from "@/lib/api";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Check, X, Loader2, Bell } from "lucide-react";
import { useGroups } from "@/components/GroupProvider";

export default function NotificationsPage() {
  const api = useApi();
  const router = useRouter();
  const { refreshGroups } = useGroups();
  const [invites, setInvites] = useState<PendingInviteResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [actioningId, setActioningId] = useState<string | null>(null);

  useEffect(() => {
    async function loadInvites() {
      try {
        const pending = await api.getPendingInvites();
        setInvites(pending);
      } catch (err) {
        console.error("Failed to load invites", err);
      } finally {
        setIsLoading(false);
      }
    }
    loadInvites();
  }, [api]);

  const handleAccept = async (inviteId: string) => {
    setActioningId(inviteId);
    try {
      await api.acceptInvite(inviteId);
      setInvites((prev) => prev.filter((i) => i.id !== inviteId));
      await refreshGroups();
      router.push("/");
    } catch (err) {
      console.error("Failed to accept invite", err);
    } finally {
      setActioningId(null);
    }
  };

  const handleDecline = async (inviteId: string) => {
    setActioningId(inviteId);
    try {
      await api.declineInvite(inviteId);
      setInvites((prev) => prev.filter((i) => i.id !== inviteId));
    } catch (err) {
      console.error("Failed to decline invite", err);
    } finally {
      setActioningId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64 text-ink-soft">
        <Loader2 className="w-5 h-5 animate-spin" />
      </div>
    );
  }

  return (
    <main className="px-4 py-6">
      <div className="flex items-center gap-2 mb-6">
        <Bell size={18} className="text-ink-soft" />
        <h2 className="text-sm font-medium tracking-wide uppercase text-ink-faint">
          Notifications
        </h2>
      </div>

      {invites.length === 0 ? (
        <div className="text-center py-12">
          <p className="text-ink-soft text-sm">No pending invites.</p>
          <p className="text-ink-faint text-xs mt-1">
            When someone invites you, it will appear here.
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {invites.map((invite) => (
            <div
              key={invite.id}
              className="flex items-center gap-3 p-4 rounded-xl border border-line bg-card"
            >
              <div className="shrink-0">
                {invite.inviterImage ? (
                  <img
                    src={invite.inviterImage}
                    alt=""
                    className="w-10 h-10 rounded-full object-cover"
                  />
                ) : (
                  <div className="w-10 h-10 rounded-full bg-paper-deep flex items-center justify-center text-ink-faint text-sm font-medium">
                    {(invite.inviterFirstName?.[0] || "?").toUpperCase()}
                  </div>
                )}
              </div>

              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-ink truncate">
                  {[invite.inviterFirstName, invite.inviterLastName]
                    .filter(Boolean)
                    .join(" ") || "Someone"}
                </p>
                <p className="text-xs text-ink-faint">wants to share with you</p>
              </div>

              <div className="flex items-center gap-2 shrink-0">
                <button
                  onClick={() => handleAccept(invite.id!)}
                  disabled={actioningId === invite.id}
                  className="p-2 rounded-full bg-emerald-100 text-emerald-700 hover:bg-emerald-200 dark:bg-emerald-900/40 dark:text-emerald-400 dark:hover:bg-emerald-900/60 transition-colors disabled:opacity-50"
                  title="Accept invite"
                >
                  {actioningId === invite.id ? (
                    <Loader2 size={18} className="animate-spin" />
                  ) : (
                    <Check size={18} />
                  )}
                </button>
                <button
                  onClick={() => handleDecline(invite.id!)}
                  disabled={actioningId === invite.id}
                  className="p-2 rounded-full bg-red-100 text-red-700 hover:bg-red-200 dark:bg-red-900/40 dark:text-red-400 dark:hover:bg-red-900/60 transition-colors disabled:opacity-50"
                  title="Decline invite"
                >
                  <X size={18} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </main>
  );
}
