## Entry 1296 — 2026-08-18 00:30 PDT — PB61 lands: the LENGTH/BYTE-LENGTH folds enumerated from the rule branches, one builder for a runtime-length group, and SR6 decided before anything binds

**All 14 PB61 rows adjudicated (10 CONFORMS, 4 PARTIAL — each PARTIAL naming the note that owns its residue);
GAP 4089 → 4079.** The two folds had grown by accretion — an arm added when a shape was met — and the sweep of
2026-08-09 had listed what that leaves: a ref-mod arm missing on the BYTE-LENGTH side (a `BoundExprError` that
compiled clean and threw at run time), the ODO arm on the LENGTH side staged loud while its file-mates were
rewritten, dynamic-capacity tables staged loud on both, the national dynamic-length item staged loud on one side,
`Math.Max(1, …)` clamps answering 1 for zero-length literals, every INDEX/POINTER/COMP-1/COMP-2 carrier folding to
1, a group with a national child under-counted, PHYSICAL unwritable on BYTE-LENGTH (the catalog row was
`Fixed(1,1)` while LENGTH's had gained the keyword — a two-arm dispatch with one arm fixed), and a level-1 TYPEDEF
unreachable as an argument because it is filed off the data-name namespace.

**The shape of the fix is the arm list rewritten from §15.50.4 / §15.14.4 in RULE ORDER**, not patched arm by arm:
`BindLengthFamily` serves both functions (`argument-1 [PHYSICAL]`, and a bare word naming a `TypeDecls` entry is a
type-name argument); each fold's arms are literal → ref-mod view → runtime-length group → ANY LENGTH / DYNAMIC
LENGTH item → elementary boolean (r1) → the fixed fold, where the fixed fold is `LengthPositions` (r2 national
positions for an elementary national item, r3 "alphanumeric character positions" for everything else — which under
the 1-byte-per-position model IS `ByteWidth`, so a POINTER is 8, X(3)+N(2) is 7, X(3)+COMP 9(4)+POINTER is 13).

**One builder for a group whose length is a runtime value.** §15.50.4 r4b (an OCCURS DEPENDING table beneath the
group), r7 (dynamic-length items and dynamic-capacity tables beneath it — the §8.5.1.12.1 variable-length group)
and their §15.14.4 twins can COEXIST in one group, and the arm-per-source design had already produced a silent
under-count: a group with an ODO table AND a dynamic-length leaf took the ODO arm, which never looked at the leaf
(2 + 3×3 = 11 where 2 + "HELLO" + 9 = 16 is the answer). `VariableLengthGroupSum` now takes ANY such group and
builds one expression, in bytes, from ONE width walk corrected for what it cannot know: `ByteWidth` counts an ODO
table at its maximum, a dynamic-capacity table as one occurrence and a dynamic-length leaf as zero, so the builder
subtracts the first two and adds the runtime term for each — the ODO extent as a new `BoundOdoExtent` node
(data-name-1 clamped to [integer-1, integer-2], EC-BOUND-ODO outside, rendered through the SAME `CobolTable.OdoExtent`
the sending image slices with, so LENGTH and the MOVE slice cannot disagree; the renderer's `OdoGroupByteExtent`
is deleted, the semantics live in the binder), each dynamic-length leaf's current byte length, and each
dynamic-capacity table's `Capacity × element bytes` through a `CapacityRegisterPlace` — the register is now minted
for EVERY dynamic table and NAMED only under `CAPACITY IN` (`DataBinder.DynamicResolve`), because the view over the
capacity exists whether or not the program named it. Every subordinate's place is derived from the group's own
access path (a `MemberSegment` per level), so a subscripted or nested group sums correctly rather than
re-resolving by name from the root. Measured: ODOG 11 → 17 after MOVE 5, ODOD 16, DCG 2 → 14 after SET CAP TO 4,
DCN 2 → 11 after a receiving reference grew it, MIX 4 + 8 + 2×(2+2) = 20.

**§8.4.3.2.3 SR6 is decided ONCE, from the DEFINITION, BEFORE any argument binds** (row SR-8.4.3.2.3-6). The
sweep had found that only the reserved word RANDOM drew the SR6 verdict: `FUNCTION UPPER-CASE (1:4)` parses as a
name plus a `refModPart` (the FNARG_LPAREN belongs to the ref-mod), so the argument list is EMPTY and binding it
first reported "takes 1 argument(s); 0 given" — an arity error about a list the user never wrote — and the SR6 arm
inside `ResultRefMod` never saw a call, only the error. Keyword-omitted `UPPER-CASE (1:4)` under FUNCTION ALL
INTRINSIC fell through the MinArgs guard to the data path and died "'UPPER-CASE(1:4)' is not defined". Now
`DefinitionPermitsArguments` (the catalog signature, or a REPOSITORY user function's USING formals — SR6 names
function-prototype-name-1 too, and §12.3.8.2 GR12 gives the user function precedence) is asked on every route
before the bind, `Sr6ArgumentListError` reports COBOLNET1543, and `ResultRefMod` carries §8.4.3.3.3 SR2 (class)
only. `FunctionRefModSr6SweepTests` sweeps the CATALOG SOURCE (the `IntrinsicArgumentClassDriftTests` precedent):
86 argument-permitting rows × both forms draw 1543 and never 1504/1639; the 7 zero-argument rows do not.

**Two green tests were holding gaps open, and both are corrected in this landing.**
`IntrinsicFunctionDifferentialTests.StagedLoudResidue_FailsLoud_NeverWrong` PINNED the loud stage for
`BYTE-LENGTH(WS-X(1:2))` — a conforming reference (§15.14.3 r1 + §8.4.3.3.4 GR6) whose answer is 2; it is now the
positive twin, and the doctrine is proven on the ONE named residue left in the family (a runtime-length item inside
a table element — a per-occurrence loop, staged loud with the shape in its message). Golden
`r03_hexadecimal_national_and_boolean_literals` pinned `LENGTH(X"") = 1` — its own DEVLOG entry says "identical to
FUNCTION LENGTH(""), consistent with the existing behaviour rather than inventing a new answer": observed, not
derived. §8.5.4 item 8 says zero; rows 13/14/17 now read 0000, with the derivation in the golden's header.

**Two sibling defects registered rather than folded in.** `kb/Work/PB79` — the GROUP-USAGE clause (§13.18.29) does
not parse at all (`01 NG GROUP-USAGE NATIONAL.` → "unexpected 'NATIONAL'"), so the bit-group / national-group legs
of RV-15.50.4-1/-2 are unreachable (those two rows are PARTIAL naming it; the clause is 7 GAP rows of its own).
`kb/Work/PB80` — a BASED root is a string-canonical class the resolver returns as a `RedefViewPlace` BEFORE the
`OdoModel.WrapGroup` step, so an ODO group inside a BASED entry never becomes an `OdoGroupPlace`: measured,
`MOVE BODO TO OUT` copies the MAXIMUM ("ABcccdddeee") where the ordinary group is sliced by GR8a ("ABcccddd"), and
LENGTH's r4a based-entry half (unassociated → the maximum, associated → the current extent) stages loud by name.
RV-15.50.4-4 / RV-15.14.4-2 are PARTIAL naming it. Both are the sweep's own finding shape: a defect ELSEWHERE that
a LENGTH row happens to expose.

**Honest scope.** A type declaration with an ODO subordinate takes "the rules of the OCCURS clause for a receiving
data item" — read as GR8b's maximum (TODO = 17), which is also what the sweep's expectation and the ALLOCATE-sizing
purpose say; the alternative reading (GR8a's current value for an outside data-name-1) is recorded in PB80's note
as the point to settle when the based half lands. PHYSICAL on BYTE-LENGTH is transparent under the same
CONFORMANCE.md determination LENGTH's PB24 recorded, now written there. PHYSICAL's own introduction edition
(believed 2014, with variable-length groups) is not separately gated — the 2002 text is not in the submodule to
cite; recorded on FMT-15.14.2.

**Gates (wave-local).** Solution build · Characterization 33/33 · Conformance
`Intrinsic|Corpus|Function|Length|RefMod|Udf|SpecTraceability|Odo|Dyn|Occurs|Negative` **1126/1126** (the new
golden `pb61_length_byte_length_rule_branches`, the five `pb61-*` negatives, the 179-case SR6 sweep) · Unit
`SpecTraceabilityInventory|Intrinsic|Odo|Length|Dyn|Bound|Capacity|Diagnostic|Manifest` **733/733** · CLI probes
(`sr6/*.cob`, `vl.cob`, `tb.cob`, `len2.cob`). The comprehensive battery runs next for the PB58+PB61 batch.
Docs: COBOLNET_INTRINSICS_DESIGN.md (the LENGTH/BYTE-LENGTH section and the SR6 paragraph replace the two stale
edge-case bullets), CONFORMANCE.md (BYTE-LENGTH PHYSICAL), kb/Work PB61 → landed, PB79/PB80 new, plan §0.

