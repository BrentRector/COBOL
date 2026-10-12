## Entry 1853 — 2026-10-03 15:31 PDT — Train 1009: wave 1009 groups B, C, A (the SET sender and address-name operand, intrinsic argument rules, the report SOURCE identifier and VARYING counter)

Three clusters, landed as three commits plus this entry. All three implementer branches were cut at `e08b604ca`, which
was still origin/main when the lander applied them, so none needed a rebase. Group A came back SPLIT: four of its five
notes (plus PB1049) are done and gated, and PB1294 (rolled totals) was not started and stays open.

**Cluster B — the SET sender and the address-identifier name operand (PB1399, PB1417, PB1929, PB1452; PB777 and PB610
discharged).** All four repros reproduced. The first mechanism was the SET sender. `OoBinder.OoExtractBareReference`
treated only a bare data reference as "the identifier", so a function-identifier, an inline method invocation and a
keyword-omitted function were refused by every format as a literal or an expression. Each of those references a data
item (§8.4.3.2.1, §8.4.3.4.4 GR1), and §8.4.3.1.3 SR1 admits any identifier format. The new `SetSender` /
`SetSenders.Classify` binds a sole such identifier once and classifies it. Format 1 asks its class, so an index function
(§15.2 item 6) is identifier-2: `SET TX TO FUNCTION MAX(IA IB)` now compiles. Formats 5, 7, 8 and 9 read the returned
temporary through `CarrierSenderPlace` and `OoBindSetObjectRef(SetSender)`. Formats 14, 16 and 17 are decided from the
receiving list before classification, because classifying binds and would run a user function twice. The second
mechanism was the address-identifier name operand. §8.4.3.12.3 SR1 and §8.4.3.13.3 SR1 are the same sentence, and it was
tested twice on the raw `Pic?.Category`, which refused a group as "category (none)" and accepted PIC A. Now
`SetBinder.ScreenAddressName` asks `ItemCategory.IsAlphanumericOrNational` (a new `Place` overload, so a ref-mod view
takes §8.4.3.3.4 GR6's category). The grammar gained `functionAddressIdentifier`, and both address-identifier rules take
`functionCall | inlineMethodInvocation | dataReference`. `PtrEmitter.NameImage` reads the name's character image (a
group's read is a struct: CS1503). The implementer also fixed `CobolArgAdapt.StoreReturn(FunctionPointer)` (I9): a
FUNCTION-ID RETURNING USAGE FUNCTION-POINTER drew CS0315. PB777's premise did not hold on today's tree, because PB555
(`e76c275d0`) had already landed the gate and SET TO FALSE, so it was discharged with the re-probe as evidence. PB610 was
stale (PB549). Goldens: `85/pb1399_set_index_function_sender`, `2002/pb1929_set_identifier_senders`,
`2002/pb1452_program_address_identifier_operands`, `2014/pb1417_function_address_identifier_operands`,
`2014/pb1929_set_function_pointer_function_sender`, and five negatives. Rows: SR-8.4.3.13.3-1 DIVERGES → CONFORMS and
FMT-8.4.3.13.2 PARTIAL → CONFORMS. No codes were used (2733–2736 were returned).

**Cluster C — intrinsic argument rules (PB658, PB1420, PB1901).** PB658: the `'t'` argument kind was a hand-listed
category set that included the de-editing class, and only a bespoke `IntrinsicBinder` arm (NUMVAL-C / TEST-NUMVAL-C)
refused an edited item. The kind is now `'a'` (ByClass alphanumeric or national), and a per-position predicate
`ArgPredicateKind.CharacterCategory` sits on exactly the rows whose clause says "category": NUMVAL §15.67.3,
NUMVAL-F §15.69.3, NUMVAL-C §15.68.3, TEST-NUMVAL-C §15.94.3 and ORD §15.70.3. The bespoke arm was deleted, and a drift
test reads each clause's wording from the spec. Under `--permissive` the category screen is now the usual warning. PB1420
had three parts. First, §8.4.3.2.3 SR12 now has a screen (`UnsignedIntegerViolation`) at CONCAT §15.18.3 r3 and at
BASECONVERT §15.12.3 r1 below base 11. Second, RANDOM argument-1 and COMBINED-DATETIME argument-1 are integer positions,
and a new drift test derives every Int-only position from §15.6 Table 21. Third, the SR12-exempt integer ABS now renders
in CONCAT (`CONCAT(ABS(-3) "A")` = `3A`) instead of aborting at run time. PB1901: the classification half was refuted,
because §15.3 item 2 treats a strongly-typed group as alphanumeric for an alphanumeric argument, so `UPPER-CASE(G)` is
legal. The crash half was real: a pointer-leaf strong group aborted. The new `OperandText.AsTransferString` is the one
one-way reader. DISPLAY's private arm was deleted, and the intrinsic string-argument visitor and CONVERT's `StorageArg`
now call the new reader. Goldens: four positives and seven negatives. Rows: SR-8.4.3.2.3-12 DIVERGES → CONFORMS and -11
PARTIAL → CONFORMS. The semgrep raw-diagnostic-code baseline went from 288 to 287 (the deleted arm). No codes were used
(2737–2740 were returned).

**Cluster A — the report SOURCE identifier and the VARYING counter (PB1306, PB1292, PB1316, PB1456, PB1049; PB1294
open).** Every probe reproduced. SOURCE identifier-1 was a two-arm dispatch: a data-phase name lookup for the identifier
and the procedure-phase binder for the expression. So a subscript, a reference modification, another report's counter,
a sum counter, a VARYING counter and a constant-name were each a staged refusal of legal source. identifier-1 is now
`FieldReferenceSource`, bound in the procedure phase by the operand binder a MOVE sender takes (§13.18.53.4 GR1), and the
RD CODE identifier uses it too. A constant-name is arithmetic-expression-1 (§13.10.3 SR2), classified once by
`IdentifierOperandOf`. A VARYING counter is now the data item of its entry. It has a scope stack, it is named through
`ReferenceResolver.VaryingScope` in operand, expression and subscript positions, and SR2/SR3 are asked once per written
entry. Counter qualification goes through one `CounterReportOf` with the enclosing RD as the implicit qualifier, and a
violation is COBOLNET2729 (replacing two COBOLNET0899 stagings, PB1049). The implementer also found and fixed (I9) that
`BindMethodRoster` never bound a class's report clauses. One guard, `BindDeclaredReportClauses`, now serves both unit
kinds. Two determinations are recorded in CONFORMANCE.md §3: several counters of one VARYING clause advance in clause
order, and a reference-modified RD CODE identifier needs literal position and length. PB1316's sweep note did not hold
as filed: VALUE, PRESENT WHEN and OCCURS already took a constant-name, but `LINE PLUS KL` / `COLUMN KC` are parse errors,
filed as **PB1947**. Goldens: six positives (85 and 2002) and eight negatives. Rows: 15 CONFORMS. Code COBOLNET2729 was
used (2730–2732 were returned).

**The train.** Lander gate (whole population, one leg): `=== BUILD-LOCAL GATE: GREEN — Conformance 10,015/10,015 · Unit
30,408/30,408 · Characterization 35/35 cases ran (skipped 0) in 1 of 1 leg(s) · lander mode (no plan (lander); run
20261003T222037Z-b50ed7) ===`. The legacy integration assembly ran 503 passed and 1 skipped. The Linux gate under WSL was
GREEN on the checkpoint head `6193b7951` (same trees as the landed commits). Semgrep: BigInteger 46 → 46, decimal 2 → 2,
raw diagnostic code 288 → 287. The CI audits all passed: code and doc citations had 0 findings, evidence supersession 0
gating, witness loss GREEN (0 unexcused, 9 retired with group A's deleted SOURCE symbols, 19 re-sited), drift_rules current, and
work.py well-formed. Each cluster's inventory hunk was discarded and its `record_verdicts` batch re-applied on the merged
tree, in order: GAP 523 → 521 → 519 → 504. B's batch reproduced B's inventory byte for byte. The manifest conflicts
(2002, 85, negative) were whole list elements, and keeping both sides gave element counts equal to base plus each
cluster's additions in all five manifests. Nine citations were spot-checked with `cite.py --check`, three per report, and
all were OK. The pre-push review of the diff (SET sender, intrinsic channels, report binder and emitter) found nothing to
confirm, and no cluster was dropped. New lead ids used: PB1947.

