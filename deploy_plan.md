# remolon Free-First Azure Deployment Plan (Option 2)

This plan is designed for **Azure-only** with a **free-first mindset**.

## Detailed Step Files

- Step 00: [Cost Safety Nets](deploy_plan_step_00.md)
- Step 01: [Target Architecture](deploy_plan_step_01.md)
- Step 02: [Create Azure Resources](deploy_plan_step_02.md)
- Step 03: [Backend Migration to Cosmos](deploy_plan_step_03.md)
- Step 04: [Backend App Settings](deploy_plan_step_04.md)
- Step 05: [Frontend Production API Configuration](deploy_plan_step_05.md)
- Step 06: [CORS Configuration](deploy_plan_step_06.md)
- Step 07: [End-to-End Validation](deploy_plan_step_07.md)
- Step 08: [Keep It Free Over Time](deploy_plan_step_08.md)

## Important Reality

- You can run at 0 USD only if you stay inside free-tier limits.
- If usage grows, Azure can charge.
- This plan includes cost guardrails to reduce surprise charges.

## Step 0: Cost Safety Nets First

Detailed guide: [deploy_plan_step_00.md](deploy_plan_step_00.md)

1. In Azure Portal, create a **Budget** for your subscription.
2. Set monthly budget to a very small value (for example 1 USD).
3. Add alerts at 50%, 80%, and 100%.
4. In Cost Management, enable anomaly alerts.
5. Keep all resources in one resource group so you can delete everything quickly if needed.

## Step 1: Target Architecture (Free-First)

Detailed guide: [deploy_plan_step_01.md](deploy_plan_step_01.md)

1. Frontend: **Azure Static Web Apps** (Free plan).
2. Backend: **Azure App Service** (Free plan, F1).
3. Database: **Azure Cosmos DB for NoSQL** (Free Tier).
4. Auth: move from ASP.NET Identity + EF/PostgreSQL to custom JWT auth persisted in Cosmos DB.
5. CORS: backend should allow only Static Web App domain and localhost dev origin.

Why this is needed:

- Current backend depends on EF relational model and PostgreSQL.
- PostgreSQL is not forever-free on Azure.
- Cosmos DB Free Tier is the Azure database option for long-term free usage with limits.

## Step 2: Create Azure Resources

Detailed guide: [deploy_plan_step_02.md](deploy_plan_step_02.md)

1. Create one resource group (example: `rg-remolon-free`).
2. Create Cosmos DB account:
   - API: NoSQL
   - Enable Free Tier
   - Choose the same region as backend
3. In Cosmos DB, create:
   - Database: `remolon`
   - Containers: `users`, `retrospectives`, `items`, `assignments`
4. Create App Service Plan:
   - SKU: Free F1
   - OS: Windows if Linux free is unavailable in your region
5. Create Web App on that plan for backend API.
6. Create Static Web App (Free), connected to your GitHub repo for frontend.

## Step 3: Backend Code Migration (Required)

Detailed guide: [deploy_plan_step_03.md](deploy_plan_step_03.md)

You must replace relational persistence and Identity storage.

1. Remove PostgreSQL dependency in runtime:
   - Remove EF relational DbContext usage for app data
   - Remove automatic SQL migrations at startup
2. Add Cosmos SDK and data services:
   - Configure Cosmos client from environment variables
   - Implement repositories for users, retrospectives, items, assignments
3. Replace ASP.NET Identity login/register with custom auth:
   - Store users in `users` container
   - Hash passwords with `PasswordHasher`
   - Keep JWT issuance flow
4. Keep roles on user documents so authorization policies continue to work.
5. Keep controller routes stable to minimize frontend changes.

## Step 4: Backend App Settings in Azure

Detailed guide: [deploy_plan_step_04.md](deploy_plan_step_04.md)

Set these environment variables in the backend Web App:

1. `CosmosDb__AccountEndpoint` = your cosmos endpoint
2. `CosmosDb__AccountKey` = your cosmos key
3. `CosmosDb__DatabaseName` = `remolon`
4. `CosmosDb__UsersContainer` = `users`
5. `CosmosDb__RetrospectivesContainer` = `retrospectives`
6. `CosmosDb__ItemsContainer` = `items`
7. `CosmosDb__AssignmentsContainer` = `assignments`
8. `Jwt__Key` = long random secret (at least 32 chars)
9. `Jwt__Issuer` = `RetroBackend`
10. `Jwt__Audience` = `RetroBackendClients`
11. `Cors__AllowedOrigins__0` = your Static Web App URL
12. `Cors__AllowedOrigins__1` = `http://localhost:5173`

Then restart the backend app.

## Step 5: Frontend Production API Configuration

Detailed guide: [deploy_plan_step_05.md](deploy_plan_step_05.md)

1. Ensure frontend API client reads base URL from env variable.
2. In Static Web App app settings, set:
   - `VITE_API_URL` = backend public URL with `/api`
3. Redeploy frontend.

## Step 6: CORS Configuration

Detailed guide: [deploy_plan_step_06.md](deploy_plan_step_06.md)

1. Allow only:
   - Production Static Web App URL
   - Localhost development URL
2. Do not use wildcard origin in production.

## Step 7: End-to-End Test

Detailed guide: [deploy_plan_step_07.md](deploy_plan_step_07.md)

1. Open frontend URL.
2. Register first user.
3. Login and create retrospective.
4. Add and move items.
5. If something fails:
   - Check browser Network tab for CORS/401 errors
   - Check backend log stream in Azure
   - Verify Cosmos endpoint/key and container names

## Step 8: Keep It Free Over Time

Detailed guide: [deploy_plan_step_08.md](deploy_plan_step_08.md)

1. Keep Cosmos data and throughput usage within free-tier limits.
2. Keep request volume low.
3. Avoid creating non-free SKUs.
4. Review cost alerts weekly.
5. Delete unused resources and old deployments.

## Optional Next Implementation Scope

If you want code changes next, implement in this order:

1. Add Cosmos configuration models and DI wiring.
2. Implement Cosmos repositories replacing EF repositories.
3. Replace Identity persistence with Cosmos-backed user auth.
4. Keep existing API routes and CORS behavior.
