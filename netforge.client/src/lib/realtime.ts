import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';

// One shared hub connection for the whole app — keyed off a module singleton so React StrictMode's
// double-mount and HMR don't open duplicate sockets. Cookie auth rides along automatically
// (the SignalR client sends credentials on negotiate + the WebSocket upgrade).
let connection: HubConnection | null = null;

export function notificationsHub(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
  }
  return connection;
}

/** Idempotent start — safe to call repeatedly; resolves once connected (or already connecting). */
export async function ensureStarted(conn: HubConnection): Promise<void> {
  if (conn.state === HubConnectionState.Disconnected) {
    await conn.start();
  }
}
