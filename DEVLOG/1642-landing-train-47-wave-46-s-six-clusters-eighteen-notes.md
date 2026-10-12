## Entry 1642 — 2026-09-22 14:42 PDT — Landing train 47: wave 46's six clusters, eighteen notes landed and one retired, GAP 2266 → 2208

Train 47 carried wave 46's six clusters in one landing: H, I (with its two finishers I2 and I3), J, K, L and M. It lands eighteen `kb/Work` notes and retires one. Each cluster came in as a squash of its implementer branch onto main, with the traceability inventory held at main's copy. Every cluster's verdict batch was then re-applied in landing order through the PB959 witness-MERGE writer. There is one commit per cluster and then this prose-and-inventory commit.

**Cluster H: PB846 + PB847 + PB848, printed optional words.** Three general formats were modelled narrower than the printed page.

- SORT Format 2 prints its KEY phrase in brackets (rendered p776). So `SORT table` alone is legal when the table's OCCURS clause has a KEY phrase (§14.9.40.3 SR15), and it sorts on those keys (GR21). The keys come from the same `OccursSpec.Keys` model SEARCH ALL reads.
- TO is not underlined in the three pointer usages, and OF is not underlined in OBJECT REFERENCE's FACTORY phrase. The TO-less operand is guarded by a predicate over FOLLOW(usageKeyword), computed from the ATN. The drift tests measured `USAGE POINTER BIT` and `USAGE POINTER HIGH-ORDER-RIGHT` each swallowing the word under weaker guards.
- A function-pointer can now be called. Both name forms bind through one prototype activation, and `CallEmitter.InvocationText` is the one renderer for both invocation sites. The per-evaluation arm had no pointer arm: the two-arm shape again. A NULL pointer raises EC-FUNCTION-PTR-NULL. SR5 is COBOLNET2234.

Rows 2 + 0 + 3.

**Cluster I: PB815 + PB814, the RAISING phrase as the tuple §14.2.1 prints.** One `RaisingPhrase.Partition` replaced two verbatim copies. One flag-to-flag comparison serves GOBACK SR4 and EXIT SR5 a)–c). Interface conformance now shares the extracted `MethodConformanceMismatches` with IMPLEMENTS. Two more defects were found and fixed on the way:

- Every method's RAISING checks used the LAST method's header, because the state loaded in the roster loop and not in the bind pass.
- An interface-typed exception object was a Roslyn CS1503 at both raise sites.

Both H and I made OBJECT REFERENCE's OF optional independently. The lander kept one grammar change and merged the two comments. Rows 5.

**Cluster I2: PB757, INVOKE OMITTED and the method omitted-argument ABI.**

- Every method formal crosses as `ref T, bool omitted`. `OoSignatureOf` declares the pair, `OoArgPair` renders it, and a drift test holds those two as the only spellings.
- One `OmittedProbe` fact serves program, function and method formals. So an omission forwards across CALL and INVOKE in both directions. The user-defined-function arm, a staged COBOLNET1506 until now, takes OMITTED and trailing omission too.
- SR18 is COBOLNET2237 and §8.4.3.2.3 SR9 is COBOLNET2238. §9.3.8.2.3 rule 8 is enforced for IMPLEMENTS and for overrides, and the 0899 optional-formal stage is deleted.

Rows 8.

**Cluster I3: PB759, parameterized classes and interfaces with REPOSITORY EXPANDS.** An expansion is built by re-parsing the skeleton's own tokens with every formal replaced (§12.3.8.4 GR5 literally, OO design D12). It is an ordinary class, so every existing OO rule and the emitter apply to it unchanged. COBOLNET2239 covers the definition's USING rules and COBOLNET2240 covers EXPANDS. `EditionContext` now reports each distinct diagnostic once, so a skeleton defect is not repeated per expansion. Rows 19.

**Cluster J: PB716 + PB790 + PB862, SPECIAL-NAMES.**

- §12.3.7.3 SR8 is enforced from ONE implementor-name table, following the surveyed implementations. The device-names are CONSOLE/SYSIN/SYSOUT/SYSERR, the feature-names are C01/CSP, and the switch-names are SWITCH-0…36 and UPSI-0…7. An unavailable name is refused with COBOLNET2241.
- Every consumer now asks the mnemonic's KIND: DISPLAY/ACCEPT 0817, SET 1757, and WRITE ADVANCING COBOLNET2243.
- CLASS's FOR phrase moved to its printed position, between class-name-1 and IS (folio 290, rendered). The note's premise was wrong: the postfix spelling had been the defect, and it had refused the legal printed form.
- THROUGH with ALSO in one ALPHABET literal phrase is COBOLNET2242.

