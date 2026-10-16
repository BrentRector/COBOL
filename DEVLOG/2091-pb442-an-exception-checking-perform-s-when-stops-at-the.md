## Entry 2091 — 2026-10-16 10:23 PDT — PB442: an exception-checking PERFORM's WHEN stops at the activation, and Table 17 reads the receiver's class

PB442 was filed as an analysis note: three inventory rows held PARTIAL on an evidence burden, not on a measured
wrong answer. Re-probing every claim on this tree found three wrong answers and one crash behind it.

**The scope of a WHEN phrase.** §14.9.28.4 GR17 says "during the execution of imperative-statement-1", §9.1.12 1)
"an active PERFORM statement", and §14.6.13.1.4 2) "the executed statement is within imperative-statement-1". The
note read the implementation as applying two readings: dynamic for a paragraph PERFORMed from imp-1, lexical across
a CALL. The standard settles it as one rule. An exception status indicator "exists for each function, method, or
program" (§14.6.13.1.1), and a condition reaches an activator only by propagation (GOBACK / EXIT … RAISING raise it
"in the activating runtime element", §14.9.18.4 GR1 b), at the CALL or INVOKE, §14.9.33.4 GR2 a) 2.). So the reach is
the PERFORM's dynamic range within its activation, bounded by that activation. It is now determination D-F3S in
docs/CONFORMANCE.md §3. The implementation honoured the boundary only by accident. The CALL boundary trimmed the frame
stack on return and hid nothing during the call, and the frame floor was raised only by a method that itself held an
exception-checking PERFORM. A CALLed program holding such a PERFORM therefore ran the CALLER's WHEN inside itself
(CEE-RAISE, Q2-WHEN-WRONG, CEE-AFTER), and so did an ordinary method of a class holding one, at an I-O raise
(DRV-WHEN-LEAK). The fix is one mechanism. Each WHEN frame records the activation that pushed it
(`PerformFrame.Activation`, stamped by `ExceptionEngine.PushPerformFrame` from `ModuleStack.CurrentActivation`), and
`RunTopFrame` stops at the first frame of another activation. Every activation mechanism already pushes a ModuleStack
frame, so none can forget the boundary. The floor, `TrimPerformTo`, `PerformDepth`, the CallProgram snapshot, the
OoEmitter method-floor emission and the dead forwarders `RuntimeApi.ModulePushMethod`, `ModulePop` and
`CobolModule.Push`/`Pop` are deleted. The shipped surface lost members, so the runtime major goes from 5 to 6 (PB651 took main from 4 to 5 first, and a module compiled at 5 still calls the deleted floor members). The
registrar's §9.1.12 refutation (an I-O statement in a PERFORMed paragraph reaches the WHEN only under `>>TURN`) is
the standard's enablement rule: §9.1.12 applies "When execution of an input-output statement raises an exception
condition", and §9.1.13.1 makes raising depend on enablement, which a TURN directive gives the text after it.

**Table 17 reads the CLASS.** COBOLNET0899 has been gone since PB646, so the numeric-edited USAGE NATIONAL cell is
reachable. A non-native alphanumeric program collating sequence (`ALPHABET IS "2" "1"`) separates the two LOW-VALUEs
by value. Measured: a `PIC Z9 USAGE NATIONAL` receiver took the alphanumeric LOW-VALUE; two such items compared under
the alphanumeric sequence (§8.8.4.2.9 says national); and INSPECT's private class projection missed the class as
well. Table 2 (§8.5.2.1) puts numeric-edited "if usage is national" in class national. The projection is now written
once, `IntrinsicArgumentRules.CharacterContext`, and asked by MOVE, VALUE, the CALL/INVOKE fill, the group fill and
INSPECT; `CollatingSelection.OperandCategory` answers National for that one operand. **The crash:** below 2023,
`MOVE LOW-VALUE TO` a `PIC 9 USAGE NATIONAL` item stopped the run unit with NotImplementedCobolFeatureException
("without image-backed storage"), and a reference-modified store into one was silently lost. Both image marks in
`MoveBinder` now share `IsCharacterImageNumeric` (display or national).

