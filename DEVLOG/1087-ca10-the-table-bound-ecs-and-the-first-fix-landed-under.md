## Entry 1087 — 2026-07-28 16:44 PDT — CA10: the table-bound ECs, and the first fix landed under mandatory citation validation

Step 7. Two conditions, and a signature change that had never been threaded.

**⚠ THE FINDING'S CITATIONS WERE BOTH WRONG, and that is why this entry exists in the shape it does.** It cites
§8.4.1.2 GR2 for EC-BOUND-SUBSCRIPT and §13.18.38 for EC-BOUND-ODO. The real rules are **§8.4.2.3.4 GR2** and
**§13.18.38.4 GR7**. The quoted TEXT was right in both cases — which is exactly why it slipped through
unchallenged into the finding, and would have gone into code comments, two goldens and this log. Caught only
because the owner required citations to be validated against the standard, which produced `scripts/spec/cite.py`
and CLAUDE.md rule 1's new sentence. Every citation below was run through `--check` before it was written.

**The rules.** §8.4.2.3.4 GR2 — "If the value of the subscript is not a positive integer or is less than one or
is greater than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to
exist." §13.18.38.4 GR7 — the DEPENDING value "shall fall within the bounds from integer-1 through integer-2",
and its closing sentence, "The content of a data item whose occurrence number exceeds the value of the data item
referenced by data-name-1 is undefined", is what makes the checking-OFF scratch read and clamp conforming
implementor choices rather than a shortcut.

**The ODO fix needed a signature change the runtime never had.** `OdoExtent` clamped with a floor hardcoded to
0, so it could not see integer-1 at all and a BELOW-MINIMUM DEPENDING value was silent at every checking state.
integer-1 already existed on `OccursSpec.Min`; it was simply never carried. Threaded
`OccursSpec.Min → OdoGroupPlace.MinOccurs → RuntimeApi.TableOdoExtent → CobolTable.OdoExtent`. This is the first
change in the batch to alter a shared emit signature, and it is covered by its own golden rather than riding on
the subscript one — a threaded parameter with no test is not threaded, it is decoration.

**Both goldens registered same commit**, each with a spec-derived expected value (the EC name from its rule, the
31-character width from §15.33 — not copied from a run):
  · `2023/ec_bound_subscript` — subscript 5 into `OCCURS 3`. Without the fix: SILENT, only "AFTER" printed.
  · `2023/ec_bound_odo_below_min` — N = 1 against `OCCURS 2 TO 5`. Passes only because integer-1 is now carried;
    with the old hardcoded floor the test `count < min` would read `1 < 0` and never fire.

**Ambient, and the comment says why:** a subscripted reference renders inline through `CobolTable.At` and a
group extent through `CobolTable.OdoExtent`, so neither is a distinguishable node kind at statement level — the
same reason EC-BOUND-REF-MOD and the CA9 pointer trio are ambient.

**Gate:** characterization 33/33 · Conformance CorpusRunner+Occurs+Odo+ExceptionCondition 450/450 (474/474 on
the wider Table filter) · Unit Odo+Place+Table+flags-drift 30/30. ⚠ The FULL Unit suite timed out at 2 min and
was run as targeted filters instead; it is not evidence of anything broader and the comprehensive suite still
owes a clean run at the pre-merge gate.

