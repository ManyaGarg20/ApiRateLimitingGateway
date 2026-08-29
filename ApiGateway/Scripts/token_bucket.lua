-- KEYS[1] = the bucket key, e.g. "ratelimit:user123:/api/products"
-- ARGV[1] = capacity
-- ARGV[2] = refill rate per second
-- ARGV[3] = current timestamp (unix ms)
-- ARGV[4] = TTL in seconds for the key

local key = KEYS[1]
local capacity = tonumber(ARGV[1])
local refillRate = tonumber(ARGV[2])
local now = tonumber(ARGV[3])
local ttl = tonumber(ARGV[4])

local bucket = redis.call("HMGET", key, "tokens", "lastRefillTimestamp")
local tokens = tonumber(bucket[1])
local lastRefill = tonumber(bucket[2])

-- If this is a brand-new key, start full
if tokens == nil then
    tokens = capacity
    lastRefill = now
end

-- Lazy refill, same math as Phase 5's in-memory version
local elapsedSeconds = (now - lastRefill) / 1000
local tokensToAdd = elapsedSeconds * refillRate
tokens = math.min(capacity, tokens + tokensToAdd)

local allowed = 0
local retryAfter = 0

if tokens >= 1 then
    tokens = tokens - 1
    allowed = 1
else
    retryAfter = (1 - tokens) / refillRate
end

redis.call("HSET", key, "tokens", tostring(tokens), "lastRefillTimestamp", tostring(now))
redis.call("EXPIRE", key, ttl)

return { allowed, tostring(retryAfter) }