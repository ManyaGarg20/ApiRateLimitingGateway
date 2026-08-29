import { useEffect, useState } from "react";
import { gatewayClient } from "../api/gatewayClient";
import type { RequestStats } from "../types";

export function StatsPanel() {
  const [stats, setStats] = useState<RequestStats | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStats = () => {
      gatewayClient
        .getStats()
        .then(setStats)
        .catch((err) => setError(err.message));
    };

    fetchStats();
    const interval = setInterval(fetchStats, 3000); // poll every 3s
    return () => clearInterval(interval);
  }, []);

  if (error) return <p>Error loading stats: {error}</p>;
  if (!stats) return <p>Loading stats...</p>;

  return (
    <div>
      <h2>Request Stats</h2>
      <p>Total: {stats.total}</p>
      <p>Allowed: {stats.allowed}</p>
      <p>Rejected: {stats.rejected}</p>
    </div>
  );
}
