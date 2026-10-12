## Entry 1716 — 2026-09-26 12:46 PDT — Community standards, user-documentation design (R50), WSL toolchain script

**GitHub community standards** — the repo's checklist lacked a code of conduct, contributing guide, security policy,
issue templates and PR template. Added, modeled on the large compiler repos (Roslyn, rustc, CPython, Go, Swift):
`CODE_OF_CONDUCT.md` (adopts Contributor Covenant 2.1; reports to conduct@wiseowl.com), `CONTRIBUTING.md` (ways to
contribute ranked by harm, the standard-is-authority rule with `cite.py --check`, the CLA, build + `build-local.ps1`
gate, what a PR needs, review through `ci-gate`), `CLA.md` (owner decision: PRs accepted under a CLA; modeled on the
Apache ICLA, grant extended to the BSL, its Change License and commercial licenses; owner-reviewed), `SECURITY.md`
(private GitHub vulnerability reporting only; 5-business-day ack, 14-day assessment; `main` supported), four issue
forms (wrong output, compiler crash — modeled on rustc's ICE form —, conformance finding requiring clause + rule,
feature/tooling; blank issues off) and `PULL_REQUEST_TEMPLATE.md`. Example citations in the templates checked with
`cite.py` (§14.9.25.4 GR1, §13.18.40.3 SR1 — the first drafts carried two clause numbers that named a Syntax-rules
and a General-formats subclause respectively). Repo settings, made with the owner's approval: private vulnerability
reporting ENABLED; labels `wrong-answer`, `crash`, `conformance`, `needs-triage` created. CLA Assistant configured by
the owner against gist e56c12d8… (file renamed `CLA.md`, signer-details `metadata` added); webhook verified active.
`license/cla` is deliberately NOT a required check on main — `push-main.sh` pushes commits, not PRs.

**User documentation (kb/Work R50)** — owner decisions: docs-as-code (no wiki), Astro Starlight on
wiseowlsoftware.com, a two-product home page with Demeanor primary, NuGet under the `WiseOwl.` prefix.
`docs/rearchitecture/DESIGN-USER-DOCUMENTATION.md` maps §4.2.16's seven referenced clauses to their sources, sets
the per-release docs bundle published at `/cobol/docs/<version>/`, the hand-written vs generated content map with
drift tests, and the proposed packages (`WiseOwl.Cobol` tool, `WiseOwl.Cobol.Runtime`). Work filed as PB1610–PB1614.

**WSL** — local WSL had no .NET SDK. `scripts/wsl/setup-wsl.sh` (user space, idempotent) installed .NET 10.0.401 and
CPython 3.14.7 (uv); the owner upgraded git to 2.55.0. First use: wave-61 D's Linux-only `/dev/full` test in
`FileBoundaryTests` ran under WSL (10/10) before its branch reached CI. DOC_INDEX updated for every new file.

