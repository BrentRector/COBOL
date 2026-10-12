## Entry 1231 — 2026-08-08 13:58 PDT — R22: the other arm of the SR2 discrimination — a bare catalogued name without REPOSITORY now reports, not stages

`MOVE FORMATTED-CURRENT-DATE("YYYYMMDDThhmmss") TO WS-R` with no REPOSITORY paragraph compiled with
zero diagnostics and threw NotImplementedCobolFeatureException at run time. §8.4.3.2.3 SR2 is explicit
— without the REPOSITORY declaration "the word FUNCTION" is required — and COBOLNET1543 existed with
exactly that citation, but it fired only on the grammar's reservedIntrinsicArgFn alternative
(SIGN/SUM/RANDOM). Every other catalogued name parsed as a data reference, resolved to nothing, and
fell into generic unresolved-reference staging: the two-arm-dispatch shape, fifth sighting — the
declared arm (PB7's keyword-omitted binding) was built, the undeclared arm was never told to speak.

The fix sits at the ONE chokepoint every consulting site funnels through —
IntrinsicBinder.KeywordOmittedFunction — so RefExpr, FieldOperand and InspectBinder are covered by one
edit. Catalogued + unshadowed + undeclared now draws COBOLNET1543 with the same guidance the reserved
arm gives; a declared data item still wins first (SQRT as an OCCURS item stays a subscript — the new
kof_shadowed_intrinsic_name golden pins it); a BARE collision with a MinArgs>0 name stays on the
ordinary unresolved path. Negative fixtures kof-undeclared-intrinsic-args/-bare reject at
2002/2014/2023.

The probe sweep then walked into the generic arm's own hole: MOVE NO-SUCH-NAME TO X and
DISPLAY NO-SUCH-NAME — names declared NOWHERE — both compile clean, exit 0, and die at run time.
That is the staging fallthrough itself, R22's fix deliberately stopped short of it, and it is now
kb/Work/R30 (MAJOR, crashes): the wrong-stage family generalized to its root. Rule 8 order held:
the note exists before this paragraph does.

Gate: Intrinsic|Corpus|Negative 664/664 · the four kof cases green by name · characterization 33/33.

