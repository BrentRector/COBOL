## Entry 1583 — 2026-09-09 15:15 PDT — Golden round for PB258: six of seven CALL/DISPLAY/DIVIDE rows close on spec-derived goldens, SR-14.9.4.3-5 restated on a measurement nobody had taken

**GAP 2713 -> 2707 of 4,348.** `kb/Work PB258`'s seven inventory rows were the second family of the same
mechanism `PB245` recorded: a verdict that was true when it was written and was never re-derived after the fix
landed underneath it. The registration wave had REFUSED to flip them on a read. This round paid the debt the way
the contract requires - every rule re-quoted from `specs/ISO_COBOL.md` and validated with `cite.py --check`
BEFORE the expected value was derived, every derived value written into a golden's header before the golden was
run, every golden run on a compiler built in this worktree. **Six rows measured exactly what was derived and
close CONFORMS; the seventh stays PARTIAL, and it stays PARTIAL for a reason no read would have found.**

**`GR-14.9.4.4-1`** (CALL, PARTIAL -> CONFORMS). Rule: "The instance of the program, function, or method that
executes the CALL statement is the activating runtime element" (`--check 14.9.4.4` -> OK, GR 1). Derived: the
activating element is an INSTANCE, so control returns to THAT instance with its own automatic data and a
CONTAINED callee reaches its container's GLOBAL storage PER ACTIVATION. Both halves of the row's refutation had
landed under `PB133` - `ProgramTable.CallProgram` restores the displaced instance in its `finally`, and
`BinderDriver.MakeUnit` honours ISO 11.10.4 GR4 so a containee of a RECURSIVE container is per-activation.
Measured with the exact composition the row named as what would close it: new golden
`conformance:2002/l1_call_activating_instance_per_activation` (RECURSIVE container, level-1 `GLOBAL`
LOCAL-STORAGE item, contained callee at two depths) printed the derived eight-line trace, whose last two lines
discriminate a stale registry slot (`C-SEES=22` last) from a once-bound container handle (`C-SEES=11` middle).

**`SR-14.9.4.3-9`** (CALL, PARTIAL -> CONFORMS). Rule: "Identifier-3 is a receiving operand" (`--check
14.9.4.3` -> OK, SR 9). Derived: the classification makes every "shall not be a receiving operand" rule bear on
identifier-3, ISO 13.18.15.3 SR2 among them. `CallBinder.BindCall`'s `callReturningPhrase` arm now rides
`ExpressionBinder.ResolveReceiving`, so `PB128`'s routing finally reached the SECOND arm of the dispatch.
Measured COBOLNET1548 at 2002, 2014 and 2023; new fixture
`conformance:negative/l1-call-returning-constant-record`, the twin of the BY REFERENCE fixture whose name is
what made the two-arm question worth asking.

**`SR-14.9.11.3-1`** (DISPLAY, **DIVERGES** -> CONFORMS) - the row where the stale verdict was the HARMFUL
direction: the inventory published a DIVERGES against a compiler `PB148` had already fixed. Rule:
"Identifier-1 shall not reference a data item of class message-tag, object, or pointer" (`--check 14.9.11.3` ->
OK, SR 1). Derived from ISO 8.4.3.11.4/8.4.3.12.4/8.4.3.13.4 GR1: "class pointer" spans data-, function- and
program-pointer, so USAGE POINTER, USAGE PROGRAM-POINTER and NULL are excluded alongside class object. The fix
landed as the CLASS-SET screen the divergence argued for - `ExpressionBinder.ScreenOperandClass` over the ONE
classifier - not a per-usage test. Measured COBOLNET1694 for all four shapes at 2002/2014/2023. Three new
negatives plus `conformance:2002/l1_display_admitted_classes`, which holds the admit side so the screen cannot
over-reject unnoticed. Two arms stay VACUOUS and are recorded as such rather than claimed as tested: class
message-tag has no Usage member at all, and category function-pointer is the pending construct
`usage-function-pointer-2014`.

**`FMT-14.9.11.2`** (DISPLAY general formats, PARTIAL -> CONFORMS). Format 1 re-derived element for element
against the printed format and today's `displayStatement` production, and pinned by new golden
`conformance:2002/l1_display_format1_elements` - a mixed identifier/literal operand LIST, GR3's single
figurative occurrence, a function-identifier operand, and `NO ADVANCING` written with and without the optional
`WITH`. The END-DISPLAY edition asymmetry the row recorded is closed by `PB134`'s ACTIVE `end-display-2002`
construct row: measured COBOLNET0816 at `--std 85`, clean at 2002/2014/2023. Format 2 (screen) is refused BY
NAME with COBOLNET1707 under `PB260`, which is a documented Annex A.4.1/A.4.2 decline - **unreachable rather
than unverified**, the same disposition `GR-8.8.1.2-6` already carries.

