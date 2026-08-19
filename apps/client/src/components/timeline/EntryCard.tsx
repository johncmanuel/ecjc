import ThreadLine from "../ui/ThreadLine";
import { MediaResponse } from "@/lib/api";
import { useState } from "react";
import MediaViewerModal from "../ui/MediaViewerModal";

type EntryCardProps = {
  author: "a" | "b";
  authorName: string;
  time: string;
  text: string;
  media?: MediaResponse[];
};

export default function EntryCard({ author, authorName, time, text, media }: EntryCardProps) {
  const [selectedMediaIndex, setSelectedMediaIndex] = useState<number | null>(null);

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