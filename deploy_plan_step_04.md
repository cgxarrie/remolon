# Step 04 - Configure Backend App Settings in Azure (Beginner Guide)

Goal: connect the deployed backend to Cosmos DB and set JWT/CORS variables.

## Where to configure

1. Go to Azure Portal.
2. Open your backend App Service.
3. Go to Settings -> Environment variables.

## Add required settings

Add each key/value exactly:

1. `CosmosDb__AccountEndpoint` = your Cosmos endpoint URL
2. `CosmosDb__AccountKey` = your Cosmos primary key
3. `CosmosDb__DatabaseName` = `retromolon`
4. `CosmosDb__UsersContainer` = `users`
5. `CosmosDb__RetrospectivesContainer` = `retrospectives`
6. `CosmosDb__ItemsContainer` = `items`
7. `CosmosDb__AssignmentsContainer` = `assignments`
8. `Jwt__Key` = long random secret (minimum 32 chars)
9. `Jwt__Issuer` = `RetroBackend`
10. `Jwt__Audience` = `RetroBackendClients`
11. `Cors__AllowedOrigins__0` = your Static Web App URL
12. `Cors__AllowedOrigins__1` = `http://localhost:5173`

## Save and restart

1. Click Save.
2. Confirm restart when prompted.
3. Wait for app restart completion.

## Verify app config applied

1. Open Log stream in App Service.
2. Confirm app starts successfully.
3. Call a simple endpoint (for example swagger or health endpoint if present).

## Common mistakes

1. Typo in variable keys (double underscore is required).
2. Wrong Cosmos key or endpoint.
3. Missing `https://` in allowed CORS origin.

## Done when

- Backend can start and connect to Cosmos using environment variables only.
