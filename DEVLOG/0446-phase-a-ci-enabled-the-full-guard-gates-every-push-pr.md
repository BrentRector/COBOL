## Entry 446 — Phase A: CI enabled — the full guard gates every push/PR

Enabled continuous integration (MASTER_PLAN Phase A, early-resolve #2). Promoted the dormant
`.github/workflows/build-and-test.yml.disabled` to a live `build-and-test.yml` and fleshed it out so the **full
guard is the gate**, not just unit+integration:

- **`guard` job (ubuntu-latest):** runs `bash scripts/guard.sh` — the project's "law" (build + unit + integration
  + the 364 NIST CCVS85 baselines). A non-zero guard exit (any regression / DIFF / FAIL* / build warning, since
  `TreatWarningsAsErrors=true`) fails the job. Creates the untracked `tests/nist/output/` dir first (a clean
  checkout doesn't have it — the guard copies the runtime DLL there). Linux-only because the NIST loop is bash.
- **`windows-build-test` job (windows-latest):** a clean Release build (warnings-as-errors) + the unit &
  integration suites, for the cross-platform coverage the bash guard can't give.
- **Hygiene:** `submodules: false` (the `specs/` submodule is a private repo, NOT needed to build/test — it is
  only spec text referenced from doc comments, confirmed by grep); SDK pinned `10.0.x`; a `concurrency` group with
  `cancel-in-progress` so a rapid second push cancels the superseded run.

Triggers on push + PR to `main`. Infra-only (a GitHub Actions YAML); the local guard is unaffected and stays
green (1204 / 509 / 364). Removed the `.disabled` template (consolidated into the live file).

