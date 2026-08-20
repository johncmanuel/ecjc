import { useEffect, useState, useCallback } from "react";
import { X, ChevronLeft, ChevronRight, PlayCircle, PauseCircle } from "lucide-react";
import { MediaResponse } from "@/lib/api";

type MediaViewerModalProps = {
  media: MediaResponse[];
  initialIndex: number;
  onClose: () => void;
};

const AUTOPLAY_STORAGE_KEY = "ecjc_autoplay";

export default function MediaViewerModal({ media, initialIndex, onClose }: MediaViewerModalProps) {
  const [currentIndex, setCurrentIndex] = useState(initialIndex);
  
  // persist autoplay preference in localStorage 
  const [autoplayEnabled, setAutoplayEnabled] = useState(() => {
    if (typeof window !== "undefined") {
      const saved = localStorage.getItem(AUTOPLAY_STORAGE_KEY);
      if (saved !== null) return saved === "true";
    }
    return true; 
  });

  const toggleAutoplay = () => {
    const nextState = !autoplayEnabled;
    setAutoplayEnabled(nextState);
    if (typeof window !== "undefined") {
      localStorage.setItem(AUTOPLAY_STORAGE_KEY, String(nextState));
    }
  };

  const handlePrevious = useCallback(() => {
    setCurrentIndex((prev) => (prev > 0 ? prev - 1 : media.length - 1));
  }, [media.length]);

  const handleNext = useCallback(() => {
    setCurrentIndex((prev) => (prev < media.length - 1 ? prev + 1 : 0));
  }, [media.length]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "ArrowLeft") handlePrevious();
      if (e.key === "ArrowRight") handleNext();
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [handleNext, handlePrevious, onClose]);

  // Prevent scrolling on the body while modal is open
  useEffect(() => {
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = "auto";
    };
  }, []);

  const currentMedia = media[currentIndex];
  const isVideo = currentMedia?.mediaType === "Video";

  return (
    <div className="fixed inset-0 z-[100] bg-black flex flex-col items-center justify-center select-none touch-none">
      <div className="absolute top-0 inset-x-0 p-4 flex items-center justify-between z-10 bg-gradient-to-b from-black/60 to-transparent">
        <button
          onClick={toggleAutoplay}
          className="flex items-center gap-2 text-white/80 hover:text-white transition-colors text-sm font-medium bg-black/40 px-3 py-1.5 rounded-full backdrop-blur-md"
        >
          {autoplayEnabled ? <PlayCircle size={16} /> : <PauseCircle size={16} />}
          Autoplay: {autoplayEnabled ? "ON" : "OFF"}
        </button>

        <button
          onClick={onClose}
          className="p-2 text-white/80 hover:text-white bg-black/40 rounded-full backdrop-blur-md transition-colors"
          aria-label="Close"
        >
          <X size={24} />
        </button>
      </div>

      <div className="w-full h-full flex items-center justify-center relative">
        {isVideo ? (
          <video
            key={currentMedia.id} // force remount when index changes
            src={currentMedia.url}
            className="w-full h-full object-contain"
            controls
            autoPlay={autoplayEnabled}
            playsInline
            onClick={(e) => e.stopPropagation()}
          />
        ) : (
          <img
            src={currentMedia.url}
            alt="Viewed media"
            className="w-full h-full object-contain"
            onClick={(e) => e.stopPropagation()}
            draggable={false}
          />
        )}

        {media.length > 1 && (
          <>
            <div
              className="absolute left-0 inset-y-0 w-1/3 flex items-center justify-start p-4 cursor-pointer group"
              onClick={handlePrevious}
            >
              <div className="hidden sm:block p-3 rounded-full bg-black/20 text-white/0 group-hover:bg-black/50 group-hover:text-white backdrop-blur-sm transition-all">
                <ChevronLeft size={32} />
              </div>
            </div>
            
            <div
              className="absolute right-0 inset-y-0 w-1/3 flex items-center justify-end p-4 cursor-pointer group"
              onClick={handleNext}
            >
              <div className="hidden sm:block p-3 rounded-full bg-black/20 text-white/0 group-hover:bg-black/50 group-hover:text-white backdrop-blur-sm transition-all">
                <ChevronRight size={32} />
              </div>
            </div>
          </>
        )}
      </div>
      
      {media.length > 1 && (
        <div className="absolute bottom-6 left-1/2 -translate-x-1/2 flex items-center gap-1.5 z-10 bg-black/40 px-3 py-1.5 rounded-full backdrop-blur-md">
          {media.map((_, idx) => (
            <div
              key={idx}
              className={`w-1.5 h-1.5 rounded-full transition-colors ${
                idx === currentIndex ? "bg-white" : "bg-white/40"
              }`}
            />
          ))}
        </div>
      )}
    </div>
  );
}
