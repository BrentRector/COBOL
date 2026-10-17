## PB1784 + PB745: one DiagnosticCatalog descriptor per OO definition rule

Wave 1108, group A, the FINISH of the kb/Work PB1784 analysis. Its decomposition table had landed through
its child notes; the re-probe on `62d0dca6a` ran every child's goldens and negatives on this build (PB1020, PB1062,
PB1112, PB1251, PB1308, PB1497, PB1499, PB1502, PB1505, PB1582, PB2051): 11 positives matched their `.out`, and every
negative refused with its code. None of the measured defects reproduces any more. What remained was item 2, "one
descriptor per rule", and the interface half of item 1 (§11.6.3 SR6 sharing SR7's descriptor). PB745, the two A.4.10
declines with no descriptor, is the same mechanism and in the same file, so it lands with them.

**The defect shape.** The class table's codes were bare literals that no `DiagnosticCatalog` entry owned
(`DiagnosticRegistryDriftTests.EveryEmittedCode_IsACatalogDescriptor` cannot see a qualified `edition.Error("…")`,
kb/Work PB294), and the numbers had been reused across unrelated rules. COBOLNET0840 carried eight: a duplicate
interface, a class named like an interface, OVERRIDE/FINAL in a prototype, a duplicate prototype, an unresolved
INHERITS or IMPLEMENTS interface, the interface INHERITS cycle and a repeated interface-name. 0820 carried four rules,
0839 two (§11.3.3 SR5 and §11.7.3 SR3), and 0849 both the Annex A.4.10 item-1 decline and GOBACK/EXIT RAISING's
mandatory §14.9.18.3 SR4.

**The fix.** Each rule now has one descriptor, and the existing number stays with the meaning the goldens pin:
- 0820 `OoDefinitionNameNotUnique` (§8.4.6.4 / §8.3.2.2), for plain and parameterized definitions alike.
- 0821 `InheritedClassNotInScope` and 0840 `InterfaceNameNotInScope`, the two codes the §8.4.6.4 funnel reports.
- 2791 `InheritsClauseRule` (renamed from `ClassInheritsRule`): §11.3.3 SR3/SR4/SR5/SR7 and §11.6.3 SR3/SR6, one
  rule shape keyed on the written name.
- 0849 `MultipleInheritanceDeclined` and 0822 `ParametricPolymorphismDeclined`: `Annex: A4`, suppress family
  `declined-optional-element`, emitted through `EditionContext.Declined`. An interface's second prototype of one name
  is the same item-3 decline as a class's second method.
