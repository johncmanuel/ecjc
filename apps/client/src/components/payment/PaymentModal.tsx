"use client";

import { useEffect, useState } from "react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { X } from "lucide-react";
import { useApi } from "@/hooks/useApi";
import AmountStepper from "./AmountStepper";

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY || "pk_test_placeholder");

type SetupFormProps = {
  penaltyAmount: number;
  onSuccess: (amount: number) => void;
  onCancel: () => void;
};

function SetupForm({ penaltyAmount, onSuccess, onCancel }: SetupFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const api = useApi();
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit: React.SubmitEventHandler<HTMLFormElement> = async (e) => {
    e.preventDefault();
    if (!stripe || !elements) return;

    setLoading(true);
    const { error: submitError } = await stripe.confirmSetup({
      elements,
      confirmParams: {
        return_url: window.location.href,
      },
      redirect: 'if_required',
    });

    if (submitError) {
      setError(submitError.message || "An unknown error occurred.");
      setLoading(false);
      return;
    }

    try {
      await api.postApiSettingsPenalty({ isPenaltyEnabled: true, penaltyAmountCents: penaltyAmount * 100 });
      onSuccess(penaltyAmount);
    } catch (apiErr: any) {
      console.error(apiErr);
      setError("Payment method saved, but failed to update penalty settings. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="mt-4 space-y-6">
      <PaymentElement />
      {error && <div className="text-red-500 text-sm">{error}</div>}
      <div className="flex items-center gap-3 pt-2">
        <button 
          type="button" 
          onClick={onCancel}
          disabled={loading}
          className="flex-1 py-2 rounded-lg font-medium border border-line text-ink-soft hover:text-ink hover:bg-card transition-colors disabled:opacity-50"
        >
          Cancel
        </button>
        <button 
          type="submit"
          disabled={!stripe || loading}
          className="flex-1 bg-ink text-paper py-2 rounded-lg font-medium hover:bg-ink-soft transition-colors disabled:opacity-50"
        >
          {loading ? "Saving..." : "Save"}
        </button>
      </div>
    </form>
  );
}

type PaymentModalProps = {
  isOpen: boolean;
  onClose: () => void;
  initialAmount: number;
  onSuccess: (amount: number) => void;
};

export default function PaymentModal({ isOpen, onClose, initialAmount, onSuccess }: PaymentModalProps) {
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [penaltyAmount, setPenaltyAmount] = useState(initialAmount);
  const api = useApi();

  useEffect(() => {
    if (isOpen) {
      setPenaltyAmount(initialAmount);
      api.postApiStripeSetupIntent()
        .then(data => setClientSecret(data?.clientSecret || null))
        .catch(console.error);
    } else {
      setClientSecret(null); // Reset when closed
      setPenaltyAmount(initialAmount);
    }
  }, [isOpen, initialAmount, api]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-sm flex items-center justify-center p-4">
      <div className="bg-paper w-full max-w-md rounded-2xl shadow-xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        <div className="px-5 py-4 border-b border-line flex items-center justify-between">
          <h2 className="font-serif text-xl font-medium text-ink">Set up Penalty</h2>
          <button onClick={onClose} className="p-1 text-ink-faint hover:text-ink transition-colors rounded-full hover:bg-card">
            <X size={20} />
          </button>
        </div>
        
        <div className="p-5 max-h-[80vh] overflow-y-auto">
          <div className="mb-6">
            <p className="text-sm text-ink-soft mb-3">Choose penalty amount (Max $20)</p>
            <AmountStepper 
              amount={penaltyAmount} 
              onChange={(val) => setPenaltyAmount(Math.min(Math.max(5, val), 20))} 
              step={1} 
            />
          </div>

          <div className="border-t border-line pt-5">
            <h3 className="font-medium text-ink mb-4">Payment Details</h3>
            {!clientSecret ? (
              <div className="py-8 text-center text-sm text-ink-faint">Loading secure payment form...</div>
            ) : (
              <Elements stripe={stripePromise} options={{ clientSecret, appearance: { theme: 'stripe' } }}>
                <SetupForm penaltyAmount={penaltyAmount} onSuccess={onSuccess} onCancel={onClose} />
              </Elements>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
