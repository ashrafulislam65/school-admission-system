# AI Usage

## Tools

- **Claude** (Anthropic chat assistant), used through the chat window for this whole project.

## Where I used it

- Reading the assignment and planning the steps and commits.
- Backend: models, `DbContext`, controllers, the row-locking approach for seats, DTOs.
- Backend tests (concurrency, rules, expiry) and the shell test scripts.
- Dockerfiles and `docker-compose.yml`.
- Frontend: API client, login, class list, create class, applications screen, countdown.
- Drafts of README and ASSUMPTIONS.

I pasted the code file by file, built and ran it after every step, and committed each step separately. I ran the commands and tests myself and gave the output back when something failed.

## Where the AI was wrong, and how I found it

1. **Wrong repo layout.** The first setup put the backend at the repository root. The brief wants one repository with `backend/` and `frontend/` folders. I noticed when I re-read the brief, and I started again with the right layout.
2. **Command run in the wrong folder.** A `cd ..` followed by `git add .` ran in a bigger parent repository instead of the project. It failed before anything was staged, and I saw it in the terminal output. Nothing was committed there.
3. **Port conflicts.** The default PostgreSQL port 5432 was already used by PostgreSQL installed on my PC, so the app failed with "password authentication failed". Also an old nginx container was using port 8080. I found both from the error message and `docker ps`, moved the development database to port 5433 and stopped the old container.
4. **Code pasted in the wrong place.** An instruction about where to add methods in `HoldsController.cs` was unclear. I pasted a method outside the class and `dotnet build` failed with CS0106. I replaced the whole file with the full version.
5. **Placeholder copied literally.** I ran `curl` with `c_XXXX` and `h_XXXX` from the instructions, and got "Class not found". I replaced the manual steps with a script that reads the real ids.
6. **Git commands from the wrong directory.** Several `git add` commands failed because I was inside `frontend/`. I ran them again from the repository root.

## What I checked myself

- `dotnet test` passes, including the test where 60 requests compete for 5 seats.
- `smoke-test.sh` and `expiry-test.sh` pass against the Docker setup.
- Data is still there after `docker compose down` and `docker compose up`.
- I used the app in the browser: apply, countdown, expiry without reload, pay fee, cancel, refresh and reopen.