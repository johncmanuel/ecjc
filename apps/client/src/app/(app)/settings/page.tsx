"use client";

import { useState, useEffect } from "react";
import SettingsRow  from "@/components/streak/SettingsRow";
import SignOutButton from "@/components/ui/SignOutButton";
import SettlementModal from "@/components/streak/SettlementModal";
import { useApi } from "@/hooks/useApi";
import { UserProfileResponse, ApiKeyResponse } from "@/lib/api";
import { Copy, Trash2, KeyRound, Save, Loader2, Check } from "lucide-react";

export default function SettingsPage() {
  const [moneyPledge, setMoneyPledge] = useState(false);
  const [accumulatedPenalty, setAccumulatedPenalty] = useState(0);
  const [isSettlementModalOpen, setIsSettlementModalOpen] = useState(false);
  
  const [profile, setProfile] = useState<UserProfileResponse | null>(null);
  const [isEditingProfile, setIsEditingProfile] = useState(false);
  const [editFirstName, setEditFirstName] = useState("");
  const [editLastName, setEditLastName] = useState("");
  const [editName, setEditName] = useState("");
  
  const [editVenmo, setEditVenmo] = useState("");
  const [editCashApp, setEditCashApp] = useState("");
  const [editPayPal, setEditPayPal] = useState("");

  const [isSavingProfile, setIsSavingProfile] = useState(false);

  const [apiKeys, setApiKeys] = useState<ApiKeyResponse[]>([]);
  const [newKeyName, setNewKeyName] = useState("");
  const [generatedToken, setGeneratedToken] = useState<string | null>(null);
  const [isGeneratingKey, setIsGeneratingKey] = useState(false);
  const [copied, setCopied] = useState(false);

  const api = useApi();

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [profileRes, keysRes, penaltyRes] = await Promise.all([
          api.getMe(),
          api.getApiSettingsApiKeys(),
          api.getApiSettingsPenalty().catch(() => null)
        ]);

        if (profileRes) {
          setProfile(profileRes);
          setEditFirstName(profileRes.firstName || "");
          setEditLastName(profileRes.lastName || "");
          setEditName(profileRes.firstName ? `${profileRes.firstName} ${profileRes.lastName}`.trim() : (profileRes.name || ""));
          setEditVenmo(profileRes.venmoHandle || "");
          setEditCashApp(profileRes.cashAppHandle || "");
          setEditPayPal(profileRes.payPalHandle || "");
        }

        if (keysRes) setApiKeys(keysRes);

        if (penaltyRes) {
          setMoneyPledge(penaltyRes.isPenaltyEnabled);
          setAccumulatedPenalty(penaltyRes.accumulatedPenaltyCents ? penaltyRes.accumulatedPenaltyCents / 100 : 0);
        }
      } catch (e) {
        console.error("Failed to load settings data", e);
      }
    };
    
    fetchData();
  }, [api]);

  const handleTogglePledge = async (v: boolean) => {
    setMoneyPledge(v);
    try {
      await api.postApiSettingsPenalty({ isPenaltyEnabled: v, penaltyAmountCents: 500 });
    } catch (e) {
      console.error(e);
      setMoneyPledge(!v); // Revert on failure
    }
  };

  const handleSaveProfile = async () => {
    setIsSavingProfile(true);
    try {
      const updated = await api.updateMe({
        firstName: editFirstName,
        lastName: editLastName,
        name: editName,
        venmoHandle: editVenmo,
        cashAppHandle: editCashApp,
        payPalHandle: editPayPal
      });
      setProfile(updated);
      setIsEditingProfile(false);
    } catch (e) {
      console.error("Failed to update profile", e);
    } finally {
      setIsSavingProfile(false);
    }
  };

  const handleCreateApiKey = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newKeyName.trim()) return;
    
    setIsGeneratingKey(true);
    try {
      const res = await api.postApiSettingsApiKeys({ name: newKeyName.trim() });
      setApiKeys([...apiKeys, res.keyDetails]);
      setGeneratedToken(res.token);
      setNewKeyName("");
      setCopied(false);
    } catch (e) {
      console.error("Failed to create API key", e);
    } finally {
      setIsGeneratingKey(false);
    }
  };

  const handleRevokeApiKey = async (id: string) => {
    if (!confirm("Are you sure you want to revoke this API key? This action cannot be undone.")) return;
    
    try {
      await api.deleteApiSettingsApiKeys(id);
      setApiKeys(apiKeys.filter(k => k.id !== id));
    } catch (e) {
      console.error("Failed to revoke API key", e);
    }
  };

  const handleCopyToken = () => {
    if (generatedToken) {
      navigator.clipboard.writeText(generatedToken);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="pb-20">
      <h1 className="font-serif text-3xl font-medium px-5 pt-8 pb-2 text-ink">Settings</h1>

      <div className="px-4.5 mt-5 space-y-10">
        
        {/* PROFILE SECTION */}
        <section>
          <div className="text-[11px] uppercase tracking-wider text-ink-faint font-medium mb-3 px-1">
            Profile Settings
          </div>
          
          <div className="bg-card border border-line rounded-xl overflow-hidden p-4">
            {isEditingProfile ? (
              <div className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-medium text-ink-faint mb-1">First Name</label>
                    <input 
                      type="text" 
                      value={editFirstName} 
                      onChange={(e) => setEditFirstName(e.target.value)}
                      className="w-full bg-page border border-line rounded-lg px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink-soft transition-colors"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-ink-faint mb-1">Last Name</label>
                    <input 
                      type="text" 
                      value={editLastName} 
                      onChange={(e) => setEditLastName(e.target.value)}
                      className="w-full bg-page border border-line rounded-lg px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink-soft transition-colors"
                    />
                  </div>
                </div>
                
                <div>
                  <label className="block text-xs font-medium text-ink-faint mb-1">Display Name / Pseudonym</label>
                  <input 
                    type="text" 
                    value={editName} 
                    onChange={(e) => setEditName(e.target.value)}
                    className="w-full bg-page border border-line rounded-lg px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink-soft transition-colors"
                  />
                </div>

                <div className="pt-2 border-t border-line mt-4">
                  <p className="text-xs text-ink-faint mb-3">Add your handles to easily receive penalties from friends.</p>
                  <div className="space-y-3">
                    <div>
                      <label className="block text-xs font-medium text-ink-faint mb-1">Venmo Handle</label>
                      <input 
                        type="text" 
                        placeholder="@username"
                        value={editVenmo} 
                        onChange={(e) => setEditVenmo(e.target.value)}
                        className="w-full bg-page border border-line rounded-lg px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink-soft transition-colors"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-ink-faint mb-1">Cash App Handle</label>
                      <input 
                        type="text" 
                        placeholder="$cashtag"
                        value={editCashApp} 
                        onChange={(e) => setEditCashApp(e.target.value)}
                        className="w-full bg-page border border-line rounded-lg px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink-soft transition-colors"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-medium text-ink-faint mb-1">PayPal Handle</label>
                      <input 
                        type="text" 
                        placeholder="username"
                        value={editPayPal} 
                        onChange={(e) => setEditPayPal(e.target.value)}
                        className="w-full bg-page border border-line rounded-lg px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink-soft transition-colors"
                      />
                    </div>
                  </div>
                </div>
                
                <div className="flex justify-end space-x-2 pt-2">
                  <button 
                    onClick={() => {
                      setIsEditingProfile(false);
                      setEditFirstName(profile?.firstName || "");
                      setEditLastName(profile?.lastName || "");
                      setEditName(profile?.name || "");
                      setEditVenmo(profile?.venmoHandle || "");
                      setEditCashApp(profile?.cashAppHandle || "");
                      setEditPayPal(profile?.payPalHandle || "");
                    }}
                    className="px-4 py-2 text-sm font-medium text-ink-soft hover:text-ink transition-colors"
                  >
                    Cancel
                  </button>
                  <button 
                    onClick={handleSaveProfile}
                    disabled={isSavingProfile}
                    className="flex items-center px-4 py-2 text-sm font-medium bg-ink text-page rounded-lg hover:bg-ink-soft transition-colors disabled:opacity-50"
                  >
                    {isSavingProfile ? <Loader2 className="w-4 h-4 mr-2 animate-spin" /> : <Save className="w-4 h-4 mr-2" />}
                    Save Changes
                  </button>
                </div>
              </div>
            ) : (
              <div>
                <div className="flex justify-between items-center mb-4">
                  <div>
                    <h3 className="text-base font-medium text-ink">
                      {profile?.firstName ? `${profile.firstName} ${profile.lastName}` : profile?.name || "Loading..."}
                    </h3>
                    <p className="text-sm text-ink-faint">
                      {profile?.email}
                    </p>
                  </div>
                  <button 
                    onClick={() => setIsEditingProfile(true)}
                    className="px-4 py-2 text-sm font-medium bg-page border border-line rounded-lg hover:border-ink-soft transition-colors text-ink"
                  >
                    Edit
                  </button>
                </div>
                <div className="pt-3 border-t border-line text-sm text-ink-faint">
                  <div className="grid grid-cols-3 gap-2">
                    <div>
                      <span className="block text-[10px] uppercase font-medium mb-0.5">Venmo</span>
                      <span className="text-ink">{profile?.venmoHandle || "Not set"}</span>
                    </div>
                    <div>
                      <span className="block text-[10px] uppercase font-medium mb-0.5">Cash App</span>
                      <span className="text-ink">{profile?.cashAppHandle || "Not set"}</span>
                    </div>
                    <div>
                      <span className="block text-[10px] uppercase font-medium mb-0.5">PayPal</span>
                      <span className="text-ink">{profile?.payPalHandle || "Not set"}</span>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </section>

        {/* PENALTY SECTION */}
        <section>
          <div className="text-[11px] uppercase tracking-wider text-ink-faint font-medium mb-2 px-1">
            If a day is missed
          </div>
          <SettingsRow
            title="Honor Code Penalty"
            description={moneyPledge ? "You are committed to paying $5 if you miss a day" : "Enable to owe $5 to your friends if you fail"}
            on={moneyPledge}
            onToggle={() => handleTogglePledge(!moneyPledge)}
          />
          
          {moneyPledge && accumulatedPenalty > 0 && (
            <div className="px-1 mt-3 flex items-center justify-between">
              <div className="flex items-center space-x-3 bg-red-500/10 border border-red-500/20 px-3 py-2 rounded-lg w-full">
                <span className="text-sm font-medium text-red-600 flex-1">
                  Accumulated Debt: ${accumulatedPenalty.toFixed(2)}
                </span>
                <button 
                  onClick={() => setIsSettlementModalOpen(true)}
                  className="text-xs font-medium bg-red-600 hover:bg-red-700 text-white px-3 py-1.5 rounded-lg transition-colors shadow-sm"
                >
                  Mark as Settled
                </button>
              </div>
            </div>
          )}
        </section>

        {/* API KEYS SECTION */}
        <section>
          <div className="flex items-center justify-between mb-3 px-1">
            <div className="text-[11px] uppercase tracking-wider text-ink-faint font-medium">
              Developer Settings
            </div>
          </div>
          
          <div className="bg-card border border-line rounded-xl p-5 space-y-6">
            <div>
              <h3 className="text-base font-medium text-ink mb-1 flex items-center">
                <KeyRound className="w-4 h-4 mr-2 text-ink-soft" />
                Personal Access Tokens
              </h3>
              <p className="text-sm text-ink-faint leading-relaxed">
                Tokens you generate can be used to authenticate with the ECJC API. These tokens give an agent full access to your account data.
              </p>
            </div>

            {generatedToken && (
              <div className="bg-green-500/10 border border-green-500/20 rounded-lg p-4 mb-4">
                <p className="text-sm text-green-700 font-medium mb-2">
                  Key generated successfully! Copy this token now. You will not be able to see it again.
                </p>
                <div className="flex items-center space-x-2">
                  <code className="flex-1 bg-page border border-green-500/20 rounded-md px-3 py-2 text-sm font-mono text-green-800 break-all">
                    {generatedToken}
                  </code>
                  <button 
                    onClick={handleCopyToken}
                    className="p-2 bg-page border border-green-500/20 text-green-700 rounded-md hover:bg-green-500/10 transition-colors"
                  >
                    {copied ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                  </button>
                </div>
              </div>
            )}

            {apiKeys.length > 0 ? (
              <div className="space-y-3">
                {apiKeys.map(key => (
                  <div key={key.id} className="flex items-center justify-between p-3 bg-page border border-line rounded-lg">
                    <div>
                      <p className="text-sm font-medium text-ink">{key.name}</p>
                      <div className="flex items-center mt-1 space-x-3 text-xs text-ink-faint">
                        <code className="px-1.5 py-0.5 bg-card border border-line rounded font-mono">{key.prefix}</code>
                        <span>Created {new Date(key.createdAt).toLocaleDateString()}</span>
                      </div>
                    </div>
                    <button 
                      onClick={() => handleRevokeApiKey(key.id)}
                      className="p-2 text-red-500 hover:bg-red-500/10 rounded-md transition-colors"
                      title="Revoke Token"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-sm text-ink-faint italic px-1">
                No API keys generated yet.
              </div>
            )}

            <form onSubmit={handleCreateApiKey} className="pt-2">
              <label className="block text-xs font-medium text-ink-faint mb-2">Create New Token</label>
              <div className="flex space-x-2">
                <input 
                  type="text" 
                  value={newKeyName}
                  onChange={(e) => setNewKeyName(e.target.value)}
                  placeholder="What's this token for? (e.g. My AI Agent)"
                  className="flex-1 bg-page border border-line rounded-lg px-3 py-2 text-sm text-ink focus:outline-none focus:border-ink-soft transition-colors"
                />
                <button 
                  type="submit"
                  disabled={isGeneratingKey || !newKeyName.trim()}
                  className="px-4 py-2 text-sm font-medium bg-ink text-page rounded-lg hover:bg-ink-soft transition-colors disabled:opacity-50 disabled:cursor-not-allowed whitespace-nowrap"
                >
                  {isGeneratingKey ? "Generating..." : "Generate"}
                </button>
              </div>
            </form>
            
          </div>
        </section>

        <section className="px-1 pt-6">
          <SignOutButton />
        </section>
      </div>

      <SettlementModal 
        isOpen={isSettlementModalOpen}
        onClose={() => setIsSettlementModalOpen(false)}
        accumulatedDebt={accumulatedPenalty}
        onSuccess={() => {
          setAccumulatedPenalty(0);
          setIsSettlementModalOpen(false);
        }}
      />
    </div>
  );
}
