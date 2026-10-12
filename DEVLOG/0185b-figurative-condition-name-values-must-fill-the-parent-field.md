## Entry 185 — 2026-05-28: Figurative condition-name values must fill the parent field — NC250A 100% (20 remaining)

**Bug (NC250A IF--TEST-26):** `IF B OF IF-D33 AND NOT B OF IF-D32` where `IF-D33`
is `PIC X(4)` set to QUOTE and `88 B VALUE QUOTE`. Expected PASS, got FAIL.

**Root cause** — `SemanticBuilder.ParseConditionClauseOperand` translated the
figurative `QUOTE` in a level-88 VALUE clause to `ConditionValue.FromString("\"")` —
a *single* quote character. At comparison time the lowerer space-pads the 1-char
literal to the parent's 4 bytes (`"` + 3 spaces) and compares against the field's
actual `""""`, which never matches. `SPACE` only "worked" by coincidence (its pad
character is also space). The note in the test confirms the intent: "TEST OF
ALPHANUMERIC FIELD FOR FIG-QUOTES."

**Fix** — A character figurative assumes the *size of its associated field*
(ISO 1989:1985 8.3.1.2) — semantically `ALL <char>`. The repeat-to-fill machinery
already existed via `ConditionValue.FromAllString` / `IsAllLiteral` (Entry 183).
Changed `SPACE`, `QUOTE`, `HIGH-VALUE`, `LOW-VALUE` to `FromAllString` so the
lowerer expands the single-char pattern to the parent's `StorageLength` before
comparing. `ZERO` stays `FromNumeric(0)` — it must compare numerically against
numeric condition-name parents.

Empirically isolated first: `IF IF-D33 = QUOTE` was already TRUE (direct figurative
comparison works), but `IF B` (88 VALUE QUOTE) was FALSE — pinpointing the 88-value
path, not qualification or the combined/NOT condition (the session-state's prior
guess). NC250A: 1→0 FAIL*, now 115/115. Guard green.

**Known latent issue (not a NIST blocker, logged for later):** `IF <qualified-cond>
AND NOT <qualified-cond> DISPLAY ... ELSE DISPLAY ...` (qualified condition-name in a
combined condition followed by an inline statement) mis-parses in synthetic repros
("no viable alternative at 'DISPLAY'"). No NIST test exercises this — NC250A uses
`PERFORM PASS/FAIL` and parses/passes correctly. Fixing it would touch the ANTLR
grammar (requires approval), so deferred.

---

