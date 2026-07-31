# Step 06 - Configure CORS Correctly (Beginner Guide)

Goal: allow browser requests only from trusted frontend origins.

## What CORS is

- CORS controls which browser origins can call your backend API.
- If origin is not allowed, browser blocks the response.

## What to allow

1. Production frontend origin (Static Web App URL).
2. Local development origin (`http://localhost:5173`).

## Configuration method

In backend App Service environment variables set:

1. `Cors__AllowedOrigins__0` = `https://<your-frontend>.azurestaticapps.net`
2. `Cors__AllowedOrigins__1` = `http://localhost:5173`

Then restart backend app.

## Security rules

1. Do not use wildcard origin (`*`) for authenticated APIs.
2. Use exact origin including protocol (`https://`).
3. Do not add trailing slash in origin value.

## Test CORS quickly

1. Open frontend app in browser.
2. Perform login request.
3. If blocked, check DevTools Network error details.
4. Verify the frontend origin string exactly matches one CORS value.

## Common mistakes

1. Using app URL with trailing slash.
2. Using wrong domain (preview URL vs production URL confusion).
3. Forgetting backend restart after config changes.

## Done when

- Browser requests from production frontend and localhost dev both succeed.
