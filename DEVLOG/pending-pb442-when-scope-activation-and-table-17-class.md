## PB442: an exception-checking PERFORM's WHEN stops at the activation, and Table 17 reads the receiver's class

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
`CobolModule.Push`/`Pop` are deleted. The shipped surface lost members, so the runtime major goes from 4 to 5. The
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
