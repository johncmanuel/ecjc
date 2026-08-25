interface AmountStepperProps {
  amount: number;
  onChange: (newAmount: number) => void;
  className?: string;
}

export default function AmountStepper({ amount, onChange, className = "" }: AmountStepperProps) {
  return (
    <div className={`flex items-center space-x-4 ${className}`}>
      <button 
        onClick={() => onChange(Math.max(1, amount - 1))}
        className="px-3 py-1 bg-gray-200 rounded text-gray-700"
      >
        -
      </button>
      <span className="text-xl font-medium">${amount}</span>
      <button 
        onClick={() => onChange(amount + 1)}
        className="px-3 py-1 bg-gray-200 rounded text-gray-700"
      >
        +
      </button>
    </div>
  );
}
