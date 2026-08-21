"use client";

import { useState } from "react";
import SettingsRow  from "@/components/streak/SettingsRow";
import SignOutButton from "@/components/ui/SignOutButton";
import PaymentModal from "@/components/payment/PaymentModal";
import { useApi } from "@/hooks/useApi";

export default function SettingsPage() {
  const [moneyPledge, setMoneyPledge] = useState(false);
  const [penaltyAmount, setPenaltyAmount] = useState(5);
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);
  const api = useApi();

  const handleTogglePledge = async (v: boolean) => {
    if (v) {
      setIsPaymentModalOpen(true);
    } else {
      setMoneyPledge(false);
      try {
        await api.postApiSettingsPenalty({ isPenaltyEnabled: false, penaltyAmountCents: penaltyAmount * 100 });
      } catch (e) {
        console.error(e);
      }
    }
  };

  const handlePaymentSuccess = (amount: number) => {
    setPenaltyAmount(amount);
    setMoneyPledge(true);
    setIsPaymentModalOpen(false);
  };

  return (
    <div>
      <h1 className="font-serif text-3xl font-medium px-5 pt-8 pb-2 text-ink">Settings</h1>

      <div className="px-4.5 mt-5">
        <div className="text-[11px] uppercase tracking-wider text-ink-faint font-medium mb-2 px-1">
          If a day is missed
        </div>
       <SettingsRow
          title="Send a few dollars"
          description={moneyPledge ? `Current penalty: $${penaltyAmount}` : "Optional, just for you"}
          on={moneyPledge}
          onToggle={() => handleTogglePledge(!moneyPledge)}
        />
        
        {moneyPledge && (
          <div className="px-1 mt-2 mb-4">
            <button 
              onClick={() => setIsPaymentModalOpen(true)}
              className="text-sm font-medium text-ink hover:text-ink-soft transition-colors px-3 py-1.5 rounded-md bg-card border border-line"
            >
              Configure Penalty Settings
            </button>
          </div>
        )}
        
        <div className="mt-8 px-1">
          <SignOutButton />
        </div>
      </div>

      <PaymentModal 
        isOpen={isPaymentModalOpen} 
        onClose={() => setIsPaymentModalOpen(false)} 
        initialAmount={penaltyAmount}
        onSuccess={handlePaymentSuccess}
      />
    </div>
  );
}