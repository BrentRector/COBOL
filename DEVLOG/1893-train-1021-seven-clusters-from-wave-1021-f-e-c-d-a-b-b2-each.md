## Entry 1893 — 2026-10-05 16:44 PDT — Train 1021: seven clusters from wave 1021 (F, E, C, D, A, B, B2), each with its review fixes folded in (PB1221, PB1408, PB609, PB1282, PB1421, PB1060, PB1112, PB1425, PB1783, PB781, PB858, PB1415, PB1039, PB1755, PB1042 landed; PB434 and PB480 split; PB2071-PB2074 and PB2076-PB2080 filed)

Four landers carried this train. The first stopped before doing any work. The second brought the seven clusters in
and gated them green, and the third verified the review findings. Both of those stopped at the coordination STOP
file. The fourth folded the verified fixes into their clusters and landed the train. Each cluster is one commit. The
implementer branches' own DEVLOG entries were dropped, and this entry replaces them.

**Group F: PB1221.** The report writer's LINE, COLUMN and SOURCE clauses wrote their keyword prefixes as cross
products, so `COLUMN ARE 5`, `COLUMNS IS 5`, `LINES IS 3` and `SOURCE ARE X` compiled clean. §13.18.14.3 SR4 and SR5
pair ARE with the plural keywords and IS with the singular, and §13.18.35.2 and §13.18.53.2 print the braces. The
rule now lives in the grammar as closed alternatives (reportLineClause, the plural and singular halves of
reportColumnClause around reportColumnAlignment, reportSourceClause), the shape `PrintedFormatAlternativeDriftTests`
pins. VersionConformancePass's 2002 gate names the written form, and it no longer calls a single-operand `LINES 3` a
multiple LINE clause. The transcription had drawn `LINE NUMBER IS` outside the brace, so the LINE diagram is
corrected in `specs/ISO_COBOL.md`. Witnesses: golden 2002/pb1221_report_keyword_prefixes, two negatives and a
96-case enumeration test. GAP 194 → 191.

**Group E: PB1408, PB609, PB1282.** PB1408: §8.4.3.11.4 GR2's "type of identifier-1" is the operand's own type. For
a restricted data-pointer that is the pointer's TYPEDEF, and for a strong group subordinate to a type declaration it
is its position in that declaration (new `TypeRestriction.Member` and `Key`). One accessor,
`StrongTypeModel.AddressOfRestriction`, serves the SET sender, the SET receiver, the CALL and INVOKE address
argument and the descriptor key. `SET PP TO ADDRESS OF RP` was refused COBOLNET0869 and now compiles, while `SET
ADDRESS OF BS TO ADDRESS OF RP` used to compile and now draws 0869. PB609: §14.9.4.3 SR14 requires a restricted
program-pointer's prototype signature to equal the AS prototype's. CallBinder compares them through
`PrototypeSignatures.Same` (COBOLNET2936), and the name-to-signature reader, which existed three times, is now the
one `StatementBinder.ProgramSignatureOf`. PB1282: `DataBinder.Sr12Sr14Violation` dropped its same-root exemption,
which was vacuous, so a REDEFINES inside a TYPEDEF STRONG template is refused per TYPE subject (§13.18.44.3 SR14
with §13.18.58.4 GR3). The PB183 golden that pinned the acceptance became a negative. An unused strong template is
still not flagged (kb/Work PB2071). At the implementer's gate the sort test classes raced on CobolSort's static
store, so they are now serialized; the root cause is PB1570, which the train extended. GAP 191 → 188.

**Group C: PB1421, PB1060, the GR16 half of PB434.** PB1421: GO TO ... DEPENDING ON and WRITE ... ADVANCING admit a
function-identifier (§8.4.3.2.4 GR1), classed by its result through `OperandClassScreen.Screen(BoundOperand)`.
`GO TO P1 P2 DEPENDING ON FUNCTION INTEGER(2.5)` was COBOL0001 and now goes to P2, and a non-integer numeric function
is refused COBOLNET2324. PB1060: an address-identifier is an EVALUATE subject and object, and a function argument,
through the one `addressIdentifier` rule and the one binder `PtrBinder.AddressIdentifierOperand` (ConditionBinder's
copy was removed). A wrong answer was found and fixed on the way. A pointer EVALUATE subject that two arms read was
materialized by MOVE, which stores nothing for a pointer, so `EVALUATE P WHEN Q ... WHEN P` took WHEN OTHER.
`SendingValueTemp.IdentityStore` now stores by SET. PB434 GR16: a statement that transfers control out of an
exception-checking PERFORM from its FINALLY phrase is COBOLNET2927 (§14.9.28.4 GR16). EXIT PERFORM stays admitted by
GR16's own sentence. PB434 stays open for SR8, and PB387 was untouched. **Review fixes.** (1) STOP RUN in a FINALLY
phrase was admitted as "ending the run unit", but §14.9.42.4 GR6 transfers control to the operating system, so it
is a transfer out. `TransfersOut` now names it (negative pb434-finally-stop-run). (2) CONVERT and BASECONVERT
accepted an ADDRESS OF argument in the strict lane and then failed at run time. `IntrinsicArgumentRules.StaticUsageOf`
now answers the address operand (two negatives, 1514 and 1642). (3) A folded numeric function result counted as an
integer, because the fold dropped its type, so GO TO DEPENDING and PERFORM TIMES took a non-integer folded function.
The folded literal now carries its FunctionType (two negatives, 2324 and 1646). BASECONVERT's arguments 2 and 3 are
never class-checked; that is pre-existing and filed as PB2079. GAP 188 → 187.

