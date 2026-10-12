## Entry 681 — 2026-07-08 02:58 PDT — Bind-time gating migration Cluster 2 — CALL … BY VALUE (dead manual-gate → the ONE Check funnel)

Second cluster of the bind-time gating migration (plan: `docs/rearchitecture/PLAN-bindtime-gating-migration.md`).
CALL … BY VALUE (ISO §14.9.4, introduced 2002) was gated in TWO redundant places: a parse-time `{is2002()}?`
predicate on `callByValue` (which rejected below 2002) AND a manual `COBOLNET0883` edition gate in
`StatementBinder.Call.cs` (dead code — unreachable below 2002 because the parser rejected first). Ungated the
grammar (`CobolParserCore.g4:958`) and replaced the manual 0883 gate with
`ConstructRegistry.Check(…, Constructs.CallByValue2002, "the CALL … BY VALUE phrase")` — one funnel, emitting
`COBOLNET0900` below 2002 at the argument's recognition point. Retired the two `EditionGateHints` BY/VALUE signature
arms. `COBOLNET0883` is fully removed (no test pinned it — the 0883 grep hits were NIST sequence numbers). BY/CONTENT
and BY/REFERENCE are 85-legal and unaffected. Verified: `CALL "S" USING BY VALUE W` → `COBOLNET0900` at 85, compiles
at 2002; `EditionGateDiagnosticTests.CallByValue_At85` green (substrings unchanged). Battery: conformance 2055 · unit
224 · FULL legacy guard NIST 353 MATCH.

