## PB2762 discharged: the UTS #10 S2.1 contraction walk is already written once

kb/Work PB2762 (wave 1096 F, the PB1754 cluster) was an R2 duplication finding read at 8be230068: the run-time
`CollationElementIterator` and the CLDR builder's `CloseOverComposites` each wrote the S2.1 walk (longest contiguous
contraction, then the unblocked discontiguous non-starters), and the builder's copy matched only the tailored keys.
Re-probed on 59208d8fb, the builder's copy is gone. The PB2765 landing (090bf3909, 2026-10-08) moved canonical
closure into one step, `CollationTable.Rebuild`, for every front end, deleted `CloseOverComposites` and the builder's
`FromCodePoints`, and made the closure walk each touched composite's decomposition with `CollationTable.Walk`, which
is `CollationElementIterator` over the provisional table. `git log -S CloseOverComposites -- src/` ends at 090bf3909,
and the matcher's members appear only in CollationElementIterator.cs. The drift test the note's target asked for
already exists in a stronger form (`CanonicalClosureDriftTests`: every decomposable code point collates equal to its
NFD in the root, nine CLDR tailorings, a `.tailor` file and two `.tailor`-over-CLDR layers). The note is RETIRED
unlanded, superseded by PB2765, and closes no inventory row (it claimed none).

The sibling sweep found one reader of a string's elements that is still not the S2.1 walk: the builder's `Lookup` (a
reset's text, a `/` extension, a `|` prefix) uses `CollationTable.GetElements(ReadOnlySpan<int>)`, an exact whole-key
match or else per-code-point concatenation, where ICU's CollationBuilder walks the NFD string over the mappings built
so far. A temporary probe over all 160 CLDR 48 tailorings counted 2,377 looked-up strings that hold an earlier
tailored contraction inside them (bn, kn and sa traditional, my, dz, bo, root emoji), two whose root contraction
the walk takes and the whole-key lookup misses (bal), and 48 that are not in NFD (ar, bn and kn traditional).
Changing it changes tables, so it is a defect lead for the fix lane under the PB2119 wave contract, recorded in the
wave report rather than fixed here. No source file changed on this branch.
