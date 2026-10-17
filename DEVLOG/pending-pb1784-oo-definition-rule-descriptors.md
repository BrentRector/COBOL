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
