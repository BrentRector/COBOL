## Entry 1106 - 2026-07-29 14:45 PDT - The table-SORT half of the V59 residue, a correction to my own last entry, and doing it by hand because the fan-out was down

Continuing DA5. Two of the sites resolved, in opposite directions.

**SortEmitter WAS a residue, and the argument for it is the strongest of the family.** `TableCompare` loud-staged a
group sort key containing a BINARY leaf: "table-sort key 'K' over a mixed-usage group - no whole-group character
image". False, exactly as the CALL one was - `AsImage()` is emitted for that group.

What makes this case airtight rather than merely plausible is **14.9.40.4 GR8** (cite.py-verified): key data items
are "compared according to the rules for comparison of operands in a relation condition". A GROUP operand in a
relation is class alphanumeric (8.8.4.2.3 SR2) compared over its REPRESENTATION, and `OperandText` already renders
precisely that through `AsImage()` gated on `IsImageCapable` - its own comment names the compare path. So the stale
predicate did not merely refuse something: it made **SORT and IF disagree about the same two group operands**. The
IF compared their byte images and answered; the SORT threw. GR8 exists to forbid exactly that divergence.

Verified both directions. `SORT E ON ASCENDING KEY K` over keys 300/100/200 now returns aaa, bbb, ccc. And with a
NEGATIVE key: `IF FK(1) > FK(2)` answers NEG-GT-POS (bytes FF FF beat 00 01) and the SORT places POS first -
consistent. The golden pins the AGREEMENT rather than just "the sort runs", because agreement is the actual
requirement.

⚠ **That negative case is a documented consequence, not a bug, and the golden exists to protect it.** A big-endian
two's-complement image is NOT order-preserving across zero, so a group key holding -1 sorts AFTER one holding +1.
That is what the standard prescribes for a GROUP key - GR8 defers to the relation rules, and those make a group
alphanumeric - and a program wanting value order names the ELEMENTARY numeric item as the key, which takes the
by-value arm. Without a golden case someone will eventually "fix" the byte order and silently break GR8.

**⛔ A CORRECTION TO ENTRY 1105.** I wrote there that "MoveEmitter used BOTH predicates in one file - a distinction
does not usually disagree with itself", and listed `MoveEmitter.cs:144` among the stale guards. That was wrong, and
reading the code properly is what showed it. Line 144 is not a Tier-C guard at all: it selects a STRATEGY. When the
receiver is not a character image it first tries the memberwise leaf-copy fast path - used when source and receiver
leaf layouts are positionally identical - and only the fall-through reaches the real capability guard at line 168,
which was ALREADY on `IsImageCapable`. Verified by repro: MOVE into a COMP-containing group works in all three
shapes (aligned memberwise; non-aligned, where the bytes redistribute positionally per 14.9.25.4 GR4; and from an
alphanumeric source). So the two uses have different jobs and the file was never inconsistent. The count is four
remaining sites, not seven, and the drift test now carries a comment so nobody migrates a working fast path.

That mistake is worth keeping visible because of how I made it: I inferred "same file, two predicates, therefore
unfinished migration" from a grep, and the grep could not see that the two call sites answer different questions.
The CALL and SORT findings were real; this one was pattern-matching.

**FOUR SITES REMAIN AND ARE STILL NOT SWEPT** - `NumericRenderer:186`, `AcceptDisplayEmitter:66,129`,
`InspectEmitter:89`, `StringEmitter:151,193`. STRING, UNSTRING and INSPECT are defined over CHARACTER POSITIONS, so
refusing a byte-imaged COMP group is quite possibly CORRECT: V59 gave those leaves bytes, not text, and feeding
radix-2 bytes to a character operation is a silent wrong answer, strictly worse than a loud stage. Each needs its
own derivation, plus the separate question of whether a syntax-rule violation is being deferred to run time when it
belongs at compile time.

**⛔ THE FAN-OUT WAS DOWN, AND THAT CHANGED HOW THIS GOT DONE.** Three successive multi-agent audits lost EVERY
agent to API 529 - nine, nine, then three - with zero tokens and zero tool calls each. The owner then confirmed a
platform incident ("elevated errors across multiple models"). The main loop was fine throughout; only subagent
spawning failed. So the CALL, SORT and MoveEmitter findings above were all derived by hand, and the four remaining
sites are the ones that did not get done. That is the honest accounting: the outage did not corrupt any result, it
reduced coverage, and the queue says which four.

One durable lesson from the first of those failures, already fixed: its synthesis fallback reported "every
IsCharacterImage use is a correct text-context guard" when NOTHING had run, because the branch could not
distinguish an empty answer from an absent one. Both later runs correctly said INCONCLUSIVE.

**Gates.** Greenfield Unit 942/942, zero skipped. FULL Conformance re-run with the new golden.

