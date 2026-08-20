"use client";

import { useApi } from "@/hooks/useApi";
import { UserProfileResponse, PendingInviteResponse, SentInviteResponse } from "@/lib/api";
import Link from "next/link";
import { useEffect, useState, useCallback } from "react";
import { Copy, ArrowRight, Loader2, Bell, X } from "lucide-react";

export default function InvitePage() {
  const api = useApi();
  
  const [profile, setProfile] = useState<UserProfileResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [copied, setCopied] = useState(false);
  
  const [pendingInvites, setPendingInvites] = useState<PendingInviteResponse[]>([]);
  const [sentInvites, setSentInvites] = useState<SentInviteResponse[]>([]);
  
  const [friendCode, setFriendCode] = useState("");
  const [isJoining, setIsJoining] = useState(false);
  const [error, setError] = useState("");
  const [cancelingId, setCancelingId] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    try {
      const [me, pending, sent] = await Promise.all([
        api.getMe(),
        api.getPendingInvites(),
        api.getSentInvites()
      ]);
      setProfile(me);
      setPendingInvites(pending);
      setSentInvites(sent);
    } catch (err) {
      console.error("Failed to load data", err);
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleCopy = async () => {
    if (profile?.friendCode) {
      await navigator.clipboard.writeText(profile.friendCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleJoin: React.FormEventHandler<HTMLFormElement> = async (e) => {
    e.preventDefault();
    if (!friendCode.trim()) return;

    setIsJoining(true);
    setError("");

    try {
      const group = await api.createGroup();
      await api.inviteUserToGroup(group.id!, { friendCode: friendCode.trim() });

      setFriendCode("");
      await loadData(); 
    } catch (err: any) {
      console.error(err);
      setError(err?.message || "Failed to send invite. Please check the friend code.");
    } finally {
      setIsJoining(false);
    }
  };

  const handleCancelInvite = async (inviteId: string) => {
    setCancelingId(inviteId);
    try {
      await api.cancelInvite(inviteId);
      setSentInvites(prev => prev.filter(i => i.id !== inviteId));
    } catch (err) {
      console.error("Failed to cancel invite", err);
    } finally {
      setCancelingId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="w-6 h-6 animate-spin text-ink-soft" />
      </div>
    );
  }

  return (
    <main className="flex flex-col items-center justify-center p-6 font-sans text-ink">
      <div className="w-full max-w-md space-y-10">
        
        {pendingInvites.length > 0 && (
          <Link
            href="/notifications"
            className="flex items-center gap-3 p-4 rounded-xl border border-emerald-200 bg-emerald-50/50 dark:border-emerald-800 dark:bg-emerald-950/30 transition-colors hover:bg-emerald-50 dark:hover:bg-emerald-950/50"
          >
            <Bell size={18} className="text-emerald-600 dark:text-emerald-400 shrink-0" />
            <div className="flex-1">
              <p className="text-sm font-medium text-emerald-900 dark:text-emerald-100">
                You have {pendingInvites.length} pending invite{pendingInvites.length > 1 ? "s" : ""}!
              </p>
              <p className="text-xs text-emerald-700 dark:text-emerald-300">Tap to view</p>
            </div>
            <ArrowRight size={16} className="text-emerald-600 dark:text-emerald-400" />
          </Link>
        )}

        <div className="text-center space-y-4">
          <h1 className="text-3xl font-serif font-medium">Welcome.</h1>
          <p className="text-ink-soft">
            Invite someone to start sharing memories.
          </p>
        </div>

        <div className="space-y-4 p-6 bg-paper-deep rounded-xl border border-line">
          <h2 className="text-sm font-medium tracking-wide uppercase text-ink-faint">Your Friend Code</h2>
          <div className="flex items-center gap-2">
            <code className="flex-1 p-3 bg-paper rounded-lg text-sm text-ink break-all border border-line">
              {profile?.friendCode}
            </code>
            <button 
              onClick={handleCopy}
              className="p-3 bg-black dark:bg-white text-white dark:text-black rounded-lg transition-transform active:scale-95 flex items-center justify-center min-w-[48px]"
              title="Copy to clipboard"
            >
              {copied ? <span className="text-xs font-medium">Copied!</span> : <Copy className="w-5 h-5" />}
            </button>
          </div>
          <p className="text-xs text-ink-soft">
            Send this code to a friend so they can invite you.
          </p>
        </div>

        <div className="flex items-center gap-4 text-ink-faint">
          <div className="flex-1 h-px bg-line"></div>
          <span className="text-xs uppercase tracking-widest">Or</span>
          <div className="flex-1 h-px bg-line"></div>
        </div>

        <div className="space-y-4">
          <h2 className="text-sm font-medium tracking-wide uppercase text-ink-faint">Invite a Friend</h2>
          <form onSubmit={handleJoin} className="space-y-3">
            <div className="flex items-center gap-2">
              <input
                type="text"
                placeholder="Paste their friend code..."
                value={friendCode}
                onChange={(e) => setFriendCode(e.target.value)}
                className="flex-1 p-3 bg-transparent border-b-2 border-line focus:border-ink outline-none transition-colors text-ink placeholder:text-ink-faint"
                required
              />
              <button 
                type="submit"
                disabled={isJoining || !friendCode.trim()}
                className="p-3 rounded-full hover:bg-paper-deep disabled:opacity-50 disabled:hover:bg-transparent transition-colors"
              >
                {isJoining ? <Loader2 className="w-6 h-6 animate-spin text-ink-soft" /> : <ArrowRight className="w-6 h-6" />}
              </button>
            </div>
            {error && (
              <p className="text-sm text-red-500">{error}</p>
            )}
          </form>
        </div>

        {sentInvites.length > 0 && (
          <div className="space-y-4 pt-6 border-t border-line">
            <h2 className="text-sm font-medium tracking-wide uppercase text-ink-faint">Sent Invites</h2>
            <div className="space-y-3">
              {sentInvites.map((invite) => (
                <div key={invite.id} className="flex items-center justify-between p-4 bg-paper-deep rounded-xl border border-line">
                  <div className="flex items-center gap-3">
                    {invite.inviteeImage ? (
                      <img src={invite.inviteeImage} alt="Profile" className="w-10 h-10 rounded-full object-cover" />
                    ) : (
                      <div className="w-10 h-10 rounded-full bg-line flex items-center justify-center text-ink-soft font-medium">
                        {invite.inviteeFirstName?.[0] || "?"}
                      </div>
                    )}
                    <div>
                      <p className="text-sm font-medium">
                        {invite.inviteeFirstName} {invite.inviteeLastName}
                      </p>
                      <p className="text-xs text-ink-soft">Pending</p>
                    </div>
                  </div>
                  <button
                    onClick={() => handleCancelInvite(invite.id!)}
                    disabled={cancelingId === invite.id}
                    className="p-2 text-ink-soft hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 rounded-lg transition-colors disabled:opacity-50"
                    title="Cancel Invite"
                  >
                    {cancelingId === invite.id ? <Loader2 className="w-5 h-5 animate-spin" /> : <X className="w-5 h-5" />}
                  </button>
                </div>
              ))}
            </div>
          </div>
        )}

      </div>
    </main>
  );
}
