## Entry 1782 — 2026-09-29 13:47 PDT — Train 73: FD formats, signs, FOR pairs and user words (F); prototype groups and ACTIVE-CLASS (E); national PCS, ALL slices, counters (C)

**Cluster F — PB1238 (+PB1081), PB1445, PB1075, PB1083; SPLIT (PB1042 left open).** Wave 73 group F finished
wave-70 U2's branch: it merged U2's committed work onto train 72 rather than redoing it, then landed PB1083.
- **PB1238.** No code decided which §13.4.5.2 format an FD was, so every clause bound against every organization.
  LINAGE on a relative file compiled, and `FD F ORGANIZATION IS RELATIVE` gave a sequential connector. Now
  `Binding/FileDescriptionFormats.cs` decides the format once, from the organization and whether a REPORT clause is
  written (§13.4.5.3 SR5/SR7/SR8). A clause outside its format is refused and not bound (COBOLNET2604), and the file
  control entry's clauses are refused by name. LINE SEQUENTIAL refuses BLOCK CONTAINS and RECORD CONTAINS
  (§13.4.5.3 SR4, COBOLNET2605). `FileDescriptionFormatDriftTests` keeps the table total over the grammar. SR4
  turned six of main's `LinageConformanceTests` red: they wrote RECORD CONTAINS on a LINE SEQUENTIAL file, which is
  illegal source, so the tests changed and the compiler did not.
- **PB1445.** One contiguity test, `ArithmeticFormationRules.SignAbuts`, applied to every signed literal slot by
  `LiteralScreenPass` (COBOLNET2155). The VALUE clause now takes one abutting sign only; `VALUE --5` used to reach
  Roslyn as a decrement operator (§8.3.3.3.2 rule 2).
- **PB1075.** The Format-2 key-level COLLATING SEQUENCE operand is a qualified `dataReference`, resolved to the key
  ITEM. `ChoiceIndicators.ForPhrasePair` is the one FOR ALPHANUMERIC / FOR NATIONAL reader for all five formats that
  print the pair, pinned by `ForPhrasePairDriftTests`.
- **PB1083.** One REPOSITORY-intrinsic membership (`RepositoryIntrinsicSpecifier`) applies >>COBOL-WORDS and each
  function's edition window (§12.3.8.4 GR14; Annex E.2 item 13). The declaration screen, `KeywordOmittedFunction`
  and `IsFunctionBearing` all ask it. `DeclareUserWord` is the one funnel for every §8.3.2.2 type, pinned by
  `UserWordDeclarationDriftTests`.
- **Re-probe (implementer, own build).** SQRT and E are legal data-names after UNDEFINE/SUBSTITUTE; MYPI/EULER work
  FUNCTION-less; an alphabet-name PI, a class-name E and a symbolic-character ABS are refused; CONCAT and its six
  siblings are legal at 2014.
- **Rows.** Four batches re-applied on the merged inventory: 12 rows, GAP 866 → 858. Notes landed: PB1238, PB1081,
  PB1445, PB1075 and PB1083. Codes COBOLNET2604/2605.
- **Leads.** Parameter-names of a parameterized class escape the funnel (filed as PB1744). The merge-union trap
  (duplicate manifest entries make the corpus loader throw for every case) was fixed on the branch.

**Cluster E — PB1115, PB1112 (activation half); SPLIT (PB480, PB1113, PB1116 not started).**
- **PB1115.** A prototype's GROUP formal or returning item now matches its definition by its subordinate entries
  (§13.7.3 SR2, read through §8.5.3.1's essential characteristics). `PrototypeSignatures.SameDescription` combines
  the pair-mode `DescriptionMismatch`, group-only-matches-group and `StrongTypeModel.SameElementaryLayout`. That
  last predicate was extracted from `EquivalentTypeDeclarations`, so type equivalence and signature equality read
  one predicate. Every same-signature asker shares it.
- **PB1112.** INVOKE describes the invocation once (`invokedWith`, the four cases of §14.8.3.3 rule 2 b)). An
  ACTIVE-CLASS formal conforms by §14.8.2.3.2 rule 4 or by §14.8.3.3's two alternatives, SELF and NULL senders
  included. An ACTIVE-CLASS returning item sends `ReturningSender`'s description.
