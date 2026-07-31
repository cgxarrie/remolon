# Step 02 - Create Azure Resources (Beginner Guide)

Goal: create all required Azure services in one clean resource group.

## What you will create

1. Resource Group.
2. Cosmos DB account (NoSQL, Free Tier).
3. Cosmos database and containers.
4. App Service Plan (Free F1).
5. Backend Web App.
6. Static Web App for frontend.

## Part A - Create Resource Group

1. Go to https://portal.azure.com.
2. Search for Resource groups.
3. Click Create.
4. Subscription: choose your active subscription.
5. Resource group name: `rg-retromolon-free`.
6. Region: choose one close to you.
7. Click Review + create, then Create.

## Part B - Create Cosmos DB Free Tier

1. Search for Azure Cosmos DB.
2. Click Create.
3. Select API: NoSQL.
4. Choose existing resource group: `rg-retromolon-free`.
5. Account name: unique name (example `retromolon-cosmos-1234`).
6. Region: same as resource group.
7. Enable Free Tier: Yes.
8. Click Review + create, then Create.

## Part C - Create Cosmos database and containers

1. Open your Cosmos account.
2. Go to Data Explorer.
3. Click New Database.
4. Database id: `retromolon`.
5. Shared throughput: keep default free-friendly option if shown.
6. Create containers (one by one):
   - `users`
   - `retrospectives`
   - `items`
   - `assignments`
7. For each container, set a partition key (temporary suggestion): `/id`.

Note: Better partition strategy can be optimized later. `/id` is simple for first deployment.

## Part D - Create App Service Plan (Free)

1. Search for App Service plans.
2. Click Create.
3. Resource group: `rg-retromolon-free`.
4. Name: `asp-retromolon-free`.
5. Operating system: choose the one available with Free tier in your region.
6. Pricing tier: Free F1.
7. Create.

## Part E - Create backend Web App

1. Search for App Services.
2. Click Create -> Web App.
3. Resource group: `rg-retromolon-free`.
4. Name: unique app name (example `retromolon-api-1234`).
5. Publish: Code or Container based on your deployment method.
6. Runtime stack if Code: .NET (latest supported).
7. App Service plan: `asp-retromolon-free`.
8. Create.

## Part F - Create Static Web App (frontend)

1. Search for Static Web Apps.
2. Click Create.
3. Resource group: `rg-retromolon-free`.
4. Name: `retromolon-frontend`.
5. Plan type: Free.
6. Deployment source: GitHub.
7. Select repo and branch.
8. Build preset: React.
9. App location: `RetroFrontend`.
10. Output location: `dist`.
11. Create.

## Verification checklist

1. All resources are visible in `rg-retromolon-free`.
2. Cosmos account shows Free Tier enabled.
3. Web App is created on Free F1 plan.
4. Static Web App deployment starts from GitHub.

## Done when

- Azure infrastructure exists and is ready for app configuration.
