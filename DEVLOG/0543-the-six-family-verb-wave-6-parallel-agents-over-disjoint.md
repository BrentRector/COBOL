## Entry 543 — 2026-06-10 12:41 PDT — THE SIX-FAMILY VERB WAVE: 6 parallel agents over disjoint partial-class files → 55→74 NC green (+19, zero lost), 556 conformance

**The experiment worked.** Six implementation agents ran CONCURRENTLY, each owning only its family's disjoint files
(bound records + `StatementBinder.<Verb>.cs` + `CSharpEmitter.<Verb>.cs` + a runtime class + a focused test file),
each fed a scout-built brief (ISO GR digest with line numbers + the legacy algorithm mined with file:line + the
greenfield grammar shapes + hook points). No agent built or touched a shared file — every shared-file wiring came
back as a structured integration edit (exact anchor + insertion) that I applied serially: 4 dispatch arms in the
BindStatement switch, 3 stub replacements (ADD/SUBTRACT/MOVE CORR routes, SET F3), the switch-condition hook
(AFTER level-88 — NC211A guard held), 10 emitter cases, 2 ContainsNextSentence arms, the AlterEmitFields hook,
the DataBinder partial flip + SPECIAL-NAMES registry call, PicInfo.IsAlphabetic, the ConditionRenderer switch arm,
and the test-harness stdin/env plumbing. ONE build error in ~6,000 agent-written lines (a null-deref pattern miss).

**The families (all complete to the 85 surface per spec, post-85 edition-gated):** INSPECT §14.9.22 (the GR8 single
shared comparison cycle + GR9 per-operand BEFORE/AFTER regions, TALLYING/REPLACING/both/CONVERTING, GR4d sign
handling; BACKWARD gated 2023); STRING §14.9.43 / UNSTRING §14.9.48 (POINTER, DELIMITED BY/OR, DELIMITER-COUNT-
TALLYING IN, ON OVERFLOW); INITIALIZE §14.9.20 (bind-time expansion through the ONE MOVE path, REPLACING categories,
85 exclusions; FILLER/VALUE/DEFAULT gated 2002+); ACCEPT §14.9.1 (device reads with MOVE-rule stores, FROM
mnemonic via the SPECIAL-NAMES registry, DATE/DAY/DAY-OF-WEEK/TIME; YYYYMMDD forms gated 2002+); MOVE/ADD/SUBTRACT
CORRESPONDING §14.7.6 (the 6 correspondence rules INCLUDING the rule-2 Table-16 and rule-3 both-numeric filters the
legacy lacked, D1-order bind-time pair expansion, subscripts hoisted once, STATEMENT-level SIZE ERROR aggregation);
ALTER + bare GO TO 85-ONLY (the D4 per-paragraph mutable field; REJECTED ≥2002 as deleted — COBOLNET0810/0811, the
SSOT §18 #10 conflict reconciled in this change set) + external switches §12.3.7 (ExternalSwitches runtime with the
legacy COBOL_SWITCH_n env contract) + SET F3 §14.9.39 + switch conditions §8.8.4.6.

**Integration fixes (mine, serial, each spec-cited):** JUSTIFIED §13.18.34 into the data model + both store paths
(NC218A's PIC X JUST); SPECIAL-NAMES user-defined classes §12.3.7 + the user-class condition §8.8.4.1.4
(CobolClass.IsInClass; THRU ranges either order); the typed-numeric IS NUMERIC fold restricted to NATIVE storage —
a REDEFINES view / StoreAsImage leaf tests its image sign-aware (§8.8.4.4 r3, CobolClass.IsNumericZoned — NC174A
GF-8/10's S9(18) over X(18) garbage); level-66 RENAMES references §13.18.45 (RenamesPlace — ONE composed
elementary-alphanumeric view over the spanned leaves, FindDescendant now searches Renames66 for qualification);
DE-EDITING §14.9.25.4 GR5 (CobolEdit.DeEdit — NC222A's NE→numeric move); INITIALIZE of AN-edited receivers keeps
insertion characters (figurative no longer bypasses the mask — NC223A's "     /"); and the PERFORM VARYING
AFTER-FROM-outer-var ordering: the OUTER augments FIRST, THEN the inner re-inits from its CURRENT FROM (§14.9.28
GR13e / '85 GR10(d)1 — NC201A PFM-TEST-F4-23 wants 3+2+1=6 iterations, we produced 3+3+2=8).

**Census: 55 → 74 NC GREEN (+19: NC109M 115A 122A 174A 201A 202A 204M 207A 208A 209A 216A 217A 218A 221A 222A 223A
253A 254A 302M), ZERO lost. 556 conformance (62 agent-written spec-cited differential facts + 19 new NIST locks) +
15 unit green.** Remaining non-green: 6 DIFFs already fixed on the `arith-waves` worktree branch (next entry),
NC104A/124A (unmapped), NC215A/219A (collating, step ③), NC235A (ODO), NC236A (spec-pinned legacy divergence),
NC105A/224A/247A/252A/401M (runtime/REDEFINES-tier residue), NC303M (no golden — flag program).

