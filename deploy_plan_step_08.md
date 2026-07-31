# Step 08 - Keep the Setup Free Over Time (Beginner Guide)

Goal: stay inside free-tier limits and avoid accidental costs.

## Ongoing weekly routine

1. Open Cost Management every week.
2. Check current month spend trend.
3. Confirm budget alerts still active.
4. Review App Service and Cosmos usage metrics.

## Free-first guardrails

1. Keep only one environment (avoid extra staging resources).
2. Do not upgrade plans unless intentionally needed.
3. Delete unused deployment slots/resources.
4. Keep stored data small.
5. Avoid high-frequency background jobs.

## Cost risk triggers to watch

1. Increased request volume.
2. Large data growth in Cosmos.
3. Creating paid SKU by mistake.
4. Extra resources left running.

## Emergency stop plan

1. If unexpected cost appears:
   - Stop app activity.
   - Delete non-essential resources in the resource group.
2. If needed, delete whole resource group:
   - `rg-retromolon-free`

## Verification checklist

1. Budget alerts are reaching your email.
2. Monthly cost remains within your target.
3. No paid SKUs appear in the resource group.

## Done when

- You have a repeatable process to remain in free-tier boundaries.