**`GR-14.9.12.4-1`** and **`GR-14.9.12.4-7`** (DIVIDE quotient and remainder, PARTIAL -> CONFORMS). Six values
hand-derived from GR1/GR2/GR6c/GR7 with ISO 14.7.7 rule 3 NOTE 1 confining ROUNDED to the final transfer -
`A=3333 B=13 C=014 002 D=333 001 E=67 020 F=SIZEERR 777` - measured identical under native arithmetic
(`conformance:85/l1_divide_quotient_remainder_native`) AND under `ARITHMETIC IS STANDARD-DECIMAL`
(`conformance:2014/l1_divide_quotient_remainder_standard_decimal`), which is the SECOND sentence each rule
states and the half a native-only test leaves unwitnessed. `E=67 020` is the discriminator: the stored quotient
is ROUNDED to 6.7 while GR6c's subsidiary quotient is TRUNCATED to 6.6, so the remainder is 0.20 and not -0.10.
GR7's two other residues had landed and are now EXECUTED rather than read - the GR6c digit cap
(`RuntimeApi.NumCapDigits`) and the checked back-multiply, the latter measured on the row's own overflow shape
(`conformance:2014/l1_divide_remainder_backmultiply_size_error`, composite 26 of 31, unscaled product ~10**40)
raising the size error and leaving identifier-4 unchanged where a silent Int128 wrap would have taken the NOT ON
SIZE ERROR arm. The standard-binary sentence is `PB198`'s 4.2.6 decline, now pinned by
`conformance:negative/l1-divide-arithmetic-standard-binary` (COBOLNET0806). Both are PER-ROW adjudications and
do NOT widen `inventory-schema.json`'s `derived-verdicts.standard-binary-only` selector.

**`SR-14.9.4.3-5`** (CALL, PARTIAL - RESTATED, not closed). Rule: "If the BY REFERENCE phrase is specified or
implied for an identifier-2 and identifier-2 is not an address-identifier, it is a receiving operand" (`--check
14.9.4.3` -> OK, SR 5). Both residues the row carried had landed AND neither was this rule's: the object-data
ban is SR3 sentence 2 (`pb132-call-byref-object-data`, COBOLNET1678) and the once-only half is GR3a
(`pb133_gr3a_once_only`); the Format-2 bare-argument mode is now read off the corresponding formal per GR9
rather than threaded from a preceding BY phrase. A read would have closed the row there. **The measurement did
not.** The rule's condition carries an EXCEPTION - "and identifier-2 is not an address-identifier" - and that
arm cannot be written at all: `CALL "S" USING BY REFERENCE ADDRESS OF W-X` and the `BY CONTENT` spelling are
both COBOL0001 parse errors, while SR3 explicitly admits an address-identifier as identifier-2. That is legal
source rejected; it is already held as their own residue by `SR-14.9.4.3-3` and `SR-14.9.4.3-4`, and it is why
this rule's discriminating case has no source form. The rule's implemented content is now pinned anyway by new
golden `conformance:85/l1_call_by_reference_receiving_operand` (`REF=0015` explicit, `BARE=0025` implied,
`CON=0030` the SR4 sending contrast).

**Twelve new corpus cases**, all CRLF/UTF-8/no-BOM/no-NUL and byte-checked after the scripted normalization,
registered `enabled` in four manifests. **The corpus runner was made to FAIL once before it was trusted**: the
discriminating `E=67 020` line was corrupted to `E=67 010` and the run came back
`Failed: 1, Passed: 1831, Total: 1832` naming `l1_divide_quotient_remainder_native`, then the line was restored.
**No diagnostic code was claimed** - the fixtures reuse COBOLNET1694, COBOLNET1548 and COBOLNET0806.
`kb/Work PB258`'s `inventory_rows` shrinks to `["SR-14.9.4.3-5"]` and the note stays `open` on it plus the
MECHANISM it exists to record: there is still no back-link that fires when a note LANDS while its rows stay
defective, and this round measured what that costs - six of seven rows stale, one of them stale in the direction
that publishes a DIVERGES against conforming code.

