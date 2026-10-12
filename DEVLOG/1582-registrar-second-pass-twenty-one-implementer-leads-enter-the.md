## Entry 1582 — 2026-09-09 15:17 PDT — Registrar (second pass): twenty-one implementer leads enter the register — PB810-PB824 filed, six notes extended, PB389's five residue rows transferred, and one lead measured out

Twenty-one defect leads from today's eleven implementer reports had been written as note-ready paragraphs and
filed nowhere. They are now `kb/Work/` notes: **fifteen new (PB810-PB824, every allocated id used, in order)**
and **six appends** to notes that already owned the mechanism. Every lead was re-probed on this worktree's own
`dotnet build CobolSharp.sln -c Debug` (exit 0) at `d3ba5795`, and every citation was re-run through
`cite.py --check` rather than inherited — which corrected three of them.

**New notes.** **PB810** (adjudication) a sequential READ whose I-O status is set to '46' takes NEITHER branch,
though §14.9.30.4 GR21 sends it to GR24 whose c) transfers control to the AT END imperative — measured
`E2-AFTER ES=46`; the compiler's reading is written down as a citation to §9.1.14, a rule about the invalid key
condition, and GR24 a)'s own '10' contradicts GR21's '46', so the standard has to be read before the code is
touched. **PB811** `audit_underlining.py` HALTS on every run (page anchors the transcription no longer carries),
so the one mechanical gate over §5.2.2 underlining has measured nothing since — it exits 1, so no false green,
but nothing runs it and `audit_figure_structure.py:7` still advertises its last result. **PB812** the
COBOLNET1572 prohibited region is a NUMERIC SPAN, so a MERGE inside an INVERTED sort-procedure range escapes it
(`mergeinv.cob` exit 0, `mergepos.cob` diagnosed — the positive control is what makes it evidence).
**PB813** §12.3.3 SR3 unenforced: a REPOSITORY paragraph inside an OBJECT **or a FACTORY** definition compiles
clean and is inert (the factory arm is new here; the report measured only the instance one). **PB814** §14.2.2
SR9's interface-name RAISING is refused COBOLNET0858 by a diagnostic that cites SR7-SR9 while refusing SR9.
**PB815** `raisingClause : RAISING cobolWord+` carries one axis where the PRINTED §14.2.1 diagram (PDF page 558,
measured) prints three. **PB816** SET Format 10 has no STRONG screen, so §14.9.39.3 SR24 is unenforced.
**PB817** `USAGE FUNCTION-POINTER` and `USAGE PROGRAM-POINTER TO prototype` are both COBOLNET0899, so SET
Formats 8 and 9 have no subject. **PB818** INVOKE's BY CONTENT screen still asks `IsImageCapable`. **PB819**
§13.18.60.3 SR14's ELEMENTARY arm is unreachable for a loud-staged usage. **PB820** five rendered C# names still
ride three bound records, and the semgrep invariant built to forbid exactly that matches only `*Text|*Rendered|
*Csharp|*Fragment`, so it has never seen `CsName` (its baseline of 3 is three literal-text nodes). **PB821**
(analysis) `PIC LL EDITING "L" IS ":"` is a valid character-string with no category under GR5-GR13. **PB822**
`PicInfo.ImageSignKind` is deleted from `src` and three docs still rest determinations on it. **PB823**
`IF S (1:4) IS NUMERIC` answers TRUE for the over-punched image `123M` — a WRONG ANSWER, silent, where
§8.4.3.3.4 GR6 c) makes the ref-mod operand alphanumeric and §8.8.4.4.4's n)2 all-digits test governs; the
RefMod exclusion was written on the `windowedNumeric` arm and the operand falls into the SIGNED arm below it.
**PB824** (analysis) 50 "RESOLVED"/"holds structurally" claims in the design docs, 6 with a quoted fragment and
5 naming a clause — roughly 44 load-bearing claims no citation audit can see, one of which decided PB366's
wrong answer.

