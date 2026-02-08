# Subscription API

## What the Project Is
A .NET 8 Web API that manages API usage for users with subscription plans (Free and Pro). It enforces monthly request quotas and per-minute rate limiting, allows plan upgrades/downgrades, and automatically handles subscription expiration with reminders and downgrades.

## What It Does
- Tracks monthly API usage per user based on their plan (Free: 1000 requests/month, Pro: 10000 requests/month)
- Enforces 100 requests per minute rate limiting per user
- Allows users to upgrade to Pro or downgrade to Free, resetting usage immediately
- Runs a daily job at 2 AM UTC to send "email reminders" (console logs) for expiring Pro subscriptions and auto-downgrade expired ones to Free

## What Problem It Solves
It solves the problem of controlling and monetizing API access by:
- Preventing abuse through rate limiting
- Limiting total usage per billing cycle with quotas
- Automating subscription lifecycle (reminders and downgrades)
- Providing a simple way to switch plans without payment processing

## How It Solves It
- Rate limiting: Fixed-window counter in Redis (atomic INCR + EXPIRE)
- Monthly quota: Stored and enforced in PostgreSQL with atomic increments
- Plan changes: Immediate database update with quota reset and end-date management
- Daily tasks: Hangfire recurring job that checks expiration dates and performs actions

## Technologies Used
- .NET 8 ASP.NET Core Web API
- PostgreSQL (via Entity Framework Core + Npgsql) – main database and Hangfire storage
- Redis (StackExchange.Redis) – rate limiting only
- Hangfire – daily recurring job at 2 AM UTC

## Endpoints

### 1. GET `/api/data/user/{userId}`
Fetches API data for the user if limits allow.

**Behavior:**
- Checks rate limit (Redis) → 429 "Too Many Requests" if exceeded
- Checks monthly quota → 429 "Monthly Quota Exceeded" if used up
- On success: Increments usage and returns JSON response
- If user not found: 404

**Success Response (200):**
```json
{ "data": "Some API response data" }
```

### 2. POST `/api/upgrade/user/{userId}`
Upgrades or downgrades the user's plan.

**Request Body:**
```json
{ "plan": "Pro" }   // or "Free"
```

**Behavior:**
- Creates subscription if none exists
- Sets plan, quota, resets usage to 0
- Pro: Sets SubscriptionEndDate to +1 month
- Free: Sets SubscriptionEndDate to null

**Success Response (200):**
```json
{
  "success": true,
  "message": "Upgraded to Pro plan",   // or "Changed to Free plan"
  "monthlyQuota": 10000,               // or 1000
  "usedThisMonth": 0
}
```