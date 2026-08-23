import { useState, useEffect } from "react";
import { useApi } from "@/hooks/useApi";
import { GroupMemberResponse } from "@/lib/api";
import { Loader2 } from "lucide-react";

interface SettlementModalProps {
  isOpen: boolean;
  onClose: () => void;
  accumulatedDebt: number;
  onSuccess: () => void;
}

export default function SettlementModal({ isOpen, onClose, accumulatedDebt, onSuccess }: SettlementModalProps) {
  const [members, setMembers] = useState<GroupMemberResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [isSettling, setIsSettling] = useState(false);
  const api = useApi();

  useEffect(() => {
    if (isOpen) {
      const fetchGroup = async () => {
        setLoading(true);
        try {
          const groups = await api.getApiGroups();
          if (groups.length > 0) {
            const groupId = groups[0].id;
            const details = await api.getGroupDetails(groupId);
            const me = await api.getMe();
            // Filter out current user and users who left
            setMembers(details.members.filter(m => m.userId !== me.id && !m.hasLeft));
          }
        } catch (e) {
          console.error(e);
        } finally {
          setLoading(false);
        }
      };
      fetchGroup();
    }
  }, [isOpen, api]);

  if (!isOpen) return null;

  const handleSettle = async () => {
    setIsSettling(true);
    try {
      await api.postApiSettingsPenaltySettle();
      onSuccess();
    } catch (e) {
      console.error(e);
    } finally {
      setIsSettling(false);
    }
  };

  const dividedDebt = members.length > 0 ? (accumulatedDebt / members.length).toFixed(2) : "0.00";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-ink/50 backdrop-blur-sm">
      <div className="bg-page w-full max-w-md rounded-2xl shadow-xl border border-line p-6 relative">
        <button 
          onClick={onClose}
          className="absolute top-4 right-4 text-ink-faint hover:text-ink"
        >
          ✕
        </button>
        
        <h2 className="text-xl font-serif font-medium text-ink mb-2">Settle Your Debt</h2>
        <p className="text-sm text-ink-faint mb-6">
          You owe a total of ${accumulatedDebt.toFixed(2)}. Pay your friends directly using the links below.
        </p>

        {loading ? (
          <div className="flex justify-center py-8">
            <Loader2 className="w-8 h-8 animate-spin text-ink-soft" />
          </div>
        ) : (
          <div className="space-y-6">
            {members.length === 0 ? (
              <div className="text-sm text-ink-faint text-center py-4">
                You don't have any active friends in your group to pay.
              </div>
            ) : (
              members.map(member => {
                const name = member.firstName ? `${member.firstName} ${member.lastName}` : "Your friend";
                const hasAnyHandle = member.venmoHandle || member.cashAppHandle || member.payPalHandle;

                return (
                  <div key={member.userId} className="p-4 bg-card border border-line rounded-xl">
                    <div className="flex items-center justify-between mb-3">
                      <span className="font-medium text-ink">{name}</span>
                      <span className="text-sm font-medium text-red-500">${dividedDebt}</span>
                    </div>

                    {!hasAnyHandle ? (
                      <p className="text-xs text-ink-faint bg-page p-2 rounded border border-line">
                        This user hasn't configured any payment handles yet. You'll have to ask them where to send the cash!
                      </p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {member.venmoHandle && (
                          <a 
                            href={`https://venmo.com/?txn=pay&audience=private&recipients=${member.venmoHandle.replace('@', '')}&amount=${dividedDebt}&note=ECJC%20Penalty`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs font-medium bg-[#008CFF] text-white px-3 py-1.5 rounded-lg hover:bg-blue-600 transition-colors"
                          >
                            Venmo
                          </a>
                        )}
                        {member.cashAppHandle && (
                          <a 
                            href={`https://cash.app/$${member.cashAppHandle.replace('$', '')}/${dividedDebt}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs font-medium bg-[#00D632] text-white px-3 py-1.5 rounded-lg hover:bg-green-600 transition-colors"
                          >
                            Cash App
                          </a>
                        )}
                        {member.payPalHandle && (
                          <a 
                            href={`https://paypal.me/${member.payPalHandle.replace('@', '')}/${dividedDebt}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs font-medium bg-[#003087] text-white px-3 py-1.5 rounded-lg hover:bg-blue-800 transition-colors"
                          >
                            PayPal
                          </a>
                        )}
                      </div>
                    )}
                  </div>
                );
              })
            )}

            <div className="pt-4 border-t border-line">
              <button 
                onClick={handleSettle}
                disabled={isSettling}
                className="w-full py-3 bg-ink text-page rounded-xl font-medium hover:bg-ink-soft transition-colors flex items-center justify-center disabled:opacity-50"
              >
                {isSettling ? <Loader2 className="w-5 h-5 mr-2 animate-spin" /> : null}
                Sent the money!
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