- **Did not hold on today's tree.** Legs 2–5 of PB1112 were already fixed by PB1166; they were re-verdicted.
- **Rows.** Two batches, 6 rows. PB1115 landed. PB1112 stays open on GR-9.3.8.2.3-2 (the override pair) and
  GR-9.3.6-L3.3 (the universal leg).
- **Findings filed on PB480.** The universal descriptor answers §9.3.6 rule 3 e) backwards for groups. An
  ACTIVE-CLASS formal through a universal receiver is keyed statically, where §9.3.6 3 d) states a run-time
  condition.

**Cluster C — PB1185, PB1655, PB1153, PB1199, PB1318; DONE.**
- **PB1185.** STRING and UNSTRING record the statement's character category once, and
  `OperandText.AsString(characterCategory:)` hands it to `FigurativeConstants.Fill`. A national statement's
  HIGH-/LOW-VALUE therefore comes from the national PCS (§8.3.3.6.4 GR6, §14.9.48.4 GR7, §14.9.43.4 GR2). The probe
  printed `[ABCDE][     ]` before and `[AB][DE]` after. INSPECT is PB1414's; its seam is noted there.
- **PB1655.** `MOVE ALL lit TO X(s:l)` now repeats the literal over the slice through one seed fill,
  `SpliceInto(repeat: true)` (§8.3.3.6.4 GR2). The probe printed `[------*  ]` before and `[------***]` after.
- **Counters (PB1153/PB1199/PB1318).** LINAGE-, LINE- and PAGE-COUNTER are PIC 9(d) USAGE DISPLAY integer items in
  every consumer, from one `AlgebraicRanges.CounterCapacity`. Before, DISPLAY, MOVE and STRING of them threw
  NotImplementedCobolFeatureException at run time, and PERFORM TIMES, the relation and LENGTH refused them.
- **Rows.** Four batches: 3 rows closed plus witness rows. All five notes landed.
- **Open question.** The 18-digit image is a documented determination; the GnuCOBOL survey of its width is PB1746.

**The train.** The three branches (base 86cdf4456, content-equal to main's train-72 head cc538bc0b) were applied as
patches in manifest order.
- **Merge.** The only conflicts were whole list elements in the 2002 and negative manifests, and both sides were
  kept (counts 565 and 1772, each the sum of the clusters' additions). The inventory was never merged as JSON: the
  ten verdict batches were re-applied in order, and each cluster's rows were proved equal to its branch's rows.
  Every file of every branch was proved present at HEAD.
- **Gate.** `build-local -Mode lander` run 20260929T204325Z-6bbcf3 was GREEN: Conformance 9,350/9,350, Unit
  29,746/29,746, Characterization 35/35. The legacy Integration assembly passed 503 (1 skipped).
- **Two earlier gates were RED on the same tree, one Conformance case each, a different case each time.**
  `DivideRemainder_Basic`: a program run exited non-zero with an empty detail. `SearchIndexRangeSpecTests…(2014)`:
  the .NET host got `CreateFileW(prog.runtimeconfig.json) failed with error 5`. Both classes passed in isolation. The
  machine was loaded with wave-73 implementer gates. This is filed as PB1747 (the harness drops the exit code, and
  cannot tell a host start failure from a program failure) and attributed to no cluster.
- **Checks.** semgrep verify PASS (raw-diagnostic-code-literal 301 → 297, nothing up). The review pass over the
  train's 47 source and test files found no correctness finding, so no cluster was dropped. `work.py check` passed.
- **GAP 866 → 849.** New notes: PB1744 (parameter-name screen), PB1745 (CRLF shell scripts survive a forward merge
  in an agent worktree; all three implementers hit it), PB1746 (the counter width survey) and PB1747 (the harness
  run failures). Codes COBOLNET2606/2607, 2638–2641, 2646–2653 were returned unused.

