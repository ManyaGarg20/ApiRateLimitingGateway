import type { RateLimitConfig, CreateRateLimitConfig, RequestStats } from "../types";

const GATEWAY_BASE_URL = "http://localhost:5225";

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let message = `Request failed: ${response.status} ${response.statusText}`;
    try {
      const body = await response.json();
      if (body?.error) {
        message = body.error;
      }
    } catch {
      // response wasn't JSON, keep the generic message
    }
    throw new Error(message);
  }
  return response.json() as Promise<T>;
}

export const gatewayClient = {
  async getStats(): Promise<RequestStats> {
    const res = await fetch(`${GATEWAY_BASE_URL}/api/stats`);
    return handleResponse<RequestStats>(res);
  },

  async getConfigs(): Promise<RateLimitConfig[]> {
    const res = await fetch(`${GATEWAY_BASE_URL}/api/config/ratelimits`);
    return handleResponse<RateLimitConfig[]>(res);
  },

  async createConfig(config: CreateRateLimitConfig): Promise<RateLimitConfig> {
    const res = await fetch(`${GATEWAY_BASE_URL}/api/config/ratelimits`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(config),
    });
    return handleResponse<RateLimitConfig>(res);
  },

  async updateConfig(id: number, config: CreateRateLimitConfig): Promise<RateLimitConfig> {
    const res = await fetch(`${GATEWAY_BASE_URL}/api/config/ratelimits/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(config),
    });
    return handleResponse<RateLimitConfig>(res);
  },

  async deleteConfig(id: number): Promise<void> {
    const res = await fetch(`${GATEWAY_BASE_URL}/api/config/ratelimits/${id}`, {
      method: "DELETE",
    });
    if (!res.ok) {
      throw new Error(`Delete failed: ${res.status}`);
    }
  },
};
