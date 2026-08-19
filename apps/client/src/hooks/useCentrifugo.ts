"use client";

import { useEffect, useRef, useCallback, useState } from "react";
import { Centrifuge, Subscription, PublicationContext } from "centrifuge";
import { authClient } from "@/lib/auth-client";

export interface CentrifugoEvent {
  type: string;
  inviteId?: string;
  groupId?: string;
  userId?: string;
  userName?: string;
  inviterId?: string;
  inviterName?: string;
  inviterImage?: string;
}

type EventHandler = (event: CentrifugoEvent) => void;

// Hook for connecting to Centrifugo and subscribing to user events
// See more on Centrifugo: https://centrifugal.dev
export function useCentrifugo(onEvent?: EventHandler) {
  const centrifugeRef = useRef<Centrifuge | null>(null);
  const subRef = useRef<Subscription | null>(null);
  const onEventRef = useRef(onEvent);
  const [isConnected, setIsConnected] = useState(false);

  // Keep the callback ref fresh without causing reconnections
  onEventRef.current = onEvent;

  const connect = useCallback(async () => {
    if (centrifugeRef.current) return;

    const centrifugoUrl = process.env.NEXT_PUBLIC_CENTRIFUGO_URL || "ws://localhost:8000/connection/websocket";

    const centrifuge = new Centrifuge(centrifugoUrl, {
      getToken: async () => {
        const { data, error } = await authClient.token();
        if (error || !data?.token) {
          console.error("Failed to get JWT for Centrifugo", error);
          return "";
        }
        return data.token;
      },
    });

    centrifuge.on("connected", () => {
      setIsConnected(true);
    });

    centrifuge.on("disconnected", () => {
      setIsConnected(false);
    });

    // Subscribe to the user's personal channel
    // Use the "user" namespace configured in Centrifugo. The channel format is "user:<userId>"
    // where userId comes from the JWT sub claim
    const { data: session } = await authClient.getSession();
    if (!session?.user?.id) {
      console.warn("No user session, skipping Centrifugo connection");
      return;
    }

    const channel = `user:${session.user.id}`;
    const sub = centrifuge.newSubscription(channel);

    sub.on("publication", (ctx: PublicationContext) => {
      const event = ctx.data as CentrifugoEvent;
      onEventRef.current?.(event);
    });

    sub.subscribe();
    centrifuge.connect();

    centrifugeRef.current = centrifuge;
    subRef.current = sub;
  }, []);

  useEffect(() => {
    connect();

    return () => {
      subRef.current?.unsubscribe();
      centrifugeRef.current?.disconnect();
      centrifugeRef.current = null;
      subRef.current = null;
    };
  }, [connect]);

  return { isConnected };
}
