# Step 03 - Backend Migration to Cosmos (Beginner Guide)

Goal: switch backend persistence from PostgreSQL/EF Identity to Cosmos-backed storage while keeping API behavior stable.

## Important scope note

This is a code migration step. It is the most complex part.

## What changes

1. Remove SQL database dependency in runtime.
2. Add Cosmos client and repositories.
3. Replace Identity persistence with Cosmos-backed user auth.
4. Keep JWT authentication and authorization roles.

## Migration checklist

1. Data access layer:
   - Add Cosmos configuration model.
   - Register Cosmos client in dependency injection.
   - Create repository implementations for users, retrospectives, items, assignments.
2. Auth layer:
   - Create user document model including roles and password hash.
   - On register, hash password and store user document.
   - On login, verify password hash and issue JWT.
3. Startup wiring:
   - Remove `UseNpgsql` and relational DbContext registration for runtime path.
   - Remove automatic EF migrations on startup.
4. Controllers/services:
   - Keep routes and DTO contracts unchanged when possible.

## Suggested safe migration order

1. Introduce Cosmos client and config without removing old code.
2. Migrate one repository at a time.
3. Migrate auth/register/login after data repositories are stable.
4. Remove unused EF runtime pieces at the end.

## Zero-knowledge validation strategy

1. Run backend locally after each small migration.
2. Test endpoints in Swagger/Postman.
3. Test login/register flows after auth migration.
4. Test board operations (create, update, delete, move items).

## Common mistakes

1. Missing partition key values when writing documents.
2. Breaking DTO contracts expected by frontend.
3. Removing old DI registrations before new services are ready.
4. Forgetting to keep role checks aligned with JWT claims.

## Done when

- Backend starts without PostgreSQL.
- All existing API routes used by frontend still work.
- Auth and role authorization work with Cosmos-backed users.
