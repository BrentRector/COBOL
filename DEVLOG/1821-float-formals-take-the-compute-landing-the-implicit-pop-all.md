## Entry 1821 — 2026-09-30 17:20 PDT — Float formals take the COMPUTE landing, the implicit POP ALL reaches SOURCE FORMAT, and the SPECIAL-NAMES ordinal has one reader

Three fix-lane bugs (PB1114, PB1066, PB1091), each re-probed on its own build first (MANDATORY-PRACTICES I3). Two of the three notes were
partly stale, which changed the work: PB1066's DEFINE half and PB1091's ordinal-overflow half had landed earlier without the notes or the
inventory rows following. Entry number 1821 is this change set's; the lander may renumber.

**PB1114 — a fixed-point argument BY CONTENT to a floating-point formal of a NESTED activation arrived as 0.** The note blamed a
"misclassified Text crossing"; the callee's float formal was in fact crossed Native. Both halves of the §14.2.3 GR9/GR10 COMPUTE
("if the formal parameter is numeric", §14.8.2.3.3 2) a)) spoke fixed-point only: the activator's guard was `IsClassNumericFixedPoint`,
and the callee's converting views landed the argument through the float formal's description, whose `Digits = 0` reduces every value
modulo 10^0. The fix is one float landing, `CobolArgAdapt.LandFloat`, over `FloatResultant` — THE one transfer into a float resultant
identifier (implied TRUNCATION §14.7.4.3 rule 10, one rounding for a scaled sender, the §14.7.5 case-3 raise under EC-SIZE checking via
the new `FloatResultant.From…OrRaise`) — chosen by one predicate `IsFloatFormal` at `LandForFormal`, `NumValue`, `TextValue` (which
used a private round-to-nearest `ArgDouble`, now deleted) and the `Num` converting view. Siblings fixed in the same change: the BY VALUE
float formal of a program or function (COBOLNET0899, "value-copy carrier not yet implemented", removed; the catalog text now names only
the METHOD arm PB1051 owns), a numeric literal or an arithmetic expression into a float formal (COBOLNET1688 "documented refinement"),
and the whole INVOKE channel (the binder's "fixed-point⇄float CONTENT conversion is not carried" residue refusal removed;
`OoEmitter.IsFloatLanding` lands through the same `FloatResultant`). Goldens `2023/pb1114_call_content_float_formal` (BY CONTENT/BY
VALUE, both formal carriers, the truncation case: 0.1 into FLOAT-SHORT is the binary32 BELOW 0.1) and `2002/pb1114_invoke_content_float_formal`;
`CallAbiFloatLandingTests`. Row GR-14.8.2.3.3-L2.2 stays open: it is shared with PB1113's index-formal, ANY LENGTH and national legs.

**PB1066 — the implicit POP ALL did not reach the reference format.** The DEFINE probe already printed `HANDLER` / `ZZ-POPPED`. The
SOURCE FORMAT half needed a different keying: the format is read per physical line before every other stage, so the ops are mapped through
the source-line map to the file and physical line each was written at (`ImplicitFormatOps.Place`) and applied by the normalizer's walker
as boundaries between lines — the PUSH after the line that ends imperative-statement-1, the POP BEFORE the END-PERFORM line, so the
restored format governs END-PERFORM itself (§14.9.28.4 GR14 "Immediately preceding the END PERFORM phrase", §7.3.24.3 1); recorded in
CONFORMANCE.md §3). The design estimate "bring normalization into the fixed point" was not enough: the unrestored format usually makes
the text after the handler unparsable, and a failed pass was final, so the op could never be placed. A pass that does not parse now reads
its ops off the parser's recovered tree (the END-PERFORM phrase located in the physical text when the recovered construct lacks it) and
re-runs speculatively, bounded by the directives in the text; a run that never parses keeps the FIRST failure's own diagnostics.
Goldens `2023/pb1066_gr14_implicit_pop_all_source_format` (+ `_free`, the END-PERFORM at column 1 decision) and the below-2023 negative;
`ExceptionPerformSourceFormatScopeTests`. Row GR-14.9.28.4-14: CONFORMS. The self-review found a sibling in the op ordering that
predates this work and the new format path made observable: ops were ordered by token index, so an inner PERFORM whose END-PERFORM
ends the outer imperative-statement-1 put the outer PUSH before the inner POP, the stack paired them crosswise and the inner POP
was dropped (a handler's DEFINE leaked the same way). They are now keyed by the boundary between tokens (PUSH after, POP before),
with a unit test. Two corpus goldens first failed under `--source-format auto` (the corpus runner's mode): a fixed-form file whose
comment lines run past column 72 is classified free by the detector, which restores FREE — comments are kept within column 72.

**PB1091 — one decoder for the SPECIAL-NAMES ordinal.** The overflow symptoms were already gone (PB1557's sibling wave). What was left:
two readers (a private saturating `OrdinalValue` and `IntegerOperandRules.HostValue`) — the literal phrases now read through
`HostValue(string)`, `OrdinalValue` is deleted, `SpecialNamesOrdinalReaderDriftTests` holds the paragraph to it — and row L7.4: the
one-character rule is asked of the operand AS WRITTEN, so a numeric ordinal is no national literal; a numeric ordinal naming a supplementary
character under THROUGH has no position in the native-set run GR12 expands and is refused under GR12's own message (implementor-defined,
DOC-A.1-188), never under c4. A found sibling fixed with it: an out-of-range symbolic ordinal left its name undefined, so its first use drew a
false "not defined" — it is now registered with a recovery value. Nine rows re-verdicted CONFORMS (`record_verdicts.py`; GAP 731 → 721 with
PB1066's).

Gates: PB1114 alone (cebb7cad0) was GREEN on Windows (9,556/29,939/35) and on Linux before the other two were built on it; the combined head
was gated again on both (the counts are in the implementer report, not in this entry, so that the entry is part of the gated commit).

