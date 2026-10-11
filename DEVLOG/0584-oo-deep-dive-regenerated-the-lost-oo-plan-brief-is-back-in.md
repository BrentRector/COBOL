## Entry 584 — 2026-07-03 16:20 PDT — OO deep-dive regenerated (the lost oo-plan brief is back IN-REPO) + a THIRD content-filter occurrence, now main-session

**① `docs/COBOLNET_OO_DESIGN.md` refreshed by the parallel re-scout workflow** (3 read-only scouts — legacy
mining, greenfield seams, spec verification — + 1 writer; 37 findings, ~362k tokens): the lost
`/e/tmp/phase2-briefs/oo-plan-brief.md` is regenerated INTO the canonical deep-dive. Landed: the four
spec-verified corrections with 2023 line anchors (method WS illegal in 2023 §13.5.3 SR1 @16461 —
LOCAL-STORAGE/LINKAGE are the 2023-legal method storage; EXIT METHOD removed 2023 §14.9.14.2 + Annex E @49034/
49036; INVOKE has NO ON EXCEPTION §14.9.23.2 @28376-28390 — EC-engine routing only; ABSTRACT is not ISO — 0
hits, struck from D7/METHOD ATTRS); the **Legacy port map** (slices S1–S3b + the typed-native flips from
DEVLOG 447-456, each with its portable algorithm — class-unit routing, INVOKE discrimination, depth-sorted
emission, SELF-virtual/SUPER-non-virtual dispatch, root-only instance state + ctor chaining, exit-bounded
per-method dispatch ranges — and explicit NOT-portable markers on the byte-substrate pieces); the
**Greenfield seams** (live Core/CobolOO.g4 vs the DEAD CobolParserOO.g4; the CallCollectUnits silent
class-unit-skip hazard → Phase-2 loud-guard sweep item; CallUnit as the ClassUnit emission template; the
already-landed two-signal StopRun/ProgramReturn architecture narrowing D8 to method-context binding); and a
"never landed = net-new" list (FACTORY, PROPERTY, EC-OO, multi-method params, …) + 10 adversarial regression
traps. Two stale-citation contradictions resolved as KEEP-decision-add-NOTE per the doc-hygiene rule.

**② AI friction, third occurrence (feedback_transparency): the API content filter fired in the MAIN session
this time** (2026-07-03, API error 400, reported live by the owner) — the trigger was dense COBOL word lists
arriving via TOOL RESULTS (a §8.9 sed excerpt + VCR rows 32/40 displayed in a grep result), NOT a model
reciting a list. The standing rule TIGHTENS: word lists never appear in tool output either — scripts print
counts only, greps of word-table files are forbidden, everything moves by file. Bug report drafted and
submitted to Anthropic (/feedback) with the three-occurrence evidence.

