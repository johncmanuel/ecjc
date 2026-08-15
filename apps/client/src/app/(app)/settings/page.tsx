"use client";

import { useState } from "react";
import StreakHero  from "@/components/streak/StreakHero";
import SettingsRow  from "@/components/streak/SettingsRow";

export default function SettingsPage() {
  const [reminder, setReminder] = useState(true);
  const [moneyPledge, setMoneyPledge] = useState(true);

  return (
    <div>
      <StreakHero count={14} />

      <div className="px-4.5 mt-5">
        <div className="text-[11px] uppercase tracking-wider text-ink-faint font-medium mb-2 px-1">
          If a day is missed
        </div>
        <SettingsRow
          title="Gentle reminder"
          description="A nudge, nothing loud"
          on={reminder}
          onToggle={() => setReminder((v) => !v)}
        />
        <SettingsRow
          title="Send a few dollars"
          description="Optional, just for you"
          on={moneyPledge}
          onToggle={() => setMoneyPledge((v) => !v)}
        />
      </div>
    </div>
  );
}