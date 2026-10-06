# SWA → linked APIM → App Service POC

A small app that tests the proposed AITO architecture before you change the real app:

```
React (Static Web Apps)
  ↓  SWA authentication (Entra ID, session cookie)
SWA /api/*
  ↓  linked APIM (subscription key + validate-jwt, added by linking)
APIM API "swa-apim-poc" (adds X-Gateway-Secret)
  ↓
.NET 9 API on App Service (reads x-ms-client-principal)
  ↓  forwarded Graph token (X-Graph-Token), only where needed
Microsoft Graph
```

No `access_as_user` scope is used anywhere.

## What the POC answers

| Team question | How the POC answers it |
|---|---|
| What token does SWA send to linked APIM? | **Inspect request** shows the decoded claims of the `Authorization` token that reached the API (`iss`, `aud`, `appid`, `sub`). |
| Does the backend get the user's Entra `oid`? | **Inspect request** shows the raw `x-ms-client-principal`. Microsoft documents that the `claims` array isn't sent to APIs, so expect `clientPrincipalHasClaims: false`. |
| Can the API still get a trusted `oid`? | **Verify Entra identity** gets a Graph token in the browser. The API asks Graph who the token belongs to, checks it matches the SWA user and links the `oid` to SWA's `userId`. |
| Can `OwnerObjectId` filtering keep working? | Yes, if the verified `oid` from the step above is the key. The dashboard shows `verifiedIdentity.entraObjectId`. |
| What replaces Graph OBO? | **Call Graph via backend** makes the API call Graph `/me` with the forwarded token. It's the same pattern the mail and calendar services would use. |
| Can someone bypass SWA? | The direct-call tests below should all fail. |

## Project layout

```
backend/                       .NET 9, clean architecture
  src/SwaApimPoc.Domain          VerifiedIdentity
  src/SwaApimPoc.Application     use cases + interfaces (ICurrentUser, IGraphTokenAccessor, ...)
  src/SwaApimPoc.Infrastructure  Graph client, in-memory identity store
  src/SwaApimPoc.Api             SWA header auth handler, gateway secret check, controllers
frontend/                      React 19 + Vite
  src/pages/LoginPage.tsx        "Sign in with Microsoft" (SWA auth)
  src/pages/DashboardPage.tsx    shown after sign-in, runs the tests
  src/auth/graphToken.ts         MSAL, used only to get Graph tokens
  public/staticwebapp.config.json
  local/staticwebapp.config.json emulator-only config (mock sign-in)
apim/api-policy.xml            adds X-Gateway-Secret
infra/deploy.ps1               Azure CLI script for all the steps below
```

## API endpoints

| Endpoint | Auth | Purpose |
|---|---|---|
| `GET /api/health` | Anonymous | Liveness check |
| `GET /api/dashboard` | Signed in | Dashboard data and verified identity |
| `GET /api/diagnostics/request` | Signed in | What reached the API (POC only; never returns token values) |
| `POST /api/identity/verify` | Signed in + `X-Graph-Token` | Gets the `oid` from Graph and links it to the SWA user |
| `GET /api/graph/me` | Signed in + `X-Graph-Token` | Backend calls Graph with the forwarded token |

## 1. Run locally (no Azure needed)

The SWA CLI emulates sign-in and adds `x-ms-client-principal`, just like SWA does in Azure.

```bash
cd backend
dotnet run --project src/SwaApimPoc.Api
```

```bash
cd frontend
npm install
cp .env.example .env.development
npm run build
npx swa start dist --api-devserver-url http://localhost:5080 --port 4280 --swa-config-location local
```

Open http://localhost:4280. Sign in on the emulator's mock screen with any username. The dashboard and **Inspect request** work locally. The two Graph buttons need a real Entra sign-in, so test those in Azure.

The local config has no custom Entra provider. The emulator would otherwise try a real sign-in.

## 2. App registration (existing one)

In **Microsoft Entra ID → App registrations → your app**:

| Setting | Value |
|---|---|
| Authentication → **Web** platform → Redirect URI | `https://<swa-host>/.auth/login/aad/callback` |
| Authentication → **Single-page application** platform → Redirect URI | `https://<swa-host>/redirect.html` |
| Authentication → Implicit grant and hybrid flows | Check **ID tokens** |
| Certificates & secrets | New client secret for the POC (used by SWA) |
| API permissions | `User.Read` (Microsoft Graph, delegated). Nothing else needed. |
| Expose an API | Nothing. No `access_as_user`. |

You'll know `<swa-host>` after creating the Static Web App (step 3.5). Remove these POC redirect URIs and the secret when you're done.

## 3. Create the Azure services

`infra/deploy.ps1` runs all of these steps. Read it first; each step also works in the portal.

```powershell
./infra/deploy.ps1 -Prefix aitopoc -TenantId <tenant-guid> -ClientId <app-client-id> -PublisherEmail you@company.com
```

To reuse your existing APIM instead of creating a Consumption one, add `-ExistingApimName <name> -ExistingApimResourceGroup <rg>`.

### Portal steps (what the script does)

1. **Resource group**, for example `rg-aitopoc`.
2. **App Service**: Linux, .NET 9, B1 plan.
   - Configuration → app setting `Gateway__Secret` = a long random value.
   - Settings → turn on **HTTPS only**.
