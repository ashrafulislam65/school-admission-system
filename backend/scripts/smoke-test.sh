#!/usr/bin/env bash
BASE="${BASE:-http://localhost:8080}"
S1=20152691234560001
S2=20152691234560002
S3=20152691234560003

# call <label> <expected-code> <METHOD> <path> [student-id] [json-body]
call() {
  local label="$1" expect="$2" method="$3" path="$4" student="$5" body="$6"
  local args=(-s -w "\n%{http_code}" -X "$method" "$BASE$path")
  [ -n "$student" ] && args+=(-H "X-Student-Id: $student")
  [ -n "$body" ] && args+=(-H "Content-Type: application/json" -d "$body")
  local out
  out=$(curl "${args[@]}")
  LAST_CODE=$(echo "$out" | tail -n1)
  LAST_BODY=$(echo "$out" | sed '$d')
  if [ "$LAST_CODE" = "$expect" ]; then
    echo "PASS [$LAST_CODE] $label"
  else
    echo "FAIL [$LAST_CODE, expected $expect] $label"
  fi
  [ -n "$LAST_BODY" ] && echo "       $LAST_BODY"
}

extract() { echo "$1" | sed -E "s/.*\"$2\":\"([^\"]+)\".*/\1/"; }

call "create class with 2 seats" 201 POST /classes "" '{"name":"Smoke Test Class","totalSeats":2}'
CLASS_ID=$(extract "$LAST_BODY" id)

call "student 1 applies" 201 POST "/classes/$CLASS_ID/holds" "$S1"
H1=$(extract "$LAST_BODY" holdId)
call "student 2 applies" 201 POST "/classes/$CLASS_ID/holds" "$S2"
H2=$(extract "$LAST_BODY" holdId)
call "student 3 applies, class is full" 409 POST "/classes/$CLASS_ID/holds" "$S3"
call "student 1 applies twice" 409 POST "/classes/$CLASS_ID/holds" "$S1"
call "apply without header" 400 POST "/classes/$CLASS_ID/holds"

call "get hold 1" 200 GET "/holds/$H1"
call "get unknown hold" 404 GET "/holds/nope"
call "my holds (student 1)" 200 GET "/me/holds" "$S1"

call "student 2 confirms student 1's hold" 403 POST "/holds/$H1/confirm" "$S2"
call "student 1 confirms" 200 POST "/holds/$H1/confirm" "$S1"
call "student 1 confirms again (same result)" 200 POST "/holds/$H1/confirm" "$S1"
call "class counts: held 1, confirmed 1" 200 GET "/classes/$CLASS_ID"

call "student 2 cancels" 204 DELETE "/holds/$H2" "$S2"
call "student 1 cancels a confirmed hold" 409 DELETE "/holds/$H1" "$S1"
call "student 3 applies, seat is free again" 201 POST "/classes/$CLASS_ID/holds" "$S3"
call "final class counts: held 1, confirmed 1" 200 GET "/classes/$CLASS_ID"