Rows 13 closed and one PARTIAL restated.

**Cluster K: PB873 + PB615 + PB834, CALL argument carriers.**

- PB873: `CobolArg` now carries the argument's whole `NumProfile` in place of a `(Digits, Scale)` pair. So a signed argument keeps its sign on both image arms (§14.2.3 GR8–GR10).
- PB615: a PRESENT argument the formal cannot read fails the activation with EC-PROGRAM-ARG-MISMATCH (§14.9.4.4 GR3 d)). It used to arrive as the omitted carrier's silent zero.
- PB834 was re-probed and discharged: PB663 had already fixed it.

CallEmitter's two forward arms were the train's only real source conflict (K against I2), and the resolution keeps both: PB757's probe with K's profile. Rows 1.

**Cluster L: PB853 + PB885, report entries and OCCURS operands.**

- §13.15.3 SR10/SR11/SR13/SR15 are screened once per written report entry with COBOLNET2247. The fabricated SPACE operand is deleted.
- Every OCCURS data-name operand goes through `ClauseDataName` in both the report arm and the data-division arm. That fixes a silently dropped subscript and a glued qualifier that had rejected `DEPENDING ON CNT OF G1`.

L's own gate never produced a verdict, so the train gate was its first. It turned up one real consequence: `ReservedWordMigrationGateDriftTests`' control probe wrote `03 VALUE "AB" PIC X(2).` in a report group. That source is non-conforming under SR13 ("A COLUMN clause shall be specified in each elementary entry that has a VALUE clause"). The probe now writes `COLUMN 1`, and L's commit carries that change. Rows 7.

**Cluster M: PB871 + PB866, with PB825 retired.**

- PB866: an IS-form EDITING phrase on a floating-point edited item is legal, because SR12's ban sits inside its FOR-phrase list. `EmitContext.EditCfg` became the one EDITING producer after the float arms were found dropping it.
- PB871: `ReceivingStore` is the one elementary character receiving store, held by a drift test. UNSTRING, ACCEPT and STRING now honour §8.5.1.10.4 under the new determination D-DL2: a dynamic-length receiver's size is its maximum.
- PB825's retirement was verified against its licence. `kb/Work/R13` is landed with the owner decision "follow GnuCOBOL": DISPLAY of a BinaryCapacity item renders the PICTURE-digit image (§14.9.11.4 GR1, implementor-defined).

The implementer reported the wrong branch name; the manifest named the real one. No rows.

**The train.**

- **Merge.** Nothing was dropped. Conflicts were whole-element appends in `DiagnosticCatalog`, `DIAGNOSTICS.md` and four corpus manifests: both sides were kept and deduplicated, `DIAGNOSTICS.md` was regenerated, and it was byte-identical to the union modulo EOL. Beyond those appends there were only the two OO comment and superset hunks and the CallEmitter hunk. Conflict markers were checked on both the working tree and the index before every checkpoint.
- **Codes.** The claimed codes 2234, 2237–2243 and 2247 lie inside each cluster's assigned range with no collision.
- **Conformance gate.** The WHOLE `Cobol.Net.Tests.Conformance` assembly, unfiltered: **7903/7903**.
- **Unit gate.** First Unit run: two reds, both attributed.
  - The L probe above.
  - `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`: J deleted `AcceptInputDevices` and `DisplayOutputDevices`, and the merge writer kept them as witnesses on DOC-A.1-2 and DOC-A.1-59. A train retirement batch retires the two.
  - After those two fixes: Unit **28730/28730**, Characterization **33/33**.
- **Integration gate.** Red once: `GrammarBatch1Tests.Class_ForAlphanumeric_ParsesSuccessfully` wrote the postfix CLASS FOR that J correctly removed. It now writes the printed position, in J's commit, and the assembly is **503 passed, 1 skipped**.
- **Other checks.** `audit_witness_loss --check` is GREEN: 0 unexcused, 2 retired, 3 re-sited. Code and doc citation audits are 0. `audit_annex_a1` has no findings. `work.py check` passes on 1000 notes. Semgrep `raw-diagnostic-code-literal` went 390 → 386 and the baseline is locked.
- **GAP 2266 → 2208.** Next free diagnostic: COBOLNET2253, the orchestrator's allocation for wave 47. Codes 2235–2236, 2244–2246 and 2248–2252 were returned unused.
- **Leads.** The cluster reports carry twenty-two new leads. They go to the registrar to become notes; none were filed here.

