# Step 05 - Configure Frontend API URL for Production (Beginner Guide)

Goal: make the frontend call your deployed backend URL in production.

## Why this matters

- Local development often uses proxy or localhost.
- Production frontend needs explicit backend URL.

## Part A - Check frontend client code

1. Open frontend API client file.
2. Ensure base URL uses environment variable with fallback.

Expected pattern:

```ts
baseURL: import.meta.env.VITE_API_URL || '/api'
```

## Part B - Set Static Web App environment variable

1. Open Static Web App resource in Azure Portal.
2. Go to Environment variables / Configuration.
3. Add key:
   - `VITE_API_URL`
4. Set value:
   - `https://<your-backend-app>.azurewebsites.net/api`
5. Save.

## Part C - Redeploy frontend

1. Trigger redeploy from GitHub action (push commit or rerun workflow).
2. Wait for deployment to finish.
3. Open frontend URL and test login.

## Verification checklist

1. Browser Network tab requests go to your backend domain.
2. Login and API calls succeed.

## Common mistakes

1. Missing `/api` in backend URL.
2. Forgetting redeploy after changing env variable.
3. Leaving frontend pointed to localhost.

## Done when

- Frontend in Azure calls backend in Azure successfully.
