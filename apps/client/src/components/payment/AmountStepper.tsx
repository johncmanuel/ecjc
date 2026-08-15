type AmountStepperProps = {
  amount: number;
  onChange: (next: number) => void;
  step?: number;
};

export default function AmountStepper({ amount, onChange, step = 1 }: AmountStepperProps) {
  return (
    <div className="flex items-center gap-5 mb-7">
      <button
        type="button"
        aria-label="Decrease amount"
        onClick={() => onChange(Math.max(step, amount - step))}
        className="w-8 h-8 rounded-full border border-line bg-card text-ink-soft text-[15px] flex items-center justify-center"
      >
        -
      </button>
      <div className="font-serif text-[34px] font-medium min-w-[70px] text-center">
        ${amount}
      </div>
      <button
        type="button"
        aria-label="Increase amount"
        onClick={() => onChange(amount + step)}
        className="w-8 h-8 rounded-full border border-line bg-card text-ink-soft text-[15px] flex items-center justify-center"
      >
        +
      </button>
    </div>
  );
}