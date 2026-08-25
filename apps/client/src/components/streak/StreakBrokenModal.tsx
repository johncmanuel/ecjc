"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Candle from "@/components/ui/Candle";
import Button from "@/components/ui/Button";
import AmountStepper from "@/components/payment/AmountStepper";

export default function StreakBrokenModal() {
  const [amount, setAmount] = useState(5);
  const router = useRouter();

  const handleDismiss = () => {
    // If modal was opened via soft navigation, call router.back().
    // If it was opened via hard navigation, router.back() might not have history,
    // so push it to the homepage.
    if (window.history.length > 1) {
      router.back();
    } else {
      router.push("/");
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div className="relative w-full max-w-sm bg-paper rounded-xl shadow-xl overflow-hidden mx-4 flex flex-col">
        <div className="px-4.5 py-3.5 flex justify-end">
          <button 
            onClick={handleDismiss} 
            aria-label="Dismiss" 
            className="text-ink-soft hover:text-ink transition-colors text-[15px]"
          >
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

          <AmountStepper amount={amount} onChange={setAmount} className="mb-6" />

          <Button variant="thread-a" className="w-full">
            <span className="font-serif italic font-semibold">stripe</span>
            Send ${amount}
          </Button>

          <button onClick={handleDismiss} className="text-[12.5px] text-ink-faint mt-4 hover:text-ink-soft transition-colors">
            Not this time
          </button>

          <p className="text-[10.5px] text-ink-faint mt-8">
            Payments are optional and only apply to accounts that turn this on.
          </p>
        </div>
      </div>
    </div>
  );
}