3. **API Management**: Consumption tier is the quickest for a POC (a few minutes to create), or use your existing instance.
   - **Named values** → add `poc-gateway-secret` (type: Secret), same value as `Gateway__Secret`.
   - **APIs → Add API → HTTP**:
     - Display name `SWA APIM POC`
     - Web service URL `https://<app>.azurewebsites.net/api`
     - API URL suffix `api` (required: SWA forwards the full `/api/...` path)
   - Add two operations, `GET /*` and `POST /*`.
   - Open the API's **All operations → Inbound processing → </>** and paste `apim/api-policy.xml`.
4. **Static Web App**: plan **Standard**, deployment source **Other**.
   - **Environment variables**: `AZURE_CLIENT_ID` = app registration client ID, `AZURE_CLIENT_SECRET` = the POC secret.
5. Note the SWA URL, then add the redirect URIs from step 2.
6. **Link APIM**: Static Web App → **APIs** → Production → **Link** → backend type **API Management** → choose the instance → **Link**.
7. **Expose the API**: APIM → **Products** → `Azure Static Web Apps - <swa-host> (Linked)` → **+ Add API** → `SWA APIM POC`.
   - Don't edit that product's `validate-jwt` policy or its subscription. Linking created them, and changing them breaks the link.

## 4. Deploy

**Backend**

```powershell
dotnet publish backend/src/SwaApimPoc.Api -c Release -o backend/publish
Compress-Archive -Path backend/publish/* -DestinationPath backend/publish.zip -Force
az webapp deploy -g rg-aitopoc -n <app-name> --src-path backend/publish.zip --type zip
```

**Frontend**

1. In `frontend/public/staticwebapp.config.json`, replace `<TENANT_ID>` with your tenant ID.
2. Create `frontend/.env.production` with `VITE_AZURE_CLIENT_ID` and `VITE_AZURE_TENANT_ID`.
3. Build and deploy:

```powershell
cd frontend
npm ci
npm run build
$token = az staticwebapp secrets list -g rg-aitopoc -n <swa-name> --query properties.apiKey -o tsv
npx swa deploy ./dist --deployment-token $token --env production
```

## 5. Test plan

### Happy path

| # | Action | Expected |
|---|---|---|
| 1 | Open `https://<swa-host>/dashboard` signed out | Redirect to `/login` |
| 2 | Click **Sign in with Microsoft** | Entra sign-in, then the dashboard |
| 3 | Card 1 (browser view) | Your email, SWA `userId`; the `oid` likely shows in `claims` here (browser only) |
| 4 | Card 2 (backend view) | HTTP 200 with your email and the same SWA `userId` |
| 5 | **Inspect request** | `clientPrincipal` without `claims`; `authorizationHeader.claims` shows the SWA-issued token; `X-Gateway-Secret` in header names |
| 6 | **Verify Entra identity** | HTTP 200; `entraObjectId` equals your Entra object ID (check in Entra → Users) |
| 7 | **Call Graph via backend** | HTTP 200 with your Graph profile |

Record the output of steps 5 and 6. They answer the team's questions about the token and the `oid`.

### Bypass attempts (all should fail)

```bash
# Direct to App Service, no gateway secret -> 403
curl -i https://<app>.azurewebsites.net/api/dashboard

# Direct to App Service with a forged identity header -> 403
curl -i -H "x-ms-client-principal: eyJ1c2VySWQiOiJmYWtlIn0=" https://<app>.azurewebsites.net/api/dashboard

# Direct to APIM without a subscription key -> 401
curl -i https://<apim>.azure-api.net/api/dashboard

# Direct to APIM with the subscription key but no SWA token -> 401 (validate-jwt)
curl -i -H "Ocp-Apim-Subscription-Key: <key from APIM > Subscriptions>" https://<apim>.azure-api.net/api/dashboard
```

### Graph token checks

| # | Action | Expected |
|---|---|---|
| 1 | Call `/api/graph/me` without `X-Graph-Token` (browser dev tools) | 400 |
| 2 | Send a token for a different user | 403 (identity mismatch) |
| 3 | Browser blocking third-party cookies | One popup on first Graph call, silent after that |

## 6. What carries over to AITO

- `SwaAuthenticationHandler` replaces `AddMicrosoftIdentityWebApi`. Keep the `AccessAsUser` policy name and drop `RequireScope`.
- `GatewaySecretMiddleware`, or private networking, so only APIM reaches the API.
- `IGraphTokenAccessor` replaces `ITokenAcquisition.GetAccessTokenForUserAsync` in the mail, calendar and coach scheduling services. Request `Mail.ReadWrite` / `Calendars.ReadWrite` instead of `User.Read`.
- The verified identity store moves to Azure SQL, so `OwnerObjectId` keeps using the Entra `oid`.

## 7. Clean up

```powershell
az group delete -n rg-aitopoc --yes
```

If you reused an existing APIM, also delete the `swa-apim-poc` API, the `poc-gateway-secret` named value, and the linked product and subscription. Unlinking doesn't delete those. Remove the POC redirect URIs and client secret from the app registration.

## Notes

- Built and tested locally: the backend compiles, auth paths behave as expected with curl, and the SWA emulator flow works. The Azure script hasn't been run against a subscription, so check each step's output.
- The identity store is in memory and resets when the App Service restarts. Click **Verify Entra identity** again after a restart.
- `/api/diagnostics/request` is for the POC only. Remove it before any real use.
