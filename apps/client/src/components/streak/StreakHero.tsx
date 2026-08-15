import Candle from "@/components/ui/Candle";

type StreakHeroProps = {
  count: number;
  caption?: string;
};

export default function StreakHero({ count, caption = "days sharing something, together" }: StreakHeroProps) {
  return (
    <div className="flex flex-col items-center text-center pt-6 pb-2">
      <Candle lit />
      <div className="font-serif text-4xl font-medium mt-4">{count}</div>
      <div className="text-xs text-ink-soft mt-2">{caption}</div>
      <div className="flex justify-center mt-4">
        <div className="w-7 h-7 rounded-full bg-thread-a border-2 border-paper" />
        <div className="w-7 h-7 rounded-full bg-thread-b border-2 border-paper -ml-2.5" />
      </div>
    </div>
  );
}