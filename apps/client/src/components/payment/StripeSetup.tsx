"use client";

import { useEffect, useState } from "react";
import { loadStripe } from "@stripe/stripe-js";
import { Elements, PaymentElement, useStripe, useElements } from "@stripe/react-stripe-js";
import { useApi } from "@/hooks/useApi";

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY || "pk_test_placeholder");

function SetupForm() {
  const stripe = useStripe();
  const [error, setError] = useState<string | null>(null);
  const elements = useElements();
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

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
    } else {
      setSuccess(true);
    }
    setLoading(false);
  };

  if (success) {
    return <div className="p-4 text-green-600 bg-green-50 rounded-lg text-sm">Payment method saved successfully!</div>;
  }

  // TODO: could make this a modal instead
  return (
    <form onSubmit={handleSubmit} className="mt-4 space-y-4">
      <PaymentElement />
      {error && <div className="text-red-500 text-sm">{error}</div>}
      <button 
        disabled={!stripe || loading}
        className="w-full bg-ink text-paper py-2 rounded-lg font-medium hover:bg-ink-soft transition-colors disabled:opacity-50"
      >
        {loading ? "Saving..." : "Save Payment Method"}
      </button>
    </form>
  );
}

export default function StripeSetup() {
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const api = useApi();

  useEffect(() => {
    api.postApiStripeSetupIntent()
      .then(data => setClientSecret(data?.clientSecret || null))
      .catch(console.error);
  }, [api]);

  if (!clientSecret) return <div className="p-4 text-sm text-ink-faint">Loading secure payment form...</div>;

  return (
    <div className="bg-paper p-4 rounded-xl border border-line">
      <h3 className="font-medium text-ink mb-4">Payment Details</h3>
      <Elements stripe={stripePromise} options={{ clientSecret, appearance: { theme: 'stripe' } }}>
        <SetupForm />
      </Elements>
    </div>
  );
}
