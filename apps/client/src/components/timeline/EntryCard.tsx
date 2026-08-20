import ThreadLine from "../ui/ThreadLine";
import { MediaResponse, ReactionResponse } from "@/lib/api";
import { useState, useRef, useEffect } from "react";
import MediaViewerModal from "../ui/MediaViewerModal";
import { useApi } from "@/hooks/useApi";
import MinimalEmojiPicker from "../ui/MinimalEmojiPicker";

type EntryCardProps = {
  id: string;
  author: "a" | "b";
  authorName: string;
  time: string;
  text: string;
  media?: MediaResponse[];
  reactions?: ReactionResponse[];
  currentUserId: string;
};

export default function EntryCard({ id, author, authorName, time, text, media, reactions, currentUserId }: EntryCardProps) {
  const [selectedMediaIndex, setSelectedMediaIndex] = useState<number | null>(null);
  const [localReactions, setLocalReactions] = useState<ReactionResponse[]>(reactions || []);
  const [showEmojiPicker, setShowEmojiPicker] = useState(false);
  const api = useApi();
  const pickerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setLocalReactions(reactions || []);
  }, [reactions]);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (pickerRef.current && !pickerRef.current.contains(event.target as Node)) {
        setShowEmojiPicker(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleToggleReaction = async (emojiCode: string) => {
    const hasReacted = localReactions.some(r => r.emojiCode === emojiCode && r.userId === currentUserId);
    
    if (hasReacted) {
      setLocalReactions(prev => prev.filter(r => !(r.emojiCode === emojiCode && r.userId === currentUserId)));
      try {
        await api.removeReaction(id, emojiCode);
      } catch (e) {
        console.error("Failed to remove reaction", e);
        setLocalReactions(prev => [...prev, { emojiCode, userId: currentUserId }]);
      }
    } else {
      const optimisticReaction: ReactionResponse = { id: Math.random().toString(), emojiCode, userId: currentUserId };
      setLocalReactions(prev => [...prev, optimisticReaction]);
      try {
        const response = await api.addReaction(id, { emojiCode });
        setLocalReactions(prev => prev.map(r => r === optimisticReaction ? response : r));
      } catch (e) {
        console.error("Failed to add reaction", e);
        setLocalReactions(prev => prev.filter(r => r !== optimisticReaction));
      }
    }
    setShowEmojiPicker(false);
  };

  const reactionGroups = localReactions.reduce((acc, r) => {
    if (!acc[r.emojiCode!]) acc[r.emojiCode!] = [];
    acc[r.emojiCode!].push(r);
    return acc;
  }, {} as Record<string, ReactionResponse[]>);

  return (
    <div className="flex gap-3 mb-4">
      <ThreadLine author={author} />
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline gap-2 mb-1">
          <span
            className={`text-[12.5px] font-semibold ${
              author === "a" ? "text-thread-a" : "text-thread-b"
            }`}
          >
            {authorName}
          </span>
          <span className="text-[11px] text-ink-faint">{time}</span>
        </div>
        <p className="font-serif text-[15px] leading-relaxed text-ink">{text}</p>
        {media && media.length > 0 && (
          <div className={`mt-2 grid gap-2 ${media.length > 1 ? 'grid-cols-2' : 'grid-cols-1'}`}>
            {media.map((m, idx) => {
              const isVideo = m.mediaType === "Video";
              return (
                <div 
                  key={m.id} 
                  className="relative h-48 rounded-xl bg-paper-deep border border-line overflow-hidden cursor-pointer group"
                  onClick={() => setSelectedMediaIndex(idx)}
                >
                  {isVideo ? (
                    <video src={m.url} className="w-full h-full object-cover group-hover:opacity-90 transition-opacity" playsInline />
                  ) : (
                    <img src={m.url} alt="Attached media" className="w-full h-full object-cover group-hover:opacity-90 transition-opacity" />
                  )}
                </div>
              );
            })}
          </div>
        )}

        <div className="mt-3 flex flex-wrap gap-2 items-center relative">
          {Object.entries(reactionGroups).map(([emoji, reacts]) => {
            const hasReacted = reacts.some(r => r.userId === currentUserId);
            return (
              <button
                key={emoji}
                onClick={() => handleToggleReaction(emoji)}
                className={`flex items-center gap-1.5 px-2 py-1 rounded-full text-[13px] border transition-colors ${
                  hasReacted 
                    ? "bg-blue-500/10 border-blue-500/30 text-blue-600 dark:text-blue-400" 
                    : "bg-paper border-line text-ink-soft hover:bg-paper-deep"
                }`}
              >
                <span>{emoji}</span>
                <span className="font-medium text-[11px]">{reacts.length}</span>
              </button>
            );
          })}
          
          <div className="relative" ref={pickerRef}>
            <button 
              onClick={() => setShowEmojiPicker(!showEmojiPicker)}
              className="flex items-center justify-center w-7 h-7 rounded-full bg-paper border border-line text-ink-soft hover:bg-paper-deep transition-colors"
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="10"></circle>
                <path d="M8 14s1.5 2 4 2 4-2 4-2"></path>
                <line x1="9" y1="9" x2="9.01" y2="9"></line>
                <line x1="15" y1="9" x2="15.01" y2="9"></line>
              </svg>
            </button>
            
            {showEmojiPicker && (
              <div className="absolute top-full left-0 mt-2 z-50">
                <MinimalEmojiPicker onSelect={(emoji) => handleToggleReaction(emoji)} />
              </div>
            )}
          </div>
        </div>
      </div>

      {selectedMediaIndex !== null && media && (
        <MediaViewerModal
          media={media}
          initialIndex={selectedMediaIndex}
          onClose={() => setSelectedMediaIndex(null)}
        />
      )}
    </div>
  );
}