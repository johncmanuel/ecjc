"use client";

import { useState } from "react";
import Candle from "@/components/ui/Candle";
import Button from "@/components/ui/Buttton";
import AmountStepper from "@/components/payment/AmountStepper";

export default function StreakBrokenPage() {
  const [amount, setAmount] = useState(5);

  return (
    <div className="flex flex-col min-h-[calc(100vh-73px)]">
      <div className="px-4.5 py-3.5 flex justify-end">
        <button aria-label="Dismiss" className="text-ink-soft text-[15px]">
          ✕
        </button>
      </div>

      <div className="flex-1 px-6 pt-2 pb-6 flex flex-col items-center text-center">
        <Candle lit={false} />
        <h1 className="font-serif text-lg font-medium mt-4.5 mb-2">
          The streak paused
        </h1>
        <p className="text-[12.5px] text-ink-soft leading-relaxed max-w-[210px] mb-6.5">
          Yesterday went by without an entry. You asked to send a little
          something when that happens.
        </p>

        <AmountStepper amount={amount} onChange={setAmount} />

        <Button variant="thread-a" className="w-full">
          <span className="font-serif italic font-semibold">stripe</span>
          Send ${amount}
        </Button>

        <button className="text-[12.5px] text-ink-faint mt-4">
          Not this time
        </button>

        <p className="text-[10.5px] text-ink-faint mt-auto pt-5">
          Payments are optional and only apply to accounts that turn this on.
        </p>
      </div>
    </div>
  );
}