## Entry 680 — 2026-07-08 02:40 PDT — Bind-time edition-gating migration — plan + Cluster 1 (proof of pattern: ALLOCATE / FREE / USAGE OBJECT REFERENCE)

Owner directive after DEVLOG 679: "implement correctly regardless of rework cost" — i.e. don't settle for the
R1-mitigation that keeps the reverse *signatures*; do the correct architecture. That is the north-star from 679:
move edition **introduction gating** from parse-time REJECTION predicates to **bind-time** `ConstructRegistry.Check`
at each construct's recognition point (the exact pattern the five inline gates already use). A hoisted `{Gate}?`
predicate can never reliably mean "the user wrote this construct" (it fires speculatively at the stuck token);
gating where the construct is genuinely *recognized* removes both the speculative-eval class of wrong diagnostics
AND the reverse-signature heuristics.

**Recon (wf_9c48ce3f, 6 agents) mapped all 30 gated constructs** into `docs/rearchitecture/PLAN-bindtime-gating-migration.md`
(decision-complete, resumable): **24 MOVE_TO_BINDTIME** (10 ordered clusters, easiest→riskiest) + **6 KEEP_PARSE_GATED**
reservation-word residue (XOR / boolean ops / SHARING / RETRY / UNLOCK / PROPERTY — context-sensitive keywords that
are valid *user-defined names* below their edition, so ungating would miscompile a valid lower-edition program; a
correctness limit, not cost). Architecture decision (verified against 25+ live in-binder Checks): keep gating
IN-BINDER at the recognition point, NOT a separate EditionValidator walk — the ONE `Check` funnel + `EditionSeverityPolicy`
already centralize the decision, and the `where` context (data-name/FD/class) is free at recognition; a pass would
re-walk to re-derive it (duplicate dispatch).

**Cluster 1 — proof of pattern (dead-Check activation; ZERO new binder code).** Three constructs already carried a
bind-time `ConstructRegistry.Check` that was unreachable below its edition because the parse gate rejected first:
ALLOCATE (`StatementBinder.Ptr.cs:91`), FREE (`Ptr.cs:127`), USAGE OBJECT REFERENCE (`PicInfo.cs:651`). Deleting the
grammar predicates (`CobolParserCore.g4:680/681`, `CobolData.g4:328` — the last gated `usageKeyword`, joining its
already-ungated NATIONAL/BIT/POINTER siblings) makes each construct parse at every edition; the dormant Check now
fires the 0900 below 2002. Retired the three `EditionGateHints` signature arms. Also cleaned the ALLOCATE/FREE Check
`where` strings, which embedded the ISO citation and so *doubled* it once the Check went live (`… - ALLOCATE (ISO
§14.9.3) (ISO §14.9.3)` → `… - the ALLOCATE statement (ISO §14.9.3)`).

**Interaction found + fixed (a textbook case of the fragility this migration removes).** Ungating USAGE OBJECT
REFERENCE broke the `set-object-reference-2002` version-matrix fixture (`01 W USAGE OBJECT REFERENCE` + `SET W TO
NULL`): at 85 its 0900 previously came *incidentally* from the USAGE **parse** gate (which failed first in the data
division). With USAGE now parsing, the error surfaces at the still-gated `SET` — ANTLR reports `NoViableAlt` at the
`SET` token, but the `EditionGateHints` set-object-ref signature keyed on `NULL`/`SELF` being the offending token, so
no 0900 fired (generic `COBOL0102` instead). Fix: added a `SET`-token arm (an object-reference sender `NULL`/`SELF`/
`SUPER` ahead in the sentence) — restores the correct `SET … TO object-reference (Format 5) requires COBOL-2002`.
This signature is retired when set-object-reference itself moves to bind-time (Cluster 8). The episode is exactly why
the reverse signatures must go: a neighbour's migration shifted where the error surfaces and the hand-tuned signature
silently missed it — caught here only because a test exercised it.

Verified: ALLOCATE/FREE/USAGE OBJECT REFERENCE now gate at bind time (COBOLNET0900 below 2002); the valid uses compile
at 2002; typos stay neutral; the set-object-ref fixture's 0900 is restored. Battery: **conformance 2055 · unit 224 ·
FULL legacy guard NIST 353 MATCH**, 0 regressions. 9 clusters remain (see the plan's STATUS + checkboxes).

