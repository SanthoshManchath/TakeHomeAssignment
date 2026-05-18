# WiFi Provisioning API

A REST API that activates a customer's WiFi service by orchestrating two downstream APIs:

1. **Network Infrastructure API** — returns the catalog of available speed profiles.
2. **Network Controller API** — performs the actual activation on network gear.

Built with C# / .NET 10 / ASP.NET Core. Stubbed with WireMock for local development.

---

## Quickstart

```bash
docker compose up --build
```

This starts:

- **WireMock** (stubbed downstream APIs) on `http://localhost:8080`
- **WiFi Provisioning API** on `http://localhost:5080`

Once up, run the manual test scenarios:

```bash
./tests/manual/run-scenarios.sh
```

You should see 8 scenarios pass.

---

## Project structure

```
.
├── src/
│   ├── WifiProvisioning.Core/        Business logic, no web/HTTP concerns
│   │   ├── Configuration/            Strongly-typed options for downstream APIs
│   │   ├── Exceptions/               Typed exception hierarchy
│   │   ├── Mapping/                  TM Forum → domain model mapping
│   │   ├── Models/
│   │   │   ├── Domain/               Clean internal models
│   │   │   └── Input/                TM Forum-shaped DTOs
│   │   └── Services/                 HTTP clients + orchestrator
│   └── WifiProvisioning.Api/         ASP.NET Core host
│       ├── Controllers/              ProvisioningController, HealthController
│       └── Middleware/               ExceptionHandlingMiddleware
├── tests/
│   ├── WifiProvisioning.Tests/       Unit + integration tests (xUnit)
│   └── manual/                       JSON fixtures + bash scenario runner
├── wiremock/
│   └── mappings/                     WireMock stubs for both downstream APIs
├── Dockerfile                        Multi-stage build, non-root runtime
├── docker-compose.yml                Brings up API + WireMock together
└── coverage.runsettings              Test coverage configuration
```

---

## Endpoints

### `POST /api/provisioning`

Provisions WiFi service for a customer.

**Request** (TM Forum-style order):

```json
{
  "externalId": "ACT-20251017-001",
  "description": "Activate WiFi service for VFZ customer",
  "orderItem": {
    "id": "1",
    "service": {
      "id": "",
      "serviceSpecification": {
        "id": "SPEC-WIFI-001",
        "name": "WiFi Service"
      },
      "serviceCharacteristic": [
        {
          "name": "customerId",
          "valueType": "string",
          "value": {
            "@type": "string",
            "customerId": "CUST-4589"
          }
        },
        {
          "name": "customerName",
          "valueType": "string",
          "value": {
            "@type": "string",
            "customerName": "Alice Johnson"
          }
        },
        {
          "name": "customerAddress",
          "valueType": "string",
          "value": {
            "@type": "string",
            "customerAddress": "Keizersgracht 123, 1015 CJ Amsterdam, Netherlands"
          }
        },
        {
          "name": "speedProfile",
          "valueType": "string",
          "value": {
            "@type": "string",
            "speedProfile": "SP-500"
          }
        }
      ]
    }
  }
}
```

**Success response** (`200 OK`):

```json
{
    "orderId": "ACT-20251017-001",
    "activationId": "ACT-a6297564-f813-45c8-8815-6a749f2fc3ee",
    "status": "ACTIVE",
    "profileId": "SP-500",
    "downloadSpeedMbps": 500,
    "uploadSpeedMbps": 100,
    "activatedAt": "2026-05-18T12:24:24+00:00"
}
```

### `GET /health`

Returns 200 with a `{"status":"healthy","timestamp":"..."}` payload. Used by Docker's `HEALTHCHECK`.

---

## Error handling

All errors return RFC 7807 ProblemDetails (`application/problem+json`):

| Scenario | HTTP status |
| --- | --- |
| Request body fails structural validation (missing required field) | `400 Bad Request` |
| Request body has structurally valid JSON but missing required characteristic (`speedProfile`, `customerId`, etc.) | `400 Bad Request` |
| Speed code not in the Network Infrastructure catalog | `404 Not Found` |
| Network Infrastructure API times out | `504 Gateway Timeout` |
| Network Infrastructure API returns 5xx or network error | `502 Bad Gateway` |
| Network Controller API times out | `504 Gateway Timeout` |
| Network Controller API returns 5xx or network error | `502 Bad Gateway` |
| Unexpected exception | `500 Internal Server Error` |

