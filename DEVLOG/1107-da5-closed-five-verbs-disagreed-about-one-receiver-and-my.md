## Entry 1107 - 2026-07-29 15:40 PDT - DA5 closed: five verbs disagreed about one receiver, and my "bytes are not text" caution was misapplied

The fan-out came back. All ten agents completed, zero errors - the platform incident had cleared - and the result
contradicted my own prediction, which is the most useful thing a review can do.

**WHAT I PREDICTED, AND WHY IT WAS WRONG.** Twice in writing - in the queue entry and in entry 1106 - I argued the
four remaining group-receiver guards were probably CORRECT: STRING, UNSTRING and INSPECT are defined over CHARACTER
POSITIONS, and V59's own governing lesson is that a COMP leaf's image is radix-2 bytes rather than its digits, so
feeding those bytes to a character operation would be a silent wrong answer - strictly worse than a loud stage. I
was confident enough to defer the work rather than guess at it.

Five of six came back STALE, and every one survived adversarial refutation. So I checked it myself instead of
believing either side, and the disproof is one line long:

    MOVE SRC TO G                          -> N=8501 A=LLO
    STRING SRC DELIMITED BY SIZE INTO G    -> THROWS
    UNSTRING SRC INTO G                    -> THROWS
    INSPECT G REPLACING ALL " " BY "x"     -> THROWS

Same receiver, a group of one BINARY leaf and one PIC X leaf. The group MOVE already worked. So the question was
never "may a character operation touch these bytes?" - MOVE settles that it may, under §14.9.25.4 GR4, which makes
a group move alphanumeric and "filled without consideration for the individual items". The question was only
whether five verbs may disagree about one receiver, and they may not.

My error was applying the right rule to the wrong operation. "BYTES ARE NOT TEXT" governs *RENDERING a COMP leaf's
VALUE as text* - that is what `DisplayTextWidth` exists for, and DA2 is entirely about it. Writing characters
POSITIONALLY over a group's bytes is a different operation, and it is exactly what a group MOVE does. I had the
distinction in hand and reached for the wrong side of it.

Worth being precise about what the caution got right, though, because the lesson is not "trust the fan-out". It
refused a blind predicate swap and demanded a per-site derivation - and that derivation is what proved the swap
correct. Right in kind, wrong in application.

**LANDED.** `StringEmitter.cs:151` and `:193`, `InspectEmitter.cs:89`, `AcceptDisplayEmitter.cs:66` and `:129` all
move to `IsImageCapable`, and the leaf-kind wording drops its `"COMP/binary"` override so the surviving message
names the predicate actually tested. Verified both directions: the four verbs now land byte-for-byte what the MOVE
lands, and a float/COMP-5/INDEX group still stages loud on all four with the truthful `float/COMP-5/INDEX` wording.

The golden pins the INVARIANT rather than the absence of a crash: `MOV`, `STR` and `UNS` must be IDENTICAL lines,
because STRING and UNSTRING are the same positional transfer the MOVE is. The INSPECT case is the sharpest - the
binary leaf holds 0, whose bytes `00 00` are not spaces, so REPLACING ALL " " must leave the leaf untouched and
change only the text tail (`INS N=0000 A=xxx`). A golden asserting merely "no longer throws" would pass over a
verb that scribbled on the wrong bytes.

**A STALE COMMENT IN CODE V59 ITSELF TOUCHED, which the review also caught.** `OperandText.cs` still described a
native BINARY/PACKED leaf as contributing "its zoned decimal digit image ... with a trailing-overpunch sign". That
was true of the PRE-V59 image and V59 falsified it - the bytes are not digits. Corrected rather than left, because
a comment describing the old representation is precisely how the two-predicate residue spread in the first place.

**TWO GENUINELY DIFFERENT DEFECTS SEPARATED OUT AND QUEUED.** Neither is a predicate residue, which is why neither
is fixed here.

DA6 - a GROUP used as a NUMERIC operand. §8.8.1.1 admits only "an identifier referencing a numeric data item", and
a group is class alphanumeric, so `COMPUTE R = G + 1` is illegal source. COBOL.NET does two different wrong things
with it: a group of `PIC X` leaves COMPILES AND COMPUTES (`R = 001235`), while a group of `PIC 9` leaves compiles
and THROWS at run time. The group whose digits are unambiguous fails and the merely-textual one succeeds - the
opposite of intuition, and neither is a compile-time rejection. ⛔ This is exactly why `NumericRenderer.cs:186` is
DELIBERATELY left on the old predicate: migrating it would make the second case silently compute too, extending
acceptance of illegal source rather than fixing anything. The drift test carries that note. Resolution needs an
owner call on the leniency axis (reject strictly with `--permissive` leniency, or accept uniformly as a documented
extension), because group-as-numeric is a common vendor extension.

DA7 - three syntax-rule violations diagnosed at RUN TIME rather than COMPILE time (`InspectBinder.cs:35`,
`StringEmitter.cs:168`, `:226`). Every one of those constructs is genuinely ILLEGAL, so no conforming program is
rejected and the compiler's VERDICT is right; only the STAGE is wrong. The `InspectBinder` one is cheapest because
it is already in the binder, where a diagnostic is immediately available and the `BoundUnsupported` is a pure
staging choice. Needs `COBOLNET1626`, the four-edition sweep and negative fixtures.

**Gates.** Greenfield Unit 942/942, zero skipped. FULL Conformance re-run with the new golden.