**Group D: PB480 (split), PB1112.** The universal INVOKE described each argument as a string compared for equality,
which could not express §9.3.6's match rule. National, boolean and numeric-edited arguments fell to a sentinel and
were refused COBOLNET0866, while an X(4) into a group formal ran. Now ONE builder (`Oo/ActivationDescriptions`)
produces a structured runtime `ActivationDescription`, and two runtime relations decide: the §9.3.6 match
(`ActivationRelations.Matches`, EC-OO-METHOD) and §14.8.2 and §14.8.3 conformance through §14.9.23.4 GR7 c)
(EC-OO-UNIVERSAL). PB1112: an ACTIVE-CLASS formal is decided on the argument's run-time class. When D composed with
E, `ActivationDescriptions.OfAddress` dropped E's `TypeRestriction.Member`, so ADDRESS OF a subordinate strong group
would match a formal restricted to the whole record. The restriction is now keyed on `TypeRestriction.Key` (golden
2002/pb1408_universal_address_of_subordinate_group). **Review fixes.** (1) An omitted OPTIONAL ANY LENGTH formal
crashed the compiler, because `ActivationRelations` returned before its ANY LENGTH arm (golden
pb480_universal_any_length_omitted). (2) Restricted pointer types were compared by name only, so two distinct types
of one name matched (§14.8.2.3.2 4)). The key is now the type's identity, in the universal description and in the
typed sibling `OoConformance` (golden pb480_universal_restricted_pointer_type, negative
pb480-typed-restricted-pointer-type). (3) A reference-modified argument with a non-literal length was described as 0
positions. The runtime now measures the evaluated slice (`CobolInvokeArg.ReferenceModified`, golden
pb480_universal_refmod_evaluated_length). The first re-gate went red on `RuntimeApiGuardTests`, because that
emission named `CobolInvokeArg.` bare in OoEmitter, so it is routed through `RuntimeApi.ObjReferenceModifiedArgument`.
(4) The EC-OO-UNIVERSAL variable-length message swapped its role labels (unit test
ActivationRelationsRoleLabelTests). Two pre-existing findings are filed: the RETURNING match ignores Table 16
(PB2076), and the typed and universal lanes disagree on a reference-modified argument (PB2077, which waits on an
owner adjudication). PB480's residue points at both. GAP 187 → 185.

