# Assumptions

The brief leaves some things open. These are the choices made, and why.

## Seats and concurrency

- A seat is taken by an application that is `CONFIRMED`, or `HELD` and not yet expired.
- To keep `held + confirmed` at or below `totalSeats` even under many parallel requests, applying runs in a database transaction that first locks the class row (`SELECT ... FOR UPDATE`). Requests for the same class wait for each other, so seats are counted one request at a time. Requests for other classes are not blocked.
- The lock lives in PostgreSQL, not in the app's memory. This is how two backend processes sharing one database stay correct. This was designed for, but only tested with one backend process.

## Expiry

- A hold expires by time, not by a background job. A `HELD` application whose `expiresAt` has passed is treated as `EXPIRED` everywhere (seat counts, status, apply, confirm). The seat is free again with no API call.
- The database row may still say `HELD` until someone touches it. Confirming an expired hold marks it `EXPIRED` and answers `409`.
- `HOLD_DURATION_SECONDS` must be a positive whole number. Otherwise the default `600` is used.
- All times are UTC (ISO 8601). The browser countdown uses the user's clock, so it can differ from the server by a few seconds. The server decides. If the countdown says time is left but the server says expired, the user gets a clear message and the screen corrects itself within 3 seconds.

## Students

- The student is identified only by `X-Student-Id`. It must be exactly 17 digits, else `400`. There is no password, because the brief says the number is the identity.
- One student can have one active application (`HELD` or `CONFIRMED`) per class. Applying again to the same class gives `409`. After `EXPIRED` or `CANCELLED` the student can apply again. A student can apply to several different classes.
- Only the owner can confirm or cancel an application. Someone else gets `403`.

## Confirm and cancel

- Confirm twice returns `200` with `CONFIRMED` and counts the seat once.
- Confirm on a `CANCELLED` or `EXPIRED` application gives `409`.
- Cancel (`DELETE`) returns `204`. Cancelling an already cancelled application also returns `204`. A `CONFIRMED` admission cannot be cancelled (`409`), because the fee is already paid. An expired application gives `409`.
- Confirm and cancel lock the application row, so they cannot both succeed at the same time.

## Other API choices

- `GET /holds/{holdId}` does not need the student header (the brief does not list one), and shows `EXPIRED` once time has passed.
- `POST /classes` needs a non-empty name (max 200 characters) and a whole number of seats, 1 or more, else `400`.
- Errors are JSON like `{ "error": "message" }` with a message a person can read.
- IDs are `c_` or `h_` followed by 8 random hex characters.
- There is no admin login. Any logged-in user can create a class, because the brief asks for a screen for the school admin but no admin accounts.
- The database tables are created at startup (`EnsureCreated`), not with migrations, to keep setup to one command.
- CORS allows any origin, to let the frontend call the backend from another port.

## Frontend

- Seat numbers and applications refresh every 3 seconds, and again when the tab becomes visible.
- The logged-in student ID is kept in the browser (`localStorage`). Applications live on the server, so they remain after a refresh or after the browser is closed.
- "Pay admission fee" counts as a successful payment (no real gateway).