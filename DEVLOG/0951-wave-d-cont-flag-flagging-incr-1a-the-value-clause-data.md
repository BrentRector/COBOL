## Entry 951 — 2026-07-21 15:05 PDT — Wave D (cont.): FLAG flagging Incr 1a — the VALUE-clause data options g NUM-ED-ZERO-FIGCONST + l VALUE-ZERO (parse-arm, PictureAnalyzer for category)

The first data-division FLAG detectors, on the design basis the owner's spec question settled: **parse-arm
anchoring** (the flag fold is source-position-based per §7.3.15.4 GR2 "all text that follows"), condition from the
**parse tree** (GR4 "the syntax to be diagnosed"). `FlagConformancePass.VisitDataDescriptionEntry` flags the
figurative constant ZERO in the VALUE clause of a **numeric-edited** item — g (NUM-ED-ZERO-FIGCONST) and l
(VALUE-ZERO) are the SAME predicate stated two ways, so one detector serves both, each firing only if its own
option is ON (both ON ⇒ two warnings, the literal per-option reading).

**Category via the ONE mechanism, no data-arm.** "Numeric-edited" is a PICTURE-string property (§13.18.40), so the
detector reuses `PictureAnalyzer.Analyze` on the parse-tree PIC_STRING — with a **discard `EditionContext`** (at 2023,
the superset) so re-classifying the already-bound picture never re-emits its bind-time diagnostics to the real sink.
This is FILLER-safe (no name lookup) and avoids adding a source line to `DataItem`. Figurative ZERO is found by a
small generic `FirstDescendant<FigurativeConstantContext>` over the value-operand subtree (the `ZERO` token covers
ZERO/ZEROS/ZEROES, with or without ALL). Known minor limitation (documented): a numeric-edited picture using a
non-default CURRENCY SIGN symbol is a rare false-NEGATIVE (never a false-positive) for this advisory flag.

**Gate.** Build 0W/0E · CLI-probed (`PIC ZZ9.99 VALUE ZERO` → two 1621s [g+l]; `PIC 999 VALUE ZERO` [numeric, not
edited] and `PIC ZZ9 VALUE 5` [numeric literal] → none) · 20 FlagDirectiveTests (4 new: g on, l on, plain-numeric
negative, numeric-literal negative) · characterization 33/33 byte-exact. NEXT (Incr 1b) = j VALUE-EDITING + k
VALUE-FIG-CON-LENGTH (the subtle §13.18.63 predicates), then Incr 1c = m WRITE-END-OF-PAGE + FLAG-02 f
TERMINATE-WITH-VARYING (statement parse-arm + name lookups).

