import * as signalR from "@microsoft/signalr";
import { api } from "./api";
import type { AnomalyLive } from "./types";

export type HubStatus = "connecting" | "connected" | "reconnecting" | "disconnected";

export function connectAnomalyHub(handlers: {
  onStatus: (status: HubStatus) => void;
  onAnomaly: (payload: AnomalyLive) => void;
  onResync: () => void;
}): signalR.HubConnection {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${api.url}/anomalyHub`, { withCredentials: true })
    .withAutomaticReconnect([0, 1000, 3000, 8000, 15000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  connection.on("anomalyDetected", (payload: AnomalyLive) => {
    handlers.onAnomaly(payload);
  });

  connection.onreconnecting(() => handlers.onStatus("reconnecting"));
  connection.onreconnected(async () => {
    handlers.onStatus("connected");
    try {
      await connection.invoke("Subscribe");
    } catch {
      // subscribe can fail if the server bounced; resync still helps
    }
    handlers.onResync();
  });
  connection.onclose(() => handlers.onStatus("disconnected"));

  void (async () => {
    handlers.onStatus("connecting");
    try {
      await connection.start();
      await connection.invoke("Subscribe");
      handlers.onStatus("connected");
    } catch {
      handlers.onStatus("disconnected");
    }
  })();

  return connection;
}