**Appends, no id spent.** **PB376** already owned the shared-temp-dir race; the tree-wide sweep it asked for was
run — 30 `GetTempPath()` sites, 28 already per-process, and **exactly one sibling**: `gen-vault-reference.ps1:37`
has the identical shape and its own drift test. **PB344** already owned the ungated INDEXED `READ PREVIOUS`
(its Instance 1); what is new is that PB343's landing MOVED the code site to
`IndexedConnector.cs:309-310`, whose comment names the edition it hard-codes, and that `--std 2002` measures
identical to 2014 and 2023. **PB496** owns the six §13.18.60 rows whose COBOLNET0899 evidence PB389 falsifies —
the re-adjudication and the dead descriptor's retirement are owed there after train 25. **PB382** already owned
the reserved-word-in-a-name-slot funnel; `01 CF PIC 999CRCR.` and `01 CH ...` are a fourth population, and the
report's clause numbers were WRONG (§8.10 is context-sensitive words, §8.4 is References — the pair is §8.9 and
§8.3.2.2). **PB258** takes an eighth row of its shape: `SR-13.18.40.3-23` read CONFORMS over an unenforced rule,
its verdict earned on the floating-point sub-case only. **PB530** records that its SR25/SR26/SR27 become
closable on PB528's `PD05`/`PD06`/`PD07`, leaving SR29 alone.

**PB389's residue is transferred.** Its five residue rows — `SR-14.9.14.3-5`, `SR-14.9.18.3-4`,
`SR-14.9.39.3-24`, `-20`, `-22` — are removed from its `inventory_rows` and claimed by PB815, PB816 and PB817,
with a table in the note naming which took which. ⛔ **Its `status` is deliberately still `open`**: the fix is
not on `main`, and all twelve remaining rows are still defective there, so flipping it now would leave twelve
rows with no live note and turn `DefectiveRowCoverageDriftTests` red. Rule 8 puts the flip in the commit that
lands the fix — train 25's — and the transfer is what lets that commit make it without orphaning a row.

**What did NOT reproduce.** One of the twenty-one — the PB393 report's own LEAD, `CallEmitter.HoistPlace`
declining to hoist a DECORATED place with a variable subscript. The `default:` arm really is a no-op and
`HasVariableIndex` really does recurse through `PlaceDecorator`, but the CALL argument path does not build a
decorated place: `CALL "S" USING IX E (IX)` over a mixed-usage group element emits
`var __ci0 = CobolTable.Occ(IX);` and reads `CobolTable.At(T.E, __ci0).AsImage()` — the image conversion is
applied by the argument RENDERER, so the Place is a plain `MemberPlace` and the `MemberPlace when
PathHasVariableIndex` case hoists it. No note filed; the measurement is in the registrar's report. Also
measured while probing PB823: `IF S (ALL) IS NUMERIC` dies at run time with `NotImplementedCobolFeatureException`,
so the `TableAllPlace` half of that exclusion has no live subject and the REF-MOD half is the whole defect.

**Three premise corrections, each caught by re-running rather than inheriting.** (1) PB389's report records
`PROCEDURE DIVISION RAISING FACTORY OF <class>` as `COBOL0307 "unexpected 'OF'"`; on `main` it is COBOLNET0901
on FACTORY, at the FACTORY token — same cause, and a fixer grepping the reported text finds nothing. (2)
`SR-14.9.39.3-24`'s recorded evidence says its subject "cannot be declared"; PB153 landed `POINTER TO`, and
§13.18.60.3 SR18 makes the TYPEDEF spelling the conforming one — the subject declares, runs, and the screen is
simply absent (measured, with COBOLNET0881/COBOLNET0869 as the positive control). (3) PB528's report cites §8.10
and §8.4.1 for reserved and user-defined words; both fail `--check`.

**Gate, at the head of this change set:** unit inventory legs `Passed! - Failed: 0, Passed: 47`;
`audit_doc_citations.py --check` 382 checked, **0 MISFILED**; `audit_code_citations.py --check` 3755 files,
**0 findings**; `work.py check` **857 items, all well-formed**; `gen_conformance_notes.py` 4348 items,
**GAP 2713 — unchanged** (this pass changed no verdict: a claim is not a verdict, and every verdict a lead
touches is left for the landing that earns it). `work.py check` earned its keep here — it rejected PB820 for
having no harm flag set, which would have made it invisible to `work.py next` for exactly the reason
`feedback_measure_the_selectors_complement` records.

