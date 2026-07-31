# Step 07 - End-to-End Validation (Beginner Guide)

Goal: verify full system works from UI to database.

## Test checklist (in order)

1. Open frontend production URL.
2. Register a new user.
3. Login with that user.
4. Create a retrospective.
5. Add items to columns.
6. Move/reorder items.
7. Update and delete an item.
8. Log out and log back in.

## What success looks like

1. No CORS errors in browser console.
2. API responses are 2xx for expected calls.
3. Data persists after page refresh.

## If something fails

1. Check browser DevTools -> Network:
   - Look for failed request status and URL.
2. Check backend App Service -> Log stream:
   - Look for startup/config/auth/database errors.
3. Check Cosmos Data Explorer:
   - Confirm documents are being created.

## Quick troubleshooting map

1. `401 Unauthorized`:
   - JWT config mismatch or invalid token handling.
2. `403` from browser/CORS message:
   - Origin not in CORS allowed list.
3. `500 Internal Server Error`:
   - Backend exception; inspect log stream.
4. Data not saved:
   - Cosmos connection/key/container or partition issue.

## Done when

- Core user journey works reliably in production.
