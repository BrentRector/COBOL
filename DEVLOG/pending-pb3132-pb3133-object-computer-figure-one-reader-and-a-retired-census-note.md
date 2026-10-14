## PB3133: the OBJECT-COMPUTER paragraph's cardinality and order have ONE reader, the clause table; PB3132 retired (its repro is already refused)

Wave 1072, group D (the data binder: PB3132, PB3133).

**PB3132, retired.** The note said a METHOD-ID spelling its CLASS-ID that also uses the class-name in its body is not
refused by the one-type-per-word census (ISO §8.3.2.2). Re-probed on this build at `--std 2002` it is refused,
COBOLNET2692, at the METHOD-ID, with or without the body's `INVOKE`: the declaration funnel announces both words, and a
class-name use can only resolve to a declaration (a REPOSITORY entry or the containing class). The note's one repro was
never written down, so nothing reproduced. `negative/pb3132-method-id-spells-its-class-id-and-invokes-it` now pins the
shape (it had no golden). The real residue is a different mechanism, reported as a lead: a name a containing element's
SPECIAL-NAMES passes down is inherited, never declared, so a contained element may declare the same word as another type
(PB990, §8.4.6.1) and then use both (`outer CLASS FOO`, `inner 01 FOO`, `IF X IS FOO` binds the class while `DISPLAY FOO`
binds the data-name). §8.4.6.1 makes the element's own item win, which would refuse `IS FOO`: a lookup-shadowing rule
across the SPECIAL-NAMES registries, not taken here.

**PB3133, landed.** At `--std 85`, `OBJECT-COMPUTER. XYZ MEMORY SIZE 10 WORDS MEMORY SIZE 20 WORDS.` and
`SEGMENT-LIMIT IS 10` before `MEMORY SIZE 20 WORDS` compiled clean. The note put the design question: the clause table
or the binder owns the §5.2.6.4 counts. Answer: the table. The binder counted PROGRAM COLLATING SEQUENCE and CHARACTER
CLASSIFICATION itself (COBOLNET1652) while `ClauseCardinalityDriftTests` exempted the list, so the 1985 figure had no
reader. The list is now a `ClauseCardinalities` row on PB2532's `Earlier`/`At(year)` chain (merged in from that branch
while it was queued): from 2002 the §12.3.6.2 figure, rendered from folio 285, encloses the two clauses in choice
indicators (each once, any order, COBOLNET2423); below 2002 the COBOL-85 figure ranks MEMORY SIZE, PROGRAM COLLATING
SEQUENCE, SEGMENT-LIMIT once each in that sequence (COBOLNET2423 repeat, COBOLNET2987 out of order). The 85 figure is
derived, not quoted (no 1985 text in the repository): IBM Enterprise COBOL prints it, GnuCOBOL takes any order and
number so it is no arm of the precedence, and the seven NIST CCVS-85 programs that write the clauses write one each.
The binder's counts and COBOLNET1652 are deleted; the code stays retired.

**Found and fixed in the files read (I9).** `UnrepeatedElements.AtMostOnce` told every list it "encloses it in its own
bracket with no ellipsis", false for a choice-indicator list; the message and COBOLNET2423's descriptor now name
§5.2.6.2/§5.2.7 and §5.2.6.4.

**Goldens.** `85/pb3133_object_computer_clauses_in_sequence_85`; `negative/pb3133-object-computer-clause-repeated-85`,
`negative/pb3133-object-computer-clause-out-of-order-85`, `negative/pb3132-method-id-spells-its-class-id-and-invokes-it`;
`pb78-object-computer-duplicate-clause` and `pb64t5-classification-twice` now expect COBOLNET2423. Unit:
`ClauseCardinalityDriftTests.TheObjectComputerRow_ScreensTheCobol85FigureBelow2002`. Rows: none claimed by either note.

**Self-review.** Architecture: one reader per rule (the binder lost its copy). Code: the row cites the figure it
implements; the 2023 figure's `Sequence` is null because §5.2.6.4 frees the order. Performance: one dictionary lookup per
clause, as before. Duplication: none left. The gate's verdict is recorded in the branch's report.
