## Entry 1089 — 2026-07-28 17:35 PDT — V55: EC-OO-UNIVERSAL's "enabled in BOTH" gate, and three defects it exposed

Step 9. The first finding in the batch with real design content rather than a recipe; both owner decisions from
earlier in the session land here.

**The rule, validated with `cite.py` first.** §14.9.23.4 GR7c: a violation of the universal-INVOKE conformance
rules sets EC-OO-UNIVERSAL "if checking for it is enabled in BOTH the activated method and the activating
runtime element, the method invocation is not successful, and execution continues as specified in General rule
7g."

**The two halves live in different places, which is the whole difficulty.** The ACTIVATOR's half is run-unit
state — `ExceptionState.OoUniversalChecking`, set by the emitted statement guard around the INVOKE and read by
the callee's `__CobolInvoke`, which runs synchronously on the same run unit. The METHOD's half is a property of
the CALLEE's SOURCE and can never be a flag, so it is folded at BIND time from the group's line-keyed TurnState
at the METHOD-ID line and baked as a compile-time literal (`OoMethodSymbol.OoUniversalCheckingHere`). Folded in
the binder, not the emitter, per the owner decision: codegen owns no TurnState, and reading one there would be
a second mechanism for the binder's job.

**Not-enabled-in-both stops WITHOUT attributing anything.** The new `CobolImplementorFatalException` carries NO
EC name, so no statement guard's `EcName ==` match can select it — the §14.6.13.1.1 NOTE 3 latitude, made
unmistakable by the type rather than by an empty string.

**⚠ THREE DEFECTS FOUND BY RUNNING IT, none predicted by the finding.**

1. **The bind-time fold crashed ELEVEN goldens.** `m.Ctx.Start` is a NullReferenceException for a PROPERTY
   accessor SYNTHESIZED from a PROPERTY clause (§13.18.42) — no METHOD-ID in source, so no method context — and
   it took down the whole bind, not just the accessor. Now falls back to the enclosing CLASS-ID line, the right
   query point since the accessor is part of that definition and has no source position of its own to disagree
   with.
2. **The non-attributing stop escaped as a RAW .NET unhandled exception**, stack trace and exit 127, because
   `RunMain` catches `CobolFatalException` and `CobolCallException` but knew nothing of the new type. A .NET
   stack trace is not a diagnostic a COBOL programmer can act on — the runtime twin of emitting a Roslyn CS
   error on generated user source. Caught at the run-unit boundary now, reported without an exception-name
   because there is none.
3. **Three `OoSpineTests` asserted the pre-GR7c behaviour.** `UnivHazard` builds its program with NO `>>TURN`,
   so under GR7c nothing may be attributed — yet the tests asserted the EC NAME appears. They passed only
   because the raise used to be unconditional. Fixed WITH the spec, not to fit the code: the harness now carries
   `>>TURN EC-OO-UNIVERSAL CHECKING ON` at the top of the group, where it covers the program AND the classes, so
   the tests exercise the enabled-in-both path and their name assertions become legitimate.

**Both arms covered, as GR7c requires.** Corpus golden `2023/ec_oo_universal_both` — a PIC 9(6) argument into a
PIC 9(4) formal through a universal receiver, checking on for both, declarative selects and RESUMEs. Behavior
test `OoUniversal_EnabledInActivatorOnly_StopsWithoutAttributingTheCondition` — the same program with the
directive switched OFF before the CLASS, asserting the run stops AND that the message does NOT contain
EC-OO-UNIVERSAL. That negative assertion is the point of the test; attributing it would be the bug.

**Gate:** characterization 33/33 · Conformance CorpusRunner+ExceptionCondition+Oo 618/618 · Unit
flags-drift+Oo 119/119.

**Left, named:** the fifth EC-OO-UNIVERSAL throw site is `SET … TO EXCEPTION-OBJECT` (the universal narrow),
which the finding lists only "for pattern consistency". GR7c governs INVOKE; I have no validated citation that
it governs that SET, so it is deliberately NOT gated rather than gated on an invented reading. EC-OO-METHOD
also still has no golden — reachable only through a universal receiver whose method the class does not define.