- Registered with their existing numbers: 0829, 0837, 0838, 0839 (now only the FINAL method), 0841, 0842.
- New codes: COBOLNET4211 `RaisingObjectOperandRule` (split off 0849; EcBinder's four sites), COBOLNET4212
  `MethodPrototypeOverrideOrFinal` (SR2 and SR8 each report), COBOLNET4213 `OverrideNamedLikeItsClass` (the CS0542
  implementation restriction, split off 0820).

§11.6.3 SR6 is now asked of the written list before resolution, as SR7 is, so `INHERITS FROM Z Z` with no Z
reports the repetition and not just two unresolved names.

**Found on the way, fixed (I9).** The §11.3.3 SR4 walk cut each link as it reported. With A → B ⇄ C it reported A,
which breaks nothing, and never reached C, which does. The cycle members are now found over the uncut links, each one
reported, and only then cut. A class named like an interface drew two reports (§8.3.2.2 plus "duplicate class") and
now draws one.

**Goldens.** New negatives: `pb1784-class-inherits-into-cycle`, `pb1784-interface-inherits-unresolved-twice` and
`pb1784-interface-duplicate-prototype`, plus `OoSpineTests.InheritsCycle_ReportsEachMember_AndNotTheClassLeadingIntoIt`.
Seven `.err` files and five test pins follow their rule's new code. The annex guard is proved to see 0849: flipping its
annex to A3 turned three of `DeclinedFacilityAnnexDriftTests`' five facts red (reverted).

**Registers and docs.** CONFORMANCE.md §4's A.4.10 paragraph and §5's row name the descriptors. §2 row 45 no longer
names 0822, because an A.4 refusal belongs to §5. The OO, conditions/exceptions and interprogram design docs are
updated too. Semgrep `cobolnet-raw-diagnostic-code-literal` went from 267 to 245, and the baseline is locked. No
inventory row changes verdict (PB1784 and PB745 both carry `closes_rows: []` with a reason).

**Self-review.** Architecture: one descriptor per rule, the declines go through the one seam, and the funnel keeps its
`code` parameter but every caller now passes `Descriptor.Code`. Full code: SR6 runs before resolution, as its class
twin does, and the cycle walk no longer mutates while it reads. Performance: one extra pass over the classes' base
chains, linear in the length of each chain. Duplication: the class and interface INHERITS rules now share one
descriptor instead of two codes.

**Gate.** Run `20261017T105616Z-457d02`, on HEAD `7f7ad8651`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN`.
Leg 1 ran Conformance 6,161/11,951, Unit 33,489/33,925 and Characterization 5/36 with no failure. The three new
negatives, the new OoSpineTests fact, the renamed `_4211` tests and the seven re-pinned negatives all passed in leg 1.
The Linux gate is run at landing (batched gating).

**Branch review round 1 (R1-1, doc accuracy).** The reviewer found the old code meanings still written where the
current state is described: the OO deep-dive said a FINAL base class (§11.3.3 SR5) was 0839, `OoClassTable.cs` said a
class/interface collision was 0840 (it is 0820), `OoClassSymbol.cs` and `CobolOO.g4` said a FINAL superclass was 0839,
`CobolOO.g4` named 0840 as the prototype-body screen (it is 2272), seven negative-golden headers named the superseded
code while their `.err` pinned the new one (l1c15 cyclic and pb1502 now 2791, l1c18 and l1m7 now 4212, pb814 and
pb815 now 4211, pb660 now 0820 alone), and ten inventory notes still described the old mapping. All are corrected.
The test `OverrideFinal_0838_0839` is renamed `OverrideFinal_0838_0839_FinalBase_2791` with its four inventory
test-refs. Each of those rows records the old name as a `retired-witness:`, the witness-loss audit's form for a
rename; the first re-gate went RED on that audit before the mark was added. The GR-11.3.4-3 probe was re-run on this build (`CLASS-ID. FC INHERITS FB.` with FB FINAL gives
COBOLNET2791 citing §11.3.3 SR5 / §11.3.4 GR3 at 2002, 2014 and 2023). The root was that nothing reads a golden's
header, so a new drift test over `tests/conformance/negative`, `NegativeGoldenHeaderCodeDriftTests`, now checks it
(a directory below the sweep depth, so it carries no `Sweep` trait; the second re-gate went RED on
`HostWitnessTraitDriftTests` while it did): a code a header states after
`Expected`, `Diagnostic` or `->` must be one its `.err` pins. It checks 85 goldens. On the reviewed head it reports
five of the seven stale headers; the other two named the code in prose. A built-in fixture proves the detector trips.
The review finisher's gate, run `20261017T113417Z-7025a0` on `f84c138f0`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN`, with Conformance 6,246/11,953, Unit 33,517/33,927 and Characterization 5/36 run and no
failure; both new facts, the renamed OoSpineTests fact and the seven re-headed negatives passed in leg 1.

Branch review round 2 found two more residues of the same rename. R2-1: the implementer had also renamed
`OoSpineTests.GobackRaising_BindViolations_0849` and `RaisingPhraseTupleTests.ExitProgramRaising_FactoryPresenceDiffers_0849`
to their `_4211` names, but the inventory rows SR-14.9.18.3-4 and SR-14.9.14.3-5 still cited the old names, so
`SpecTraceabilityInventoryDriftTests.EveryTestRef_ResolvesToARealTest` was RED ("2 unresolved test-ref(s) over 4195
inventory row(s)", reproduced on the reviewed inventory). Leg 1 never ran that method: it is tier 1 and over the 2 %
leg-1 budget, so it sat in leg 2 while twelve cheaper methods of the same class ran. Both rows now cite the `_4211`
names and record the old ones as `retired-witness:`; the witness-loss audit reads 0 unexcused, 6 retired, and the
drift test passes. A sweep of every test method the branch removed found no third. `kb/Work/PB389.md` named the
old GOBACK test as the pin and now names the new one. R2-2: the SR-11.6.3-3 note said the cycle DFS reports
COBOLNET2791 but its probe lines still read 0840. The three probes (self, two-cycle, three-cycle) were rebuilt and
re-run at 2002, 2014 and 2023; each reports COBOLNET2791 on every interface in the cycle, so the note now says so
and names the golden as the row's spec-derived test in place of "test-needed". A sweep of every inventory note
stating a probe result `-> 0820/0821/0822/0838/0839/0840/0849` found the rest current: the 0840 lines of
SR-11.4.3-1, SR-11.6.3-2 and SR-11.8.3-1 are the not-in-scope funnel (sr2_classname, sr2_norepo and sr2_undefined
re-run: 0840 citing §11.6.3 SR2), and the 0839 lines are the FINAL-method rule.
The round-2 finisher's gate, run `20261017T115300Z-00d321` on `00a3735e5`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN`, with Conformance 8,201/11,964, Unit 33,529/33,932 and Characterization 5/36 run and no
failure. `EveryTestRef_ResolvesToARealTest` was again left to leg 2, so it was run directly: 1/1 passed (red 1/1 on
the reviewed inventory).
