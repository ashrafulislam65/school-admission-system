#!/usr/bin/env bash
BASE="${BASE:-http://localhost:8080}"
WAIT="${WAIT:-12}"

extract() { echo "$1" | sed -E "s/.*\"$2\":\"([^\"]+)\".*/\1/"; }

CLASS=$(curl -s -X POST "$BASE/classes" -H "Content-Type: application/json" -d '{"name":"Expiry Test","totalSeats":1}')
CLASS_ID=$(extract "$CLASS" id)
HOLD=$(curl -s -X POST "$BASE/classes/$CLASS_ID/holds" -H "X-Student-Id: 20152691234569991")
HOLD_ID=$(extract "$HOLD" holdId)

echo "hold created:"
echo "  $HOLD"
echo "class right after hold (expect heldSeats 1, availableSeats 0):"
echo "  $(curl -s "$BASE/classes/$CLASS_ID")"

echo "waiting $WAIT seconds, no API calls..."
sleep "$WAIT"

echo "hold after wait (expect EXPIRED):"
echo "  $(curl -s "$BASE/holds/$HOLD_ID")"
echo "class after wait (expect heldSeats 0, availableSeats 1):"
echo "  $(curl -s "$BASE/classes/$CLASS_ID")"
echo "second student applies (expect 201):"
echo "  $(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE/classes/$CLASS_ID/holds" -H "X-Student-Id: 20152691234569992")"