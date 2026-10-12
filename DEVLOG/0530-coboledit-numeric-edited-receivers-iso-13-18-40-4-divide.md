## Entry 530 — 2026-06-10 01:40 PDT — CobolEdit (numeric-edited receivers, ISO §13.18.40.4) + DIVIDE REMAINDER + alphanumeric→numeric MOVE → 2 new NC greens (35 total)

**Wave 3 of the diagnosis plan: the editing stack.** Three features in one change set, each complete to its spec
section:
- **`CobolEdit.Format(value, scale, mask)`** — the ONE numeric→edited conversion (ISO §13.18.40.4): zero
  suppression/replacement (Z, * incl. check-protect fill), fixed insertion (cs, leading/trailing +/-, CR/DB →
  spaces when non-negative), floating insertion ($$…, ++…, --… with rightmost-suppressed-position placement),
  simple/special insertion (B 0 / , .), full-field blanking for zero with no fixed 9. The algorithm is the legacy
  engine's `FormatByEditPattern` — proven over the NIST-85 corpus — re-hosted typed-native (unscaled long + scale,
  no byte areas, no decimal). `PicInfo.EditMask` carries the expanded picture (V retained); `CobolEdit.MaskScale`
  folds the mask's fraction scale at emit time. Wired into BOTH receiver paths: MOVE (§14.9.25.4 GR5 — incl.
  figurative ZERO, which EDITS to the mask, not a zero-fill: that early-return bug was caught by the new
  differential net) and arithmetic stores (§14.7.7), with **ROUNDED applied BEFORE editing** by rescaling to the
  mask scale with the receiver's mode (§14.7.4 — NC120A's `MULTIPLY … GIVING $ZZ9.99CR ROUNDED` $72.105→$72.11),
  and `ScaleOf` returning the MASK scale for edited receivers (a `.`-pointed mask has PIC Scale 0 — quotients must
  be computed at the mask's scale).
- **DIVIDE … GIVING q REMAINDER r** (§14.9.12 F4/F5): remainder = dividend − (intermediate quotient × divisor)
  with the intermediate quotient TRUNCATED at the quotient receiver's scale even when the stored quotient is
  ROUNDED (GR7); one-GIVING-receiver rule (SR6) enforced at bind.
- **Alphanumeric→numeric MOVE** (diagnosis B1, §14.9.25.4 GR6): an alphanumeric operand in a numeric context is an
  UNSIGNED integer — new `CobolNum.FromAlphanumeric`; `NumericRenderer.FieldNum` now NEVER emits a raw string read
  into numeric context (the bind-success ⇒ compilable invariant): alphanumeric converts, numeric-edited fails loud
  BY NAME (de-editing is its own wave).
**Spec-vs-legacy:** positive CR renders as TWO SPACES that remain part of the field (§13.18.40.4); the legacy
trims them on DISPLAY (its known §14.9.11.4 GR6 non-conformance) → that case is SPEC-PINNED.
**Result: NC117A + NC120A byte-match → 35 NC.** The edited cluster's remaining diffs (NC203A/251A/170A/172A/
173A/175A) are the **deferred G3 Int128 intermediate-arithmetic gap** (18-digit dividends, π-precision quotients —
exactly the documented NC171A blocker), NOT editing bugs; NC114M needs ALPHANUMERIC-edited insertion (a separate
category path). Those are the next wave. New `NumericEditedDifferentialTests` (9 facts). **359 conformance + 15
unit green.**

