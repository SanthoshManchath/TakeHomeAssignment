
set -uo pipefail

BASE="${BASE:-http://localhost:5080}"

PASS=0
FAIL=0


color_pass=$'\033[32m'
color_fail=$'\033[31m'
color_dim=$'\033[2m'
color_off=$'\033[0m'

expect_status() {
    local name="$1"
    local expected="$2"
    local method="$3"
    local path="$4"
    local body="${5:-}"

    local actual
    if [[ -n "$body" ]]; then
        actual=$(curl -s -o /tmp/last-body -w '%{http_code}' \
            -X "$method" "$BASE$path" \
            -H 'Content-Type: application/json' \
            -d "$body")
    else
        actual=$(curl -s -o /tmp/last-body -w '%{http_code}' \
            -X "$method" "$BASE$path")
    fi

    if [[ "$actual" == "$expected" ]]; then
        printf "${color_pass}PASS${color_off}  %-50s  expected %s, got %s\n" "$name" "$expected" "$actual"
        PASS=$((PASS + 1))
    else
        printf "${color_fail}FAIL${color_off}  %-50s  expected %s, got %s\n" "$name" "$expected" "$actual"
        printf "${color_dim}      body: %s${color_off}\n" "$(cat /tmp/last-body | head -c 200)"
        FAIL=$((FAIL + 1))
    fi
}


SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WIREMOCK="${WIREMOCK:-http://localhost:8080}"

push_mapping() {
    curl -sS -X POST "$WIREMOCK/__admin/mappings" \
        -H 'Content-Type: application/json' \
        -d @"$1" > /dev/null
}

reset_wiremock() {
    curl -sS -X POST "$WIREMOCK/__admin/mappings/reset" > /dev/null
}


echo
echo "Running scenarios against $BASE"
echo "================================================================"

expect_status "Health"                         200 GET  "/health"

expect_status "Happy path"                     200 POST "/api/provisioning" \
    "$(cat "$SCRIPT_DIR/happy-path.json")"

expect_status "Speed not in catalog"           404 POST "/api/provisioning" \
    "$(cat "$SCRIPT_DIR/speed-not-available.json")"

expect_status "Empty body"                     400 POST "/api/provisioning" "{}"

expect_status "Malformed JSON"                 400 POST "/api/provisioning" \
    '{ invalid'

expect_status "Missing required characteristic" 400 POST "/api/provisioning" \
    "$(cat "$SCRIPT_DIR/missing-speed-char.json")"

expect_status "Wrong HTTP verb"                405 GET  "/api/provisioning"

expect_status "Unknown route"                  404 GET  "/api/nonsense"


echo "================================================================"
echo "Results: ${color_pass}$PASS passed${color_off}, ${color_fail}$FAIL failed${color_off}"
echo

[[ "$FAIL" -eq 0 ]]

echo
echo "Failure scenarios (override WireMock mappings)"
echo "----------------------------------------------------------------"

# Network Infrastructure API returns 500 → expect 502
push_mapping "$SCRIPT_DIR/failure-mappings/speed-profile-500.json"
expect_status "Speed Profile API 500"           502 POST "/api/provisioning" \
    "$(cat "$SCRIPT_DIR/happy-path.json")"
reset_wiremock

# Network Controller API returns 500 → expect 502
push_mapping "$SCRIPT_DIR/failure-mappings/activation-500.json"
expect_status "Activation API 500"              502 POST "/api/provisioning" \
    "$(cat "$SCRIPT_DIR/happy-path.json")"
reset_wiremock

# Network Infrastructure API hangs for 15s → expect 504 (client times out at 10s)
push_mapping "$SCRIPT_DIR/failure-mappings/speed-profile-timeout.json"
echo "(running timeout test — this takes ~10 seconds...)"
expect_status "Speed Profile API timeout"       504 POST "/api/provisioning" \
    "$(cat "$SCRIPT_DIR/happy-path.json")"
reset_wiremock