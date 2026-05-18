#!/usr/bin/env bash
# WireMock admin helpers

WIREMOCK="${WIREMOCK:-http://localhost:8080}"

# Push a mapping file to WireMock
push_mapping() {
    local file="$1"
    curl -sS -X POST "$WIREMOCK/__admin/mappings" \
        -H 'Content-Type: application/json' \
        -d @"$file" > /dev/null
}

# Reset WireMock to the mappings on disk (clears runtime overrides)
reset_mappings() {
    curl -sS -X POST "$WIREMOCK/__admin/mappings/reset" > /dev/null
}