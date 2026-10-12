## Entry 620 — 2026-07-05 21:26 PDT — the M2-DATA-3/4 adversarial review wave: 9 real findings fixed same change set

**The proven find→verify wave over the track-(a) diff (5 lenses × 2-skeptic verify) confirmed 6 findings by dual
skeptics; the Fable-5 limit then killed 34 of 53 verify agents mid-run, so I switched to Opus 4.8 and adjudicated
the unverified findings myself (each re-checked against the spec text + a live CLI probe). 9 real bugs fixed in
THIS change set; the rest refuted or staged with a name.** This is exactly why the wave exists — the goldens were
byte-exact and every battery green, yet the review surfaced NINE genuine defects, several silent-wrong-value.

**Confirmed + fixed (all probed on the prebuilt CLI):** (1) **apostrophe literals `N'…'`/`B'…'` stored their
quotes** — all three DecodeCobolString twins unwrapped only `"`; now either delimiter + doubled-opening-quote
collapse (§8.3.1.2), which ALSO closed a pre-existing plain-`'…'` STRINGLIT leak; (2) **§14.9.25.3 SR8** — a
BINARY-LONG→national MOVE compiled silently and destroyed the value (`00000`); added the SR8 guard ahead of the
Table-16 arms; (3) **figurative → ref-mod slice filled only position 1** (`MOVE ZERO TO N5(2:3)` → `A0␠␠E`); new
`RefModPlace.WriteFill` fills every position (§8.3.3.6.4 GR2); (4) **boolean figurative relation dropped the
zero-pad** (`IF B(1:2) = ZERO` compared unequal); threaded `pad:'0'` like the sibling legs; (5) **level-88 VALUE
category unchecked both directions** — `88 X VALUE B"01"` under PIC X accepted; routed through the ONE
ValidateValueCategory; (6) **class conditions on boolean** — `IS NUMERIC` on USAGE BIT answered a tautology;
new CheckClassConditionOperand (§8.8.4.4.3 SR8/SR4, DISPLAY-form boolean IS NUMERIC stays legal).

**Self-adjudicated real (skeptics died) + fixed:** (7) **`N"…"`/`B"…"` intrinsic args were a hard PARSE error** —
the SUB_NATLIT/SUB_BOOLLIT tokens were added to the lexer in 619 but NOT to the `subToken` parser rule, so the
Intrinsics decode arms were DEAD CODE and DEVLOG 619's "cured FUNCTION LENGTH(N\"AB\")" claim was aspirational;
added the two subToken alternatives — now genuinely cured (LENGTH(N"AB")=2); (8) **`ALL SPACES`/`ALL ZEROS` VALUE
falsely rejected** on PIC N/PIC 1 (the National arm rejected any ALL-prefix); (9) **SET 88 TO TRUE stored the
figurative WORD** (`88 F VALUE ZERO` → stored "ZERO"); EmitSet now fills via FigurativeWordFill. Plus two
correctness riders: **national/boolean HIGH/LOW-VALUE fill now uses the D-N3 pins** not the alphanumeric PCS
extreme (new `FigFill(kind, cat)`), and the **USAGE SR citations corrected `.4 → .3`** (SRs live in §13.18.60.3),
and the **double COBOLNET0900** for `PIC N USAGE NATIONAL` at <2002 suppressed.

**Refuted/staged:** `national_data` "asserted by no runner" REFUTED (CorpusRunnerTests byte-compares it);
zero-length `B""` named as residue #23 (the lexer's `[01]+` can't express it; `[01]*` risks ambiguity).

**Battery:** conformance **1776** (+16: 9 regression facts in the new NationalBooleanReviewFixTests + 5 negative
cases + the wave's own) · unit **159** · legacy ConformanceTests **55** · FULL legacy guard on the subToken .g4
change. The as-built + this disposition list live in docs/PHASE4_RECONCILIATION.md. ⚠ Process note: a workflow
that dies on a model-usage limit is not a failed review — its Find phase completed and the confirmed set + the
one-vote findings were all actionable; the fix is to finish verification in-session, which caught all 9.

