import StreakBrokenModal from "@/components/streak/StreakBrokenModal";
import TimelinePage from "../page";

export default function StreakBrokenFallbackPage() {
  return (
    <>
      <TimelinePage />
      <StreakBrokenModal />
    </>
  );
}