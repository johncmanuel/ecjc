import Candle from "@/components/ui/Candle";

export default function SignInPage() {
  return (
    <div className="w-full max-w-xs flex flex-col items-center text-center gap-10">
      <div className="flex flex-col items-center">
        <Candle lit />
        <h1 className="font-serif text-2xl font-medium mt-5 mb-2">
            ecjc 
        </h1>
        <p className="font-serif italic text-sm text-ink-soft leading-relaxed max-w-[210px]">
          A private thread between you and one other person.
        </p>
      </div>

      <div className="w-full">
        <button className="w-full flex items-center justify-center gap-2.5 rounded-full border border-line bg-card px-5 py-3.5 text-sm font-medium text-ink">
          <span
            className="w-4 h-4 rounded-full flex-shrink-0"
            style={{
              background:
                "conic-gradient(from 0deg, #6E8F8A 0deg 90deg, #D3A24E 90deg 180deg, #9C8AA0 180deg 270deg, #B7BDAF 270deg 360deg)",
            }}
          />
          Continue with Google
        </button>
        <p className="text-[11px] text-ink-faint mt-4 leading-relaxed max-w-[220px] mx-auto">
          Only you and whoever you invite will ever see what&apos;s shared here.
        </p>
      </div>
    </div>
  );
}
