"use client";

import EntryCard from "@/components/timeline/EntryCard";
import { FabButton } from "@/components/ui/FabButton";
import { useRouter } from "next/navigation";
import { useEffect, useState, useRef, useCallback } from "react";
import { EntryResponse, UserProfileResponse } from "@/lib/api";
import { useGroups } from "@/components/GroupProvider";
import { useApi } from "@/hooks/useApi";
import { LogOut, UserPlus, Loader2 } from "lucide-react";
import { useScrollDirection } from "@/hooks/useScrollDirection";

export default function TimelinePage() {
  const router = useRouter();
  const api = useApi();
  const { groups, activeGroup, isLoading, refreshGroups } = useGroups();
  const [isLeaving, setIsLeaving] = useState(false);
  const [isReinviting, setIsReinviting] = useState(false);

  const [entries, setEntries] = useState<EntryResponse[]>([]);
  const [me, setMe] = useState<UserProfileResponse | null>(null);
  const [isLoadingEntries, setIsLoadingEntries] = useState(false);
  const [showLeaveModal, setShowLeaveModal] = useState(false);
  const scrollDirection = useScrollDirection();

  const [hasMore, setHasMore] = useState(true);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const observerTarget = useRef<HTMLDivElement>(null);

  // TODO: improve typing for the group members and entries, and handle cases where the data might be incomplete or missing.

  useEffect(() => {
    if (!isLoading && groups.length === 0) {
      router.push("/invite");
    }
  }, [isLoading, groups.length, router]);

  useEffect(() => {
    if (!activeGroup) return;
    
    async function fetchEntries() {
      setIsLoadingEntries(true);
      try {
        const [meData, entriesData] = await Promise.all([
          api.getMe(),
          api.getEntries(activeGroup!.id!, 0, 50)
        ]);
        setMe(meData);
        setEntries(entriesData.items || []);
        if (process.env.NODE_ENV !== 'production') {
          setHasMore(true); // Force true in dev so the mock generator can run
        } else {
          setHasMore((entriesData.items || []).length === 50);
        }
      } catch (err) {
        console.error("Failed to fetch entries", err);
      } finally {
        setIsLoadingEntries(false);
      }
    }
    
    fetchEntries();
  }, [activeGroup, api]);

  // Infinite scroll logic
  const loadMore = useCallback(async () => {
    if (isLoadingMore || !hasMore || !activeGroup) return;
    setIsLoadingMore(true);
    try {
      const skip = entries.length;
      const take = 50;
      
      let newItems: EntryResponse[] = [];

      // generate mock entries in development mode for testing infinite scrolling
      if (process.env.NODE_ENV !== 'production') {
        await new Promise(resolve => setTimeout(resolve, 800)); // fake delay
        newItems = Array.from({ length: take }).map((_, i) => ({
          id: `mock-${skip + i}`,
          textContent: `This is mock post #${skip + i + 1} to test infinite scrolling.`,
          authorId: me?.id || "mock-author",
          authorFirstName: "Mock",
          authorLastName: "User",
          createdAt: new Date(Date.now() - (skip + i) * 3600000), // 1 hour apart
          media: [],
          reactions: []
        }));
      } else {
        const data = await api.getEntries(activeGroup.id!, skip, take);
        newItems = data.items || [];
      }

      if (newItems.length === 0) {
        setHasMore(false);
      } else {
        setEntries(prev => {
          const existingIds = new Set(prev.map(e => e.id));
          const filtered = newItems.filter(item => !existingIds.has(item.id));
          return [...prev, ...filtered];
        });
        if (newItems.length < take) {
          setHasMore(false);
        }
      }
    } catch (err) {
      console.error("Failed to load more entries", err);
    } finally {
      setIsLoadingMore(false);
    }
  }, [activeGroup, api, entries.length, hasMore, isLoadingMore, me?.id]);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (observerEntries) => {
        if (observerEntries[0].isIntersecting) {
          loadMore();
        }
      },
      { threshold: 0.1, rootMargin: "200px" } // Load a bit before it enters the viewport
    );
    
    if (observerTarget.current) {
      observer.observe(observerTarget.current);
    }
    
    return () => observer.disconnect();
  }, [loadMore]);

  if (isLoading || !activeGroup) {
    return <div className="flex justify-center items-center h-64 text-ink-soft">Loading timeline...</div>;
  }

  // To properly find the partner we'd need our own ID, but since the group only has 2 people max,
  // we can find the one that hasLeft, or just show the UI if ANY member hasLeft.
  const hasLeftMember = activeGroup.members!.some(m => m.hasLeft);

  const confirmLeaveGroup = async () => {
    setShowLeaveModal(false);
    setIsLeaving(true);
    try {
      await api.leaveGroup(activeGroup.id!);
      await refreshGroups();
    } catch (err) {
      console.error(err);
      alert("Failed to leave group");
    } finally {
      setIsLeaving(false);
    }
  };

  const handleReinvite = async () => {
    setIsReinviting(true);
    try {
      await api.reinviteUser(activeGroup.id!);
      alert("Invite sent!");
    } catch (err: any) {
      console.error(err);
      alert(err?.message || "Failed to send invite");
    } finally {
      setIsReinviting(false);
    }
  };

  return (
    <main className="pb-24">
      <header 
        className="sticky top-[69px] z-40 bg-paper border-b border-line px-4 py-3 flex items-center justify-center mb-4 transition-transform duration-300"
        style={{ transform: scrollDirection === "down" ? "translateY(calc(-100% - 69px))" : "translateY(0)" }}
      >
        <div className="flex items-baseline gap-1.5">
          <span className="font-serif text-xl font-medium">{activeGroup.streakCount}</span>
          <span className="text-sm text-ink-soft">days sharing something, together</span>
        </div>
      </header>

      {hasLeftMember && (
        <div className="bg-amber-50 dark:bg-amber-950/30 border-b border-amber-200 dark:border-amber-900 px-4 py-3 text-sm text-amber-900 dark:text-amber-200 flex flex-col gap-2">
          <p>Your partner has left this group. This timeline is now read-only.</p>
          <button 
            onClick={handleReinvite}
            disabled={isReinviting}
            className="flex items-center gap-2 bg-amber-200 dark:bg-amber-900/50 hover:bg-amber-300 dark:hover:bg-amber-800 transition-colors w-fit px-3 py-1.5 rounded-lg font-medium text-xs disabled:opacity-50"
          >
            {isReinviting ? <Loader2 size={14} className="animate-spin" /> : <UserPlus size={14} />}
            Invite Them Back
          </button>
        </div>
      )}

      <section className="px-4 py-4">
        <div className="flex items-center justify-between mb-4">
          <div className="text-[11px] uppercase tracking-wider text-ink-faint font-medium">
            Today
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => setShowLeaveModal(true)}
              disabled={isLeaving}
              className="flex items-center gap-1.5 text-xs text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 px-2 py-1 rounded-md transition-colors disabled:opacity-50"
            >
              {isLeaving ? <Loader2 size={14} className="animate-spin" /> : <LogOut size={14} />}
              Leave Group
            </button>
          </div>
        </div>

        {isLoadingEntries ? (
          <div className="flex justify-center py-8">
            <Loader2 className="animate-spin text-ink-soft w-6 h-6" />
          </div>
        ) : entries.length === 0 && !hasMore ? (
          <div className="text-center py-8 text-ink-soft text-sm">
            No entries yet. Be the first to share something!
          </div>
        ) : (
          <>
            {entries.map(entry => {
              const isMe = entry.authorId === me?.id;
              const timeStr = entry.createdAt ? new Date(entry.createdAt).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) : '';
              
              return (
                <EntryCard
                  key={entry.id}
                  id={entry.id!}
                  author={isMe ? "a" : "b"}
                  authorName={isMe ? "You" : (entry.authorFirstName || "Someone")}
                  time={timeStr}
                  text={entry.textContent || ""}
                  media={entry.media || []}
                  reactions={entry.reactions || []}
                  currentUserId={me?.id || ""}
                />
              );
            })}
            
            {hasMore && (
              <div ref={observerTarget} className="flex justify-center py-6">
                {isLoadingMore ? (
                  <Loader2 className="animate-spin text-ink-soft w-5 h-5" />
                ) : (
                  // Empty placeholder to maintain height
                  <div className="h-5 w-5" /> 
                )}
              </div>
            )}
            
            {!hasMore && entries.length > 0 && (
              <div className="text-center py-8 text-ink-faint text-xs">
                You've reached the end of the timeline.
              </div>
            )}
          </>
        )}
      </section>

      {!hasLeftMember && <FabButton href="/compose" label="New entry" />}

      {showLeaveModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-sm">
          <div className="bg-paper dark:bg-card border border-line rounded-2xl p-6 max-w-sm w-full shadow-xl">
            <h3 className="font-serif text-xl font-medium text-ink mb-2">Leave Group</h3>
            <p className="text-sm text-ink-soft mb-6">
              Are you sure you want to leave this group? The data will remain for your partner, but you will lose access.
            </p>
            <div className="flex justify-end gap-3">
              <button 
                onClick={() => setShowLeaveModal(false)}
                className="px-4 py-2 rounded-lg text-sm font-medium text-ink hover:bg-paper-deep transition-colors"
              >
                No, cancel
              </button>
              <button 
                onClick={confirmLeaveGroup}
                className="px-4 py-2 rounded-lg text-sm font-medium bg-red-500 text-white hover:bg-red-600 transition-colors"
              >
                Yes, leave
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}
