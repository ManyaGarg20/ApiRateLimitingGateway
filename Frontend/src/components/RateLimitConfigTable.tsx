import { useEffect, useState } from "react";
import { gatewayClient } from "../api/gatewayClient";
import type { RateLimitConfig, CreateRateLimitConfig } from "../types";

const emptyForm: CreateRateLimitConfig = {
  endpoint: "",
  capacity: 10,
  refillRatePerSecond: 1,
  isActive: true,
};

export function RateLimitConfigTable() {
  const [configs, setConfigs] = useState<RateLimitConfig[]>([]);
  const [form, setForm] = useState<CreateRateLimitConfig>(emptyForm);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadConfigs = () => {
    gatewayClient
      .getConfigs()
      .then(setConfigs)
      .catch((err) => setError(err.message));
  };

  useEffect(() => {
    loadConfigs();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingId !== null) {
        await gatewayClient.updateConfig(editingId, form);
      } else {
        await gatewayClient.createConfig(form);
      }
      setForm(emptyForm);
      setEditingId(null);
      loadConfigs();
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const handleEdit = (config: RateLimitConfig) => {
    setEditingId(config.id);
    setForm({
      endpoint: config.endpoint,
      capacity: config.capacity,
      refillRatePerSecond: config.refillRatePerSecond,
      isActive: config.isActive,
    });
  };

  const handleDelete = async (id: number) => {
    try {
      await gatewayClient.deleteConfig(id);
      loadConfigs();
    } catch (err) {
      setError((err as Error).message);
    }
  };

  return (
    <div>
      <h2>Rate Limit Configurations</h2>
      {error && (
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            background: "#fdecea",
            color: "#611a15",
            border: "1px solid #f5c6cb",
            borderRadius: "4px",
            padding: "0.5rem 0.75rem",
            marginBottom: "1rem",
          }}
        >
          <span>{error}</span>
          <button
            onClick={() => setError(null)}
            aria-label="Dismiss error"
            style={{
              background: "none",
              border: "none",
              fontSize: "1.1rem",
              cursor: "pointer",
              color: "#611a15",
              lineHeight: 1,
              marginLeft: "0.75rem",
            }}
          >
            ×
          </button>
        </div>
      )}

      <table border={1} cellPadding={6}>
        <thead>
          <tr>
            <th>Endpoint</th>
            <th>Capacity</th>
            <th>Refill Rate</th>
            <th>Active</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {configs.map((c) => (
            <tr key={c.id}>
              <td>{c.endpoint}</td>
              <td>{c.capacity}</td>
              <td>{c.refillRatePerSecond}</td>
              <td>{c.isActive ? "Yes" : "No"}</td>
              <td>
                <button onClick={() => handleEdit(c)}>Edit</button>
                <button onClick={() => handleDelete(c.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <h3>{editingId !== null ? "Edit Configuration" : "Create Configuration"}</h3>
      <form onSubmit={handleSubmit}>
        <div>
          <label>Endpoint: </label>
          <input
            value={form.endpoint}
            onChange={(e) => setForm({ ...form, endpoint: e.target.value })}
            placeholder="/api/products"
            required
          />
        </div>
        <div>
          <label>Capacity: </label>
          <input
            type="number"
            value={form.capacity}
            onChange={(e) => setForm({ ...form, capacity: Number(e.target.value) })}
            required
          />
        </div>
        <div>
          <label>Refill Rate (tokens/sec): </label>
          <input
            type="number"
            step="0.1"
            value={form.refillRatePerSecond}
            onChange={(e) => setForm({ ...form, refillRatePerSecond: Number(e.target.value) })}
            required
          />
        </div>
        <div>
          <label>
            Active:
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
            />
          </label>
        </div>
        <button type="submit">{editingId !== null ? "Update" : "Create"}</button>
        {editingId !== null && (
          <button type="button" onClick={() => { setEditingId(null); setForm(emptyForm); }}>
            Cancel
          </button>
        )}
      </form>
    </div>
  );
}
