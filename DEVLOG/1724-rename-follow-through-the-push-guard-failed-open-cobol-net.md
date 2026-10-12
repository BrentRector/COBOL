## Entry 1724 — 2026-09-26 22:30 PDT — Rename follow-through: the push guard failed open; "COBOL.NET" → WiseOwl COBOL

**The rename commit (DEVLOG 1723) went RED in CI** on the audits job's guard-hook self-test:
`FAIL: expected BLOCK: 'cd /e/COBOL && git push origin HEAD:main'` (forbidden_commands self-test 20/21). The path
sweep had correctly rewritten the test's `cd /e/CobolSharp` to `cd /e/COBOL`, and the hook
(`scripts/hooks/forbidden_commands.py`) recognized "this repo" by the hard-coded literal `"cobolsharp"`, so a `cd` into
the renamed repo looked like ANOTHER repo and a direct push to main was let through — the guard failed OPEN, and the
self-test is what caught it. **Root fix:** the hook derives this repo's identity from its own location
(`Path(__file__).parents[2].name`), keeps the pre-rename name so older paths still match, treats a relative `cd` as
never leaving the repo (a pre-existing hole: `cd scripts && git push origin main` passed), and fails closed for a
`<name>-…` sibling. Self-test extended to 25 cases (old name, relative cd, sibling, an unrelated repo): 25/25;
readonly_repo 4/4. Sibling sweep: `scripts/cloud/setup-env.sh`'s SessionStart shim looked only for
`/home/user/CobolSharp`; it now tries `COBOL` first and still accepts the old folder (⚠ the cloud environment holds a
PASTED copy of this script — it must be re-pasted before the next cloud run).

**Product name.** "COBOL.NET" → **WiseOwl COBOL** in 778 places across 293 live files — the compiler's diagnostic
messages (DiagnosticCatalog), docs/CONFORMANCE.md (the §4.2.16 documentation), design docs, code comments, scripts,
the ledger title (`gen_ledger.py` TITLE → "WiseOwl COBOL Conformance Ledger"); docs/DIAGNOSTICS.md and
docs/DRIFT_RULES.md regenerated. NOT changed: identifiers (`COBOLNET####` codes, `Cobol.Net.*`, `COBOLNET_*.md`),
test programs and expected output (`.cob/.cpy/.out/.err` — fixed-form column limits, and their mentions are comments;
the one exception is `negative/pb531-picture-item-too-large.err`, which pins the diagnostic's text and changes with it,
while the embedded program `DISPLAY "HELLO, COBOL.NET"` in DataDisplayDifferentialTests stays as written because its
golden is keyed by the source hash),
history (DEVLOG, kb/Work, adjudication/, evidence/) and the traceability inventory's evidence text. GitHub: both
repository descriptions and the CLA gist's description now name WiseOwl COBOL; the public description lists all four
editions ("built to all four editions of standard COBOL (ISO/IEC 1989): 1985, 2002, 2014 and 2023").
Gate (local, Normal priority, whole assembly): first run RED on exactly those two (Conformance 9044/9046) — both
fixed as described, re-run by name 23/23; Unit 29491/29491; Characterization 33/33.

