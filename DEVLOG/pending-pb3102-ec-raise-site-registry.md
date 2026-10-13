## PB3102: the exception-names a statement can raise are one structure, and a drift test ties every catalogued name to a raise site

The note measured that `EcWrap` took a statement's enabled exception-names from three unrelated lists (the `QueryFor`
node-kind switch, the operand walker and an ambient tail) and that nothing tied the catalog's level-3 names to anything
that raises them. Re-probed on this build: all of it reproduced, and the classification the new test forces found three
real defects of the shape the note predicted (a raise site exists, no statement ever enables it).

**The structure.** `EcBinder.RaiseDeclarations.cs` holds the three sources as tables. `NodeKindRaises` has one row per
statement shape, `IoRaise` declares the EC-I-O family once per file `IoFilesOf` names, `OperandRaises` declares the program
address operand and now also the four intrinsic families (EC-ARGUMENT-FUNCTION, EC-ORDER-NOT-SUPPORTED, EC-DATA-CONVERSION,
EC-LOCALE-INVALID) over `PartHasIntrinsic`, and `AmbientRaises` has one row per ambient name, scoped only by activated-element
kind and the locale CHARACTER CLASSIFICATION. `DeclaredRaisableNames` is the one enumeration, and `EcWrap` filters it by the
TurnState at the statement's line. `QueryFor`, `ContainsIntrinsic`, `DirectIntrinsic` and the nested-statement recursion are
deleted: a nested statement is its own EC region (kb/Work PB441), so the recursion wrapped an enclosing statement a second
time. The `docs/COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md` D13 section describes the as-built shape.

**The registry.** `EcRaiseSiteRegistryDriftTests` reads the catalog's 133 level-3 names. Each must be declared by an EcBinder
table and have a raise site (a string literal that is the name, in the runtime or the code generator, not one it is merely
compared with; the I-O status map for EC-I-O), or be on `DeclinedRaiseSites` with a CONFORMANCE.md anchor the test reads and
requires to quote the name, or be one of the two names RAISING LAST EXCEPTION stages at run time and the activating
statement's `EcCheckingProfile` enables. A second test asks `IoFilesOf` for every bound statement that carries a `FileModel`.
It was seen to fail by removing EC-OO-RESOURCE from the INVOKE row. The note's own proof (delete the `BoundSetEntry`
EC-PROGRAM-NOT-FOUND row) cannot work on a per-name registry, because the CALL row, the operand row and `ProgramNames` also
declare that name; D13 says so.

**Three defects the classification found, fixed.**
- EC-OO-RESOURCE (ISO 1989:2023 section 16.2.1.2 GR2): PB1524 landed the runtime raise and its checking flag, but no
  statement ever set the flag, so `>>TURN EC-OO-RESOURCE CHECKING ON` reached nothing. The INVOKE row names it now.
  `OoResourceStatementGuardTests` reads the emitted C#: the main build emits no guard, this one does.
- EC-REPORT-NOT-TERMINATED (section 14.9.6.4 GR5): the CLOSE decided the check in `SequentialIoBinder` and carried no
  statement context, so with `WITH LOCATION` EXCEPTION-STATEMENT and EXCEPTION-LOCATION answered spaces (sections 15.32.3 and
  15.30.3). `BoundClose` is a row and `ReportNotTerminatedCheck` is deleted; golden `pb3102_report_not_terminated_with_location`.
- EC-PROGRAM-IMP, the one implementor name the implementation sets (DOC-A.1-100), was in `CobolCallException.CarriedNames`
  but in no CALL statement's enabled set, so the guard's filter could never admit it.

EC-FLOW-GLOBAL-EXIT is never set (Table 13 names it, no general rule sets it): new CONFORMANCE.md determination D-GLOBAL-EXIT.
Golden `pb3102_intrinsic_names_ride_every_statement` pins that a function reference raises in STRING, EVALUATE, SEARCH and a
subscript, and once, not twice, from an IF's nested statement. Sibling sweep: the other places that ask `Turn.Enabled` outside
EcBinder (Search, Continue After, Perform Varying, EC-RANGE-INVALID, EC-EXTERNAL, EC-BOUND-ODO, ref-mod) are all declared;
`SequentialIoBinder` was the only one that was not. Inventory batch `pb3102-ec-raise-declarations` re-sited eight
code-location witnesses and added two; the GAP count stays 103. PB1037 and PB1762 are landed with it.

Self-review (architecture, full code, performance, duplication): the enumeration allocates an iterator per statement as the
closure did; the tables are static. The ambient rows replace 40 `if (Enabled) add` pairs with data.

Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — Conformance 4,900/11,496 · Unit 32,581/33,468 · Characterization 6/36 cases ran` (run `20261013T032042Z-6ef3fc`; the full Conformance project, 11,496 of 11,496, was also run locally and passed). The Linux gate is the landing's. The arch-oracle baseline must be re-recorded at the landing (L11): the emitted C# of programs that enable EC-OO-RESOURCE, EC-REPORT-NOT-TERMINATED or EC-PROGRAM-IMP (directly, or through EC-ALL or their level-2 names) gains a guard; that is the expected difference class, not measured here.
