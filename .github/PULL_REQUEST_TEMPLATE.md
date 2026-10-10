<!--
Thanks for contributing! Please read CONTRIBUTING.md first. First-time contributors: the CLA bot will ask you to
sign the Contributor License Agreement (CLA.md) — once, for all your future pull requests.
-->

## What this changes

<!-- One defect or one feature. Link the issue: "Fixes #123". -->

## Why — the standard

<!-- The clause and rule that require this behavior, e.g. §14.9.25.3 SR1, checked with
     `python scripts/spec/cite.py --check <clause> "<text>"`. Behavior changes without a citation cannot be merged. -->

## How it was verified

<!-- The gate you ran and its verdict line, e.g.
     pwsh scripts/build-local.ps1 -Mode implementer  →  === BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — … === -->

## Checklist

- [ ] The fix addresses the root cause (no workaround in a test program, no special case for a test).
- [ ] New or changed behavior has a conformance program under `tests/conformance/` (a golden at the introducing
      edition, plus a negative for earlier editions where one applies), registered in its `manifest.json`.
- [ ] I looked for the same defect in sibling code paths.
- [ ] Documentation that describes the changed behavior is updated in this pull request.
- [ ] I have signed the CLA.
