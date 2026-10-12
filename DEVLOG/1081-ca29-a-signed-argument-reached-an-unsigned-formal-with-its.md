## Entry 1081 — 2026-07-28 14:45 PDT — CA29: a signed argument reached an unsigned formal with its sign intact

Step 2 of 11 in the EC-infra + OO super-batch. Track E again — a one-clause compile-time guard in OoEmitter,
no EC machinery touched.

**The rule, in two hops.** §14.8.2.3.3 rule 2a: "If the formal parameter is numeric, the conformance rules are
the same as for a COMPUTE statement with the argument as the sending operand and the corresponding formal
parameter as the receiving operand." COMPUTE's store is §14.9.25.4 GR6d2b: "When an unsigned numeric item is
the receiving item, the ABSOLUTE VALUE of the sending value is used, and no operational sign is generated for
the receiving item." Both read verbatim before any code.

**The gap.** The BY CONTENT numeric arm converts only when `Digits` or `Scale` differ. An argument that
matches the formal on both and differs ONLY signed-vs-unsigned therefore fell to the plain arm, which emits
`({ElementType})(value)` — and `ClrType` is `long` for EVERY fixed-point usage, so −7 was copied verbatim into
an unsigned formal.

**The trap in the symptom, and why the golden asserts a comparison rather than a DISPLAY.** Without the fix
the golden prints `BAD=0007` — the image has NO sign, because an unsigned PICTURE cannot render one, while the
stored value is −7 and `IF LK-U = 7` fails. The defect is invisible to DISPLAY and visible only to arithmetic
or comparison. A golden that eyeballed the rendering would have passed.

**The sweep, which came back NARROWER than the finding.** The guard is a whitelist of "differences that
matter", so rather than add one more clause by eye I derived the identity test from what
`CobolNum.Store` actually consults: `FractionScale`, `Digits`, `Signed`, and `Truncation`. The fourth looked
like a second live hole — a `BinaryCapacity` (COMP-5 / BINARY-CHAR) formal reaching the plain arm skips
`WrapBinary`, and the C# cast does not substitute for it since every fixed-point usage projects to `long`. But
`WrapBinary` is an IDENTITY whenever the digit counts match, because COMP-5's binary capacity always exceeds
its decimal digit range — and when digits differ the existing guard already converts. So sign really is the
only missing axis. The check was worth doing: it is the difference between "added a clause" and "know the
clause is complete".

**Tighter than the finding proposed.** The queue entry suggested `Signed != Signed`. That also converts
UNSIGNED → SIGNED, which GR6d2a makes a provable identity ("If the sending operand is unsigned, the sign shall
be positive" — the value is unchanged). The condition landed is `sender.Signed && !formal.Signed`, matching
GR6d2b exactly and leaving the identity case on the cheap path, so no generated code churns for it.

**Golden** `2002/oo_invoke_content_unsigned_formal`, registered same commit. A NEGATIVE argument is required —
a positive one passes either way — and the second call passes +7 as the control that the new conversion does
not disturb the case that already worked. Expected `SEVEN` twice; verified `BAD=0007` / `SEVEN` without the
fix. Proved the harness reads it by breaking the `.out`: fails exactly
`EnabledProgram_CompilesStrict_AndMatchesOutIfPresent(edition: "2002", name: "oo_invoke_content_unsigned_formal")`.

**Gate (wave-local):** characterization 33/33 · Conformance CorpusRunner+Oo 558/558 · CorpusRunnerTests 347/347.

**Noted, not actioned:** the finding flags the BY VALUE numeric path and CALL BY CONTENT (CallEmitter) as
siblings; it also records that CallEmitter has NO analogous NumStore guard and marshals by a different
mechanism, so it is a separate check rather than a shared-guard edit — out of CA29 scope and still open.

