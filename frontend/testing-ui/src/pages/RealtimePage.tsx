import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { useRef, useState } from 'react';
import { ResultPanel } from '../components/ResultPanel';
import { Section } from '../components/Section';
import { apiUrl } from '../config';
import type { LowStockAlert, StockChangedAlert } from '../models';

type EventLog = {
  id: number;
  type: string;
  payload: StockChangedAlert | LowStockAlert;
};

export function RealtimePage({ accessToken }: { accessToken: string }) {
  const connectionRef = useRef<HubConnection | null>(null);
  const [status, setStatus] = useState('Disconnected');
  const [events, setEvents] = useState<EventLog[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function connect() {
    if (connectionRef.current?.state === HubConnectionState.Connected) return;

    const connection = new HubConnectionBuilder()
        .withUrl(apiUrl('/hubs/low-stock'), {
            accessTokenFactory: () => accessToken,
            withCredentials: false
        }).withAutomaticReconnect()
      .build();

    connection.on('StockChanged', (payload: StockChangedAlert) => {
      setEvents((current) => [{ id: Date.now(), type: 'StockChanged', payload }, ...current]);
    });

    connection.on('LowStockReached', (payload: LowStockAlert) => {
      setEvents((current) => [{ id: Date.now(), type: 'LowStockReached', payload }, ...current]);
    });

    connection.onreconnecting(() => setStatus('Reconnecting'));
    connection.onreconnected(() => setStatus('Connected'));
    connection.onclose(() => setStatus('Disconnected'));

    try {
      await connection.start();
      connectionRef.current = connection;
      setStatus('Connected');
      setError(null);
    } catch (connectionError) {
      setError(connectionError instanceof Error ? connectionError.message : 'SignalR connection failed.');
      setStatus('Disconnected');
    }
  }

  async function disconnect() {
    await connectionRef.current?.stop();
    connectionRef.current = null;
    setStatus('Disconnected');
  }

  return (
    <Section title="Realtime Monitor" description="Listen to SignalR stock-change and low-stock events.">
      <div className="panel actions">
        <span className={`status ${status.toLowerCase()}`}>{status}</span>
        <button onClick={connect}>Connect</button>
        <button onClick={disconnect}>Disconnect</button>
        <button onClick={() => setEvents([])}>Clear events</button>
      </div>
      {error && <ResultPanel result={{ ok: false, status: 0, error }} />}
      <div className="panel">
        <h3>Events</h3>
        {events.length === 0 ? (
          <p>No realtime events received yet. Create a receipt, sale, or transfer while connected.</p>
        ) : (
          <div className="event-list">
            {events.map((event) => (
              <article className="event-item" key={event.id}>
                <strong>{event.type}</strong>
                <pre>{JSON.stringify(event.payload, null, 2)}</pre>
              </article>
            ))}
          </div>
        )}
      </div>
    </Section>
  );
}