Every error response includes a `traceId` (the request's `HttpContext.TraceIdentifier`) for correlation with server logs.

---

## Running tests

### Unit + integration tests

```bash
dotnet test
```

~33 tests across the mapper, HTTP clients, orchestrator, validator, and end-to-end through `WebApplicationFactory`.

### Test coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory ./TestResults

reportgenerator \
  -reports:"./TestResults/**/coverage.cobertura.xml" \
  -targetdir:"./TestResults/CoverageReport" \
  -reporttypes:"Html;TextSummary"

cat ./TestResults/CoverageReport/Summary.txt
```

Current coverage:

- **Line coverage: 97.7%**
- **Branch coverage: 92.1%**

The HTML report at `./TestResults/CoverageReport/index.html` shows per-class coverage.

### Manual scenario tests

With the stack running (`docker compose up`):

```bash
./tests/manual/run-scenarios.sh
```

Hits the running API with 11 scenarios (happy path, validation failures, missing speed, etc.) and prints PASS/FAIL for each. Failure scenarios verify the middleware correctly maps upstream failures (500 status codes, timeouts) to 502 Bad Gateway and 504 Gateway Timeout responses, end-to-end through the full pipeline including WireMock.

---

## Resilience

- **Timeouts**: each downstream `HttpClient` has a configurable `Timeout` (10s for Speed Profile, 15s for Activation by default). On timeout, requests are wrapped in `SpeedProfileServiceException` / `ActivationServiceException` and surfaced as `504 Gateway Timeout`.
- **Cancellation**: a `CancellationToken` is threaded through the entire request pipeline. If the client disconnects, all downstream calls are aborted.
- **Retries**: not implemented (deliberately scoped out).

---

## Design decisions and assumptions

Decisions worth noting that are not obvious from the code alone:

- **`.NET 10`** is used rather than legacy `.NET Framework` 4.x. Modern .NET runs cross-platform (Linux/macOS/Windows) and inside standard Linux Docker containers.
- **Three-project layout** (`Core`, `Api`, `Tests`). Core has zero web/HTTP dependencies, making it trivially testable. Api references Core; Tests reference both.
- **TM Forum input + domain model split**: the wire format (`ProvisioningOrderRequest` with nested `oorderItem.service.serviceSpecification/servCharacteristic`) is mapped at the edge to a clean flat domain model (`ProvisioningRequest`). Business logic operates exclusively on the domain model.
- **Speed profile selection is client-side**: the Speed Profile API always returns the full catalog of available profiles regardless of input, so the orchestrator sends no request body and filters the response locally. The match is made on the `code` field of each returned profile against the `speedProfile` value extracted from the incoming TM Forum order (e.g., a request with `"speedProfile": "S123"` resolves to the profile with `"code": "S123"`). 
- **Network Controller is assumed synchronous**: the activation response contains `status` and `activatedAt`, suggesting the upstream completes activation before responding. The orchestrator passes through whatever status is returned, so an asynchronous upstream that returns `status: "PENDING"` would also work — the intermediate status would propagate to the caller.
- **Speed values flow `int → string`**: `SpeedProfile.DownloadMbps`/`UploadMbps` are typed as `int` (catalog data), while `ActivationRequest.DownloadMbps`/`UploadMbps` are typed as `string` (matching the assumed Network Controller wire format). The orchestrator converts via `int.ToString()`.

---

## Configuration

Settings live in `src/WifiProvisioning.Api/appsettings.json` and are overrideable via environment variables. Compose injects production values:

| Setting | Default | Description |
| --- | --- | --- |
| `SpeedProfileApi:BaseUrl` | `http://localhost:8080` | Base URL of the Network Infrastructure API |
| `SpeedProfileApi:TimeoutSeconds` | `10` | Request timeout |
| `ActivationApi:BaseUrl` | `http://localhost:8080` | Base URL of the Network Controller API |
| `ActivationApi:TimeoutSeconds` | `15` | Request timeout |

Environment variables use `__` for nested keys (e.g., `SpeedProfileApi__BaseUrl`).

---

## WireMock stubs

`wiremock/mappings/` contains:

- `speed-profiles.json` — `GET /speed-profiles` returns a catalog with profiles `S100`, `S500`, `S1000`, `S123`.
- `activation.json` — `POST /activation` returns `{ activationId: "ACT-<uuid>", status: "ACTIVE", activatedAt: <now> }` (UUID and timestamp generated per request via WireMock's response templating).

To simulate failures, edit a mapping (e.g., change `"status": 200` to `"status": 500`) and `docker compose restart wiremock`.

---

## Manual chaos testing

To verify the 502 mapping in a running stack:

```bash
docker compose up -d
docker compose stop wiremock
curl -i -X POST http://localhost:5080/api/provisioning \
  -H 'Content-Type: application/json' \
  -d @tests/manual/happy-path.json    # returns 502 Bad Gateway
docker compose start wiremock

---

