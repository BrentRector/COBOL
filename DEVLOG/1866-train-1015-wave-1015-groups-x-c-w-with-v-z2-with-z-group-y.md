## Entry 1866 — 2026-10-04 14:10 PDT — Train 1015: wave 1015 groups X, C, W (with V), Z2 (with Z); group Y dropped at review; GAP 336 → 289

**Group X — PB1407 (half), PB1062, PB665, PB816: the ADDRESS OF operand rules.** `PtrBinder.PtrBindAddressOf` is the
one binder of every data-address-identifier (SET sender, CALL and INVOKE argument, relation operand), and it checked
only that the operand's record was cell-backed, so a pointer could be taken to an object reference, into a
strongly-typed record, into a CONSTANT RECORD (a BASED view could then write the constant), to a dynamic-length item and
into an object's own working storage. All four notes reproduced. A new `AddressOfOperandScreen`, asked once by
`PtrBindAddressOf`, gives each §8.4.3.11.3 syntax rule one arm on the RESOLVED operand: SR1's second sentence
(COBOLNET2784, through `DataBinder.OoIsObjectData`, the predicate CALL and INVOKE already use), SR2, SR3 and SR6
(COBOLNET2785), SR4 (COBOLNET2786, through `ParameterConformance.BitStartOf`, extracted from the CALL/INVOKE screen).
A reference-modified identifier-1, which SR4 a) admits, now binds: `ReferenceResolver.ReadScreenedRefMod` is the one
ref-mod admission, and `PtrEmitter` adds `CobolString.RefModStartOffset`, the same range test a read spends. PB665:
`PtrBinder.ScreenBasedReceiverRestriction` asks both data-name-1 sentences of §14.9.39.3 SR19 per receiver. PB816:
SET UP/DOWN BY on a pointer restricted to a STRONG type is refused (SR24). The `StrongTypeModel.AddressOfRestriction`
comment said the opposite of SR2 and was corrected, as was DESIGN-data-model. Three goldens took ADDRESS OF object data
or of a constant record and were made conforming (2002/oo_based_class_data and pb1216_method_pointer_lifetime now
ALLOCATE; 2023/pb1233_constant_record_options_initialize lost its pointer lane). Goldens: 2002/pb1407_address_of_reference_modified,
2002/pb665_restricted_pointer_conforming and seven negatives; drift test `AddressOfOperandScreenDriftTests`. Rows
SR-8.4.3.11.3-1/2/3/4/6, FMT-8.4.3.11.2, SR-8.4.3.1.3-10 and SR-14.9.39.3-24 CONFORMS. PB1407 stays half on
SR-8.4.3.1.3-4 (`DISPLAY X (:2)` reaches only the internal-error net COBOLNET2362). Leads filed as PB1969 (an ODO
group's ref-mod range checked against its maximum extent), PB1970 (possibly dead constant-record cell seed lanes) and
PB1971 (no golden for an object or factory FILE SECTION record as the operand).

**Group C — PB351, PB1146, PB937 (closed by measurement); PB1197 and PB1136 blocked.** Every conditional phrase spells
its operand `statementBlock`, so an IF with no END-IF as a READ AT END operand, a SEARCH WHEN operand or an EVALUATE WHEN
operand compiled, and the procedure division parsed the union of §14.2.1's two formats. One pre-bind
`Validation/ProcedureFormatPass` (beside IntegerOperandPass) now refuses a conditional statement as an
imperative-statement operand (COBOLNET2796; §14.5.1, IF's statement-n exempt by §14.9.19.3 SR1) using
`Binding/ConditionalStatements.IsConditional`, which reads the phrase and Table 12's terminator off the parse rather
than a verb list, and refuses a sentence or paragraph outside every section in a division with sections or DECLARATIVES,
and a second DECLARATIVES (COBOLNET2797; §14.2.1, §14.4.1). Drift tests `Table12StatementNameDriftTests` and
`RequiredImperativeStatementDriftTests`. Nine repository sources that used the refused shapes were made conforming
(output unchanged; four differential goldens re-baked). PB937's second arm was measured on PDF page 761 (every word of
SET LAST EXCEPTION … OFF is required; the grammar is right). PB1197/PB1136's `::` receivers wait on PB1551 then PB1425.
Rows FMT-14.9.34.2, FMT-14.9.37.2 and FMT-14.2.1 CONFORMS.

**Group W — PB322, PB165 (half), PB1422, PB1531, PB1756 (retired), carrying group V (PB1955, PB1957 row 40).** V:
CI's guard read every divergent corpus row as a legacy divergence, so PB322's TERMINATES rows (NC401M, ST146A) scored
NO-VERDICT and REGRESSION (CI run 37217227958, the red that dropped PB322 from train 1013). `guard-population.sh` is now
the one shell reader of a row's kind, `guard-verdict.sh#guard_termination_verdict` scores a declared termination in both
runners, the audit takes its expectations from the reader, `CorpusManifestTests` pins the shell grammar to
`CorpusRow.TerminatesMarkerPattern`, and the guard job is linux-gate's fourth default leg (PB1957 row 40). W re-landed
PB322 on V: the implementor-default sharing mode is a function of the open mode (OPEN INPUT is READ ONLY, every other
mode NO OTHER; GnuCOBOL's posture, DOC-A.1-77/131), so every arbitration is a plain Table 19 lookup and the posture
widening and handle rebuild that existed only for an undetermined default are deleted. PB165: `BoundaryItem` gains a
`BoundaryClass`, each activated unit that checks EC-PROGRAM-ARG-MISMATCH registers its formals, and
`ProgramTable.CallProgram` compares every argument with its formal on the dynamic lane (§14.8.2.3.2/§14.8.2.3.3 rule 1,
§14.8.2.2), with the RETURNING group class fixed as a sibling; the program-prototype lane without a compile-time
signature stays open on PB165. PB1422: a class whose type initializer fails is EC-OO-METHOD (§14.9.23.4 GR7 b,
DOC-A.1-102), and `RunUnit.FactoryObject` unwraps the `TargetInvocationException` that `new T()` wraps a factory
constructor's condition in. PB1531: DOC-A.1-99/100 recorded, `EcImplementorNamesDocumentationDriftTests`. PB1756: an
`AS` literal on a contained program is already COBOLNET1795, so the table key is the externalized name. Goldens
2002/pb165_dynamic_call_description_check and 2002/pb1422_function_argument_exception_no_activation; rows
GR-14.8.2.3.2-L2.1, GR-14.8.2.3.3-L2.1, GR-14.9.4.4-3, GR-8.4.3.2.4-6, DOC-A.1-102, DOC-A.1-100 CONFORMS, DOC-A.1-99
DOCUMENTED-NON-SUPPORT, plus PB322's fourteen.

**Group Z2 — PB988, carrying group Z (PB1274, PB1503, PB1449, PB1582, PB1505, PB1020); PB1940 split.** Z: OVERRIDE and
FINAL now resolve after every class's data has bound (`OoClassTable.ResolveOverrides`), so a PROPERTY clause's
accessors are superclass methods like any other; §11.7.3 SR5/SR6/SR7 have one home each (COBOLNET2790, the factory arm
was missing); interface GET/SET PROPERTY prototypes and interface-typed property receivers work; a factory METHOD-ID
NEW is ordinary outside BASE and NEW OVERRIDE overrides BASE's now-virtual `__New` (SUPER reaches `base.__New()`);
§11.3.3 SR3/SR4/SR7 get COBOLNET2791 and run over parameterized definitions. Z2: §10.7.3 had five partial copies and
none for PROGRAM or FUNCTION names; one pre-bind `Validation/EndMarkerPass` checks every end marker against its
definition (SR2, SR4, SR6-SR9), the nesting order (SR3), SR1's presence rule and SR5 for factory, object and interface
methods including a PROPERTY method's bare END METHOD (COBOLNET2793); the old copies are deleted and
`EndMarkerRuleDriftTests` pins the single home; FUNCTION-ID is gated at 2002. SR1 made 38 concatenated NIST bundles and
30 test sources non-conforming; they gained END PROGRAM markers and nine differential goldens were re-keyed. Rows: Z's
three batches (13 rows off the GAP) and Z2's ten CONFORMS. Z's lead on SR-11.3.3-7 (enforced now, yet still DOCUMENTED-NON-SUPPORT through the R43
selector) is the owner question PB1972.

**Group Y — PB1674, PB1261, PB1263, PB1265 — DROPPED at the lander's review.** The review of the train diff found one
confirmed correctness defect: `DataBinder.Odo.cs#OccursKeyItemFault` walks §13.18.38.3 SR4's chain from `key.Parent`
to the table, so when the key is the subject of its own OCCURS entry (legal by SR3 and SR6) the walk starts above the
table, and an outer OCCURS group refuses legal source with COBOLNET2787. Repro: `05 OUTER OCCURS 2. 10 T PIC X(3)
OCCURS 3 ASCENDING KEY IS T INDEXED BY IX` with SEARCH ALL T prints FOUND on a build without Y and fails to compile
with Y. The branch (`worktree-wf_dcb48a76-667-3`, `0ab613a13`) is otherwise complete; the finding and the fix shape are
in kb/Work/PB1263. Its codes COBOLNET2787-2789 are unused. PB1042 stays open as its own redesign.

**The train.** Five clusters brought in by patch (`git apply -3`), conflicts only in DiagnosticCatalog, DIAGNOSTICS.md,
BinderDriver (ProcedureFormatPass and EndMarkerPass are independent pre-bind passes; both kept) and the negative
manifest (whole elements; every manifest checked as main plus each cluster's additions, none lost). Every verdict batch
was re-checked on the merged tree with `record_verdicts.py --dry-run` (0 rows changed). Y was removed with
`git rebase --onto` past its commit. Gate (lander mode, whole population, one leg), with Y: GREEN 10,467/30,555/35;
without Y: GREEN — Conformance 10,450/10,450 · Unit 30,553/30,553 · Characterization 35/35 (run 20261004T210506Z-c84535). Legacy integration 503 passed, 1 skipped. Linux gate: GREEN on all four legs (unit 30,584, characterization 35, conformance 10,450, guard 362 MATCH and 2 TERMINATES declared; HEAD 8c3c0980b). Audits (code and doc citations,
evidence supersession, witness loss, drift rules, work register) clean; semgrep counts unchanged. GAP 336 → 289. Codes
claimed: COBOLNET2784-2786, 2790-2791, 2793, 2796-2797; returned unused: 2778-2783, 2787-2789, 2792, 2794-2795, 2798.
Notes filed: PB1969-PB1972.

