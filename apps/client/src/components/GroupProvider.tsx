"use client";

import { createContext, useContext, useEffect, useState, useCallback } from "react";
import { GroupSummaryResponse } from "@/lib/api";
import { useApi } from "@/hooks/useApi";

interface GroupContextType {
  groups: GroupSummaryResponse[];
  activeGroup: GroupSummaryResponse | null;
  setActiveGroupId: (id: string) => void;
  isLoading: boolean;
  refreshGroups: () => Promise<void>;
}

const GroupContext = createContext<GroupContextType>({} as any);

export function GroupProvider({ children }: { children: React.ReactNode }) {
  const api = useApi();
  const [groups, setGroups] = useState<GroupSummaryResponse[]>([]);
  const [activeGroupId, setActiveGroupId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // We only fetch if we are in the app, but actually the layout wraps the app, 
  // so it's safe to fetch. We can skip fetching if we are on /login or something,
  // but app layout already redirects if no session.

  const refreshGroups = useCallback(async () => {
    try {
      const myGroups = await api.getMyGroups();
      setGroups(myGroups);
      if (myGroups.length > 0) {
        setGroups(myGroups);
        // If no active group or the active group is no longer in the list, default to first
        if (!activeGroupId || !myGroups.find(g => g.id === activeGroupId)) {
          setActiveGroupId(myGroups[0].id!);
        }
      } else {
        setActiveGroupId(null);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  }, [api, activeGroupId]);

  useEffect(() => {
    refreshGroups();
  }, [refreshGroups]);

  const activeGroup = groups.find(g => g.id === activeGroupId) || null;

  return (
    <GroupContext.Provider value={{ groups, activeGroup, setActiveGroupId, isLoading, refreshGroups }}>
      {children}
    </GroupContext.Provider>
  );
}

export const useGroups = () => useContext(GroupContext);
