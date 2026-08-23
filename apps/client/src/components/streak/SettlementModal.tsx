import { useState, useEffect } from "react";
import { useApi } from "@/hooks/useApi";
import { GroupMemberResponse } from "@/lib/api";
import { Loader2 } from "lucide-react";

interface SettlementModalProps {
  isOpen: boolean;
  onClose: () => void;
  accumulatedDebt: number;
  groupId: string;
  onSuccess: () => void;
}

export default function SettlementModal({ isOpen, onClose, accumulatedDebt, groupId, onSuccess }: SettlementModalProps) {
  const [members, setMembers] = useState<GroupMemberResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [isSettling, setIsSettling] = useState(false);
  const api = useApi();

  useEffect(() => {
    if (isOpen) {
      const fetchGroup = async () => {
        setLoading(true);
        try {
          const groupDetails = await api.getGroupDetails(groupId);
          const me = await api.getMe();
          const friends = (groupDetails.members || []).filter(
            m => m.userId && m.userId !== me.id && m.hasLeft !== true
          );
          setMembers(friends as GroupMemberResponse[]);
        } catch (e) {
          console.error(e);
        } finally {
          setLoading(false);
        }
      };
      fetchGroup();
    }
  }, [isOpen, api, groupId]);

  if (!isOpen) return null;

  const handleSettle = async () => {
    setIsSettling(true);
    try {
      await api.postApiSettingsPenaltySettle(groupId);
      onSuccess();
    } catch (e) {
      console.error(e);
    } finally {
      setIsSettling(false);
    }
  };

  const dividedDebt = members.length > 0 ? (accumulatedDebt / members.length).toFixed(2) : "0.00";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
      <div className="relative w-full max-w-sm bg-paper rounded-xl shadow-xl overflow-hidden mx-4 flex flex-col">
        <div className="px-4.5 py-3.5 flex justify-end">
          <button 
            onClick={onClose}
            aria-label="Dismiss" 
            className="text-ink-soft hover:text-ink transition-colors text-[15px]"
          >
            ✕
          </button>
        </div>
        
        <div className="flex-1 px-6 pt-2 pb-6 flex flex-col items-center text-center">
          <h2 className="font-serif text-lg font-medium mb-2">Settle Your Debt</h2>
          <p className="text-[12.5px] text-ink-soft leading-relaxed max-w-[240px] mb-6.5">
            You owe a total of ${accumulatedDebt.toFixed(2)}. Pay your friends directly using the links below.
          </p>

          {loading ? (
            <div className="flex justify-center py-8 w-full">
              <Loader2 className="w-8 h-8 animate-spin text-ink-soft" />
            </div>
          ) : (
            <div className="space-y-4 w-full">
              {members.length === 0 ? (
                <div className="text-[12.5px] text-ink-soft text-center py-4">
                  You don't have any active friends in your group to pay.
                </div>
              ) : (
                members.map(member => {
                  const name = member.firstName ? `${member.firstName} ${member.lastName}` : "Your friend";
                  const hasAnyHandle = member.venmoHandle || member.cashAppHandle || member.payPalHandle;

                  return (
                    <div key={member.userId} className="p-4 bg-card border border-line rounded-xl text-left">
                      <div className="flex items-center justify-between mb-3">
                        <span className="font-medium text-[13.5px] text-ink">{name}</span>
                        <span className="text-[13.5px] font-medium text-red-500">${dividedDebt}</span>
                      </div>

                      {!hasAnyHandle ? (
                        <p className="text-[11.5px] text-ink-faint bg-paper p-2 rounded border border-line">
                          This user hasn't configured any payment handles yet. You'll have to ask them where to send the cash!
                        </p>
                      ) : (
                        <div className="flex flex-wrap gap-2">
                          {member.venmoHandle && (
                            <a 
                              href={`https://venmo.com/${member.venmoHandle.replace('@', '')}?txn=pay&amount=${dividedDebt}&note=ECJC%20Penalty`}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-[11.5px] font-medium bg-[#008CFF] text-white px-3 py-1.5 rounded-lg hover:bg-blue-600 transition-colors"
                            >
                              Venmo
                            </a>
                          )}
                          {member.cashAppHandle && (
                            <a 
                              href={`https://cash.app/$${member.cashAppHandle.replace('$', '')}/${dividedDebt}`}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-[11.5px] font-medium bg-[#00D632] text-white px-3 py-1.5 rounded-lg hover:bg-green-600 transition-colors"
                            >
                              Cash App
                            </a>
                          )}
                          {member.payPalHandle && (
                            <a 
                              href={`https://paypal.me/${member.payPalHandle.replace('@', '')}/${dividedDebt}`}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-[11.5px] font-medium bg-[#003087] text-white px-3 py-1.5 rounded-lg hover:bg-blue-800 transition-colors"
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

              <div className="pt-2">
                <button 
                  onClick={handleSettle}
                  disabled={isSettling}
                  className="w-full py-3 bg-paper border border-line text-ink rounded-lg font-medium hover:border-ink-soft transition-colors flex items-center justify-center disabled:opacity-50 text-[13.5px]"
                >
                  {isSettling ? <Loader2 className="w-5 h-5 mr-2 animate-spin" /> : null}
                  Sent the money!
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
