## Entry 725 — 2026-07-09 18:52 PDT — PHASE-03 Step 14h.1: the parse-arm foundation (EditionValidator absorbed, run post-bind, fail-fast deleted)

Step 14h begins the root-cause fix for the DEVLOG-724 finding (introduction gates must fire on the construct's
syntactic RECOGNITION, not its bound node — the bound-arm silently drops the 0900 when a below-edition construct
ALSO fails to bind). 14h.1 is the FOUNDATION commit: it stands up the parse-tree arm and re-times it, WITHOUT yet
moving any statement gate (those relocate byte-identically in 14h.2–14h.4). Driven by the decision-complete recon
`wf_be806171-b25` (5 agents, 667k tok).

- **`EditionValidator` absorbed into `VersionConformancePass` as a nested `ParseArm` visitor** — a verbatim port of
  its ~22 removal/obsolete/comment/segment overrides + the §8.9 reserved-word funnel (`VisitCobolWord` +
  `CheckedTokenTypes`) + the WITH-DEBUGGING-MODE comment-treatment posture; every gate now routes through the
  pass's ONE `Check`. `IsProvableUserWordPosition` moved to the outer pass as `internal static`.
- **The pass is now genuinely TWO-ARM**: `Run` walks the raw `BoundRunUnit.Tree` with the ParseArm FIRST (syntactic
  gates on recognition), then walks the bound programs (semantic gates), over ONE `EditionContext` sink.
- **The driver dropped the pre-bind `EditionValidator` + its fail-fast**; the post-bind `VersionConformancePass.Run`
  is now the SOLE edition gate, with the single post-pass `HasErrors` return. `EditionValidator.cs` DELETED.
- **Intended consequence (design §2.5 / DEVLOG 724):** a below-edition construct that ALSO fails to bind now
  surfaces BOTH its edition diagnostic AND its bind diagnostics — the pass runs after bind so both accumulate. No
  verdict changes (the tests are contains-based; ST101A already witnessed the both-codes case).
- `ReservedWordPositionTests` repointed `EditionValidator.IsProvableUserWordPosition` →
  `VersionConformancePass.IsProvableUserWordPosition` (InternalsVisibleTo(Unit) already present).

The bound-arm STATEMENT gates (UNLOCK/FREE/ALTER/DELETE-FILE/SET-ADDRESS; OPEN-SHARING/GOBACK-RETURNING/
CALL-BY-VALUE/CALL-ON-OVERFLOW/STOP-STATUS/END-ACCEPT) are UNCHANGED this commit — they migrate to the parse-arm in
14h.2–14h.4. ALLOCATE stays bind-time until 14h.2; UDF is a documented PRINCIPLED exception (an intrinsic FUNCTION
and a user-function call are syntactically identical — only the repository-resolved name set separates them, and
bind-time already fires on recognition before operand binding). 7 more gates the recon proved need a resolved
semantic fact (SET object-ref, SET pointer UP/DOWN, INVOKE, READ PREVIOUS, START FIRST/LAST, START WITH LENGTH,
READ ADVANCING-ON-LOCK) STAY bound-arm permanently — a naive presence check would over/under-fire them.

**Process transparency:** my first 14h.1 draft over-trimmed the bound-arm — it pre-removed ~10 statement-gate cases
(the ones slated for 14h.2–.4) WITHOUT adding them to the parse-arm, so `DELETE FILE`@85 stopped rejecting and
`ALTER`@permissive stopped warning (22 conformance + 2 unit reds). Caught immediately by a FRESH-build battery
([[feedback_fresh_build_before_no_build_test]]); restoring the bound-arm verbatim greened it. (Stale prose comments
in DataBinder/OoClassTable/StatementBinder still naming "the EditionValidator" as a gate's home are cosmetic and
swept in 14h.5.)

**Battery (fresh CobolSharp.sln build):** greenfield conformance 3114 · unit 227 · characterization 32 · INV-1-strong
byte-exact @ 2023 permissive 349/349 · FULL legacy guard NIST 353 MATCH (unaffected — 14h is greenfield-only, no
grammar change) + legacy unit 1196 + integration 608.

