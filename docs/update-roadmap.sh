#!/usr/bin/env bash
# Writes the issues the docs site's roadmap shows. The Docs workflow runs it before each publish; run it from the
# repository root, with the GitHub CLI signed in, to refresh the copy in git.
set -euo pipefail

# An issue closed as not planned or as a duplicate is dropped, not shipped.
gh issue list --state all --limit 1000 --json number,title,state,stateReason,milestone,labels \
    --jq '[.[] | {number, title, open: (.state == "OPEN"), phase: .milestone.title, labels: [.labels[].name],
            dropped: (.state == "CLOSED" and .stateReason != null and .stateReason != "COMPLETED")}]
        | sort_by(.number)' \
    > docs/Trailblazor.Docs.Examples/roadmap.json