**Group A: PB1425, PB1783.** Identifier Format 7 (`property-name OF identifier-3`) took only one word as its object.
`BAL OF AR(2)` drew COBOLNET2096 naming an internal temp, `BAL OF A OF T(2)` drew 1639, and `BAL OF SUPER` drew
COBOL0001. ONE resolution, `ReferenceResolver.ResolveObjectProperty`, now splits a failed qualified chain into the
property-name and identifier-3, which the ordinary resolver resolves. A `propertyObject` grammar suffix carries the
forms a word cannot spell (an object-view, SELF, SUPER, NULL, a function-identifier). §8.4.3.9.3 SR2 gets its own
code, COBOLNET2918 (the rest of COBOLNET0843's sharing is PB2072). **Review fixes.** (1) A property of an object-view
of a qualified item drew 2918, because the probe arm read the roster from the un-viewed item (golden
pb1425_property_of_qualified_view). (2) A subscript written beside a non-word property object was dropped silently;
it is now 2776 before the object and 2096 after it (two negatives). (3) `NM OF FUNCTION F (5) (2:3)` was refused, and
the trailing reference modifier now applies to the property's value, last (§8.4.3.1.4 GR1 g); golden
pb1425_property_of_function_refmod. (4) Finding 3 was an introduced wrong answer. A receiving property's GET ran
before its statement and its SET after it, each evaluating identifier-3 afresh, so `ADD 1 TO I, BAL OF AR(I)` stored
AR(1)'s BAL + 1 into AR(2): AR2 = 11, where §14.7.7 4) b) gives 21. The lander found two more shapes of it.
`MOVE 2 TO BAL OF AR(I), I` set AR(2), where §14.9.25.4 GR1 identifies AR(1), and a SIZE ERROR phrase that changed I
sent the unchanged value to the other element. The reviewer's snapshot fix would identify AR(I) at the statement's
start, which is also wrong. The correct shape interleaves each receiver's accessors with its own store, which is a
redesign of the bound receivers. **The lander chose a narrow refusal over dropping the cluster**, because the
refusal is about 30 code lines, it restores base's rejection for exactly that shape, and the rest of the cluster
(including three verified fixes) is sound. A receiving property whose identifier-3 selects its object through a
run-time value (a data-name subscript, or a function-identifier) is COBOLNET0899
`receiving-reference-shape-not-implemented` (`OoBinder.OoWrapPropertyOps`, with the flag set by
`ReferenceResolver.SelectsByValue`). Literal subscripts and every sending reference stay admitted (negative
pb1425-property-receiver-selected-by-value). The redesign, and the pre-existing unsubscripted sibling `ADD 1 TO BAL
OF D, BAL OF D` (101 where 102 is due), are PB2078. GAP 185 → 178.

**Group B: PB781, PB858, PB1415, PB1039, PB1755.** A RESERVE clause on a LINE SEQUENTIAL file is refused by
§12.4.5.2 SR12 (COBOLNET2921). The DEPENDING item of RECORD IS VARYING is screened by three RecordClauseRules rows
for §13.18.43.3 SR6 (COBOLNET2922). The ALL half of §8.3.3.6.3 SR1 (only ZERO without ALL where a numeric literal is
required) is gated from 2002. PB1039 was already fixed by PB1947, so only its witness negative is added. PB1755 fixed
the harness's copybook staging race. **Review fix.** A trailing-P depending item (PIC 99P) was refused 2922, although
§5.5 2) b) 2. makes it an integer (§13.18.40.4 GR14). `RecordClauseRules.IsUnscaledInteger` and its two pre-existing
siblings (`DataBinder.Odo`, 0852; `DataBinder.Reports`, 2021) now admit it (goldens 2023/pb858_record_depending_p_scaled
and pb858_odo_depending_p_scaled). GAP 178 → 175.

**Group B2: PB1042.** Cell-backed areas (EXTERNAL, ADDRESS OF) now carry dynamic-capacity tables and per-occurrence
dynamic-length items through one model, `Place.CellComponents`, with element cells as nested scopes. The string
`TablePath` twin is deleted. An INITIALIZE stride over a NATIONAL REDEFINES table was fixed on the way. **Review
fixes.** (1) A table VALUE under a fixed OCCURS inside a dynamic table was lost in the cell lane (`[  ]` where `[AA]`
is due), because the CellComponents walk dropped the fixed occurrence numbers. (2) An EXTERNAL INITIALIZED dynamic
table repeated occurrence 1's VALUE in every occurrence. Goldens 2014/pb1042_cell_table_value_under_fixed and
pb1042_external_initialized_table_value. An unverifiable third candidate is the analysis note PB2080. GAP 175 → 173.

**The train.** Each cluster's patch was applied from its branch, and its verdict batches were re-applied with
`record_verdicts.py` (0 row differences from the branch). The review fixes were applied to a rebuild from
17924fbb4, one commit per cluster. The first full gate after the rebuild was red on one test, `RuntimeApiGuardTests`
(D's review fix). That was fixed in D's commit. The re-gate was GREEN: Conformance 10,707/10,707 · Unit 31,724/31,724 · Characterization 35/35 (run 20261005T233050Z-596805), with no external-corpus fetch failure. semgrep verify PASSED with no count raised. The legacy
Integration assembly passed 503 of 504 with one skip. The Linux gate was GREEN on all four legs (unit, characterization, conformance, guard; NIST 362 MATCH, 0 regressions). The review covered all seven clusters: 14 findings and 2
candidates were verified by read-only adjudicators with repros. Eleven are fixed in their clusters, A3 by the narrow
refusal, and four are filed as notes (PB2076, PB2077, PB2079, PB2080). No cluster was dropped. Leads from the
implementers' reports: PB2071 (an unused STRONG typedef holding REDEFINES, adjudication), PB2072 (0843 still carries
SR1, SR3 and SR4), PB2073 (a function-identifier property object unseen by the resolver probe) and PB2074 (a
`--permissive` address-identifier reaching IntrinsicRenderer's LoudValue). **GAP 194 → 173.**

