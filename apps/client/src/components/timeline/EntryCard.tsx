import  ThreadLine from "../ui/ThreadLine";

type EntryCardProps = {
  author: "a" | "b";
  authorName: string;
  time: string;
  text: string;
  hasMedia?: boolean;
};

export default function EntryCard({ author, authorName, time, text, hasMedia }: EntryCardProps) {
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
        {hasMedia && (
          <div
            className={`mt-2 h-32 rounded-xl bg-gradient-to-br ${
              author === "a"
                ? "from-thread-a-soft to-paper-deep"
                : "from-thread-b-soft to-paper-deep"
            }`}
          />
        )}
      </div>
    </div>
  );
}