# School Admission System

Online admission for a private school. Admission is first come, first served. When a student applies, a seat is put on hold for a limited time. If the admission fee is paid in time, the admission is confirmed. Otherwise the seat is released for other students.

- Backend: C# / ASP.NET Core (.NET 10), PostgreSQL
- Frontend: React 19 + TypeScript (Vite)
- Everything runs with Docker

## Run

Only Docker is needed.

```bash
docker compose up --build
```

- Frontend: http://localhost:3000
- Backend API: http://localhost:8080

Data is kept in the Docker volume `pgdata`, so it survives `docker compose down` and `docker compose up`. Do not use `docker compose down -v`, that deletes the data.

## Configuration

| Variable | Used by | Default | Meaning |
|---|---|---|---|
| `DB_CONNECTION` | backend | set in `docker-compose.yml` | PostgreSQL connection string |
| `HOLD_DURATION_SECONDS` | backend | `600` | How long a seat stays on hold |
| `API_URL` | frontend | `http://localhost:8080` | Backend URL, read when the container starts |

`API_URL` is used by the browser, so it must be reachable from the browser. Example with a 30 second hold:

```bash
HOLD_DURATION_SECONDS=30 docker compose up -d
```

## How to use

1. Open http://localhost:3000 and log in with a 17-digit birth registration number. The number is shown at the top of every page.
2. **Create class**: a name and a number of seats. Every shift is its own class, for example "Class 6 - Morning Shift".
3. **Classes**: total, on hold, confirmed and available seats. The numbers refresh every 3 seconds. Press **Apply now** to hold a seat.
4. **My applications**: status, a countdown, **Pay admission fee** and **Cancel**. When the time ends the application shows `EXPIRED` and the fee button is disabled, without reloading the page.

Pressing "Pay admission fee" is treated as a successful payment. There is no real payment gateway.

## API

Base URL `http://localhost:8080`, JSON. The student is identified by the `X-Student-Id` header (17 digits).

| Method | Path | Description |
|---|---|---|
| POST | `/classes` | Create a class |
| GET | `/classes` | List classes with seat counts |
| GET | `/classes/{classId}` | One class with seat counts |
| POST | `/classes/{classId}/holds` | Apply (hold a seat), `409` if no seat |
| GET | `/holds/{holdId}` | Status of an application |
| POST | `/holds/{holdId}/confirm` | Pay the fee, confirm admission |
| DELETE | `/holds/{holdId}` | Cancel an application, `204` |
| GET | `/me/holds` | All applications of this student |

## Tests

Backend tests (xUnit) run against a real PostgreSQL, including a concurrency test where many requests try to take the same seats at once. They use their own database `school_admission_test`, which is created automatically. Start a PostgreSQL on port 5433 and run:

```bash
docker run --name school-test-pg -e POSTGRES_PASSWORD=postgres -p 5433:5432 -d postgres:16
dotnet test backend
```

To use another database, set `TEST_DB_CONNECTION` to its connection string.

Two scripts check a running app (`docker compose up`) from the outside:

```bash
bash backend/scripts/smoke-test.sh
bash backend/scripts/expiry-test.sh
```

`expiry-test.sh` needs `HOLD_DURATION_SECONDS=10` on the running backend.

## Project layout

```
backend/    ASP.NET Core API, tests, scripts, Dockerfile
frontend/   React app, Dockerfile, nginx config
docker-compose.yml
ASSUMPTIONS.md
AI_USAGE.md
```