Goldens: `2023/pb442_f3_when_activation_scope`, `2023/pb442_f3_when_method_scope`, `2023/pb442_f3_when_io_enablement`,
`2002/pb442_numeric_edited_national_class` (all expectations derived from the cited rules; each fails on the old
build). Rows GR-14.9.28.4-17, GR-14.9.28.4-1, GR-9.1.12-1 and GR-14.9.25.4-7 went PARTIAL → CONFORMS (GAP 98 → 94).
Docs: CONFORMANCE §3 D-F3S, conditions/exceptions design D12 (d), the as-built SSOT §9.2/§9.10.1-C2, the EC-OO
super-batch summary, STRING_OPS INSPECT entry. Not widened, reported as a lead: the other `Usage.Display`-only
numeric image marks in DataBinder (whole-group union, Tier-B leaves, reports, linkage).

**Branch review round 1 (three findings, all docs and register).** The as-built SSOT §9.2 code block still showed
`PerformDepth`/`TrimPerformTo` and a `RunTopFrame` with no activation stop, under a paragraph that said the opposite;
the block now mirrors the code (the `Activation` stamp in `PushPerformFrame`, the `break` at the first foreign frame).
The same doc's banner and §9.7 still listed F3-in-a-method, the cross-CALL reach and `>>PROPAGATE` as staged, and
its §8 scout map and §9.8 increment row described the deleted snapshot; all four now say what is built. The EC design
doc's "Still later waves" paragraph listed the F3 WHEN and `>>PROPAGATE` as future work; it now names the one staged
sub-form (an exception-object raise in imp-1). Sweeping that paragraph found two more stale claims, fixed with it:
"F3 declaratives are not yet GLOBAL-walkable" (a Format 3 USE cannot carry GLOBAL, §14.9.49.2; CA12 was refuted on
exactly this), and "VALIDATE is implemented minimally" (it is the declined A.4.14 facility, inert with COBOLNET1580),
which `COBOLNET_DESIGN.md` §18 item 17 also said. The super-batch map and the runtime-library example named the
deleted members too. kb/Work PB2181 now records that PB442 deleted two of its six sites and its `CobolModule` lead.

**The finisher's first gate was RED on a drift inventory the branch had not updated.** On the merged tree
`DisplayUsageUnionDriftTests.HandRolledDisplayUnion_IsTheKnownInventory` still listed MoveBinder's two image marks
as DISPLAY-only, the column PB646 chose for carriage marks. The implementer's leg 1 had not reached that case.
`IsCharacterImageNumeric` now asks the display/national pair, which is the PB442 crash fix, so the inventory records
it as one PAIR site. The reason is §13.18.60.4 GR8: a national numeric item HAS the character cells both marks
write. PicInfo's IMAGE-vs-CARRIAGE comment now names which carriage sites admit national and which stay
DISPLAY-only. PB646's warning that promotion corrupts a group-to-group MOVE was about the whole-group promotion,
so it was probed for the single-leaf mark. Group MOVEs ran between two alphanumeric groups of national numerics and
between two GROUP-USAGE NATIONAL groups, with both leaves forced, one forced, and an ALL "7" deposit. Every value
and class test matched the unforced twin. Golden `2002/pb442_forced_national_leaf_group_move` (§14.9.25.4 GR4)
pins it.

**The landing's Linux gate was RED on an inventory witness the branch had orphaned.** Deleting INSPECT's private
class projection (`InspectEmitter.CharacterCategoryOf`, folded into `IntrinsicArgumentRules.CharacterContext`) left
row GR-8.3.3.6.4-7 naming the deleted member, so `SpecTraceabilityInventoryDriftTests.EveryCodeLocation_ResolvesInTheTree`
failed. Main had not touched the file or the row; the defect was the branch's own. A `record_verdicts.py` batch
(`w1091h-PB442-finisher2-resite`) retired the dead witness and named `IntrinsicArgumentRules.cs#CharacterContext` and
`InspectEmitter.cs#Emit`; the verdict stays CONFORMS. Every other inventory code-location resolves. The Windows leg 1
never ran the test: its class is a tree-wide sweep, but its recorded 8.6 s is above the 2 s cheap-sweep bound, so
with no impact map it was tier 1 and fell outside leg 1's budget.

