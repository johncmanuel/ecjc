import EntryCard from "@/components/timeline/EntryCard";
import Candle from "@/components/ui/Candle";

export default function TimelinePage() {
  return (
    <main>
      <section className="px-4 py-4">
        <div className="text-[11px] uppercase tracking-wider text-ink-faint font-medium mb-2">
          Today
        </div>

        <EntryCard
          author="b"
          authorName="Emmanuel" // TODO: change to name pulled from database
          time="6:42 PM"
          text="Long day, but the sky was doing that pink thing again on the drive home."
          hasMedia
        />
        <EntryCard
          author="a"
          authorName="You"
          time="1:15 PM"
          text="Finally fixed that bug that's been haunting me all week. Small victories."
        />
      </section>

      <section className="flex flex-col items-center py-8 border-t border-line">
        <Candle lit />
        <div className="font-serif text-4xl font-medium mt-4">14</div>
        <div className="text-xs text-ink-soft mt-1">days sharing something, together</div>
      </section>
    </main>
  );
}
