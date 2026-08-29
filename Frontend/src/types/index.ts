export interface RateLimitConfig {
  id: number;
  endpoint: string;
  capacity: number;
  refillRatePerSecond: number;
  isActive: boolean;
}

export interface RequestStats {
  total: number;
  allowed: number;
  rejected: number;
}

export type CreateRateLimitConfig = Omit<RateLimitConfig, "id">;
