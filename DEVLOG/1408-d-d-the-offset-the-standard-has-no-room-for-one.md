## Entry 1408 — 2026-08-31 01:52 PDT — D-D: the offset the standard has no room for — one determination, one formatter, and a scope that had to be pinned rather than asserted

§15.21.3 rule 1 lays out CURRENT-DATE's 21 characters position by position, and positions 18-19 carry the UTC
offset in hours: "00 through 13" when position 17 is '+', "00 through 12" when it is '–'. §15.99.3 rule 1 repeats
that table character for character for WHEN-COMPILED. Both bounds are wrong about the world. UTC+14:00 is a
shipping zone on both platforms this compiler targets — IANA `Pacific/Kiritimati`, Windows `Line Islands Standard
Time`, display name "(UTC+14:00) Kiritimati Island", read off the host zone table for this entry rather than
inherited from the note that first claimed it. So on a Kiritimati host the rule's table names no value the
function is allowed to return.

Two rows had sat at NEEDS-OWNER-DECISION carrying that as a bare question. The owner answered on 2026-08-30:
**true offset**. What makes it the right answer is that it is the only candidate that states nothing untrue.
Emitting 14 leaves the stated range — a departure, but a departure about a range. Clamping to 13 misreports what
§15.21.1 calls "the ... local time differential factor provided by the system on which the function is
evaluated", so the value is simply wrong. And writing '0'/'00'/'00' asserts position 17's "the system ... does
not have the facility to provide the local time differential factor", which is a false statement about the
machine: it has the facility, and the answer is fourteen hours. Two of the three options lie; one is out of
range. Every citation in that paragraph was re-run through `cite.py --check` for this entry rather than carried
across from the row notes that made the same argument.

The minus side turned out to be reachable too, which the record had denied. A row note asserted the '–' branch
"cannot exceed 12 because no TimeZoneInfo zone is below -12:00". True of the HOST ZONE TABLE, and false of the
value CURRENT-DATE reports: `COBOLNET_CLOCK` parses a `DateTimeOffset` directly, and that type's offset range is
±14:00. So the departure is symmetric, the documentation names both signs, and the sentence is struck. A second
sentence went with it — the note's aside that `CurrentDate()` "reads DateTimeOffset.Now directly instead of the
injectable RunUnit.Current.Clock", which predates R21 and is now the exact opposite of the code, where
`ClockSeamDriftTests` exists to stop that read coming back.

The part worth more than the decision is what the tests pin. "FUNCTION CURRENT-DATE returns 14 at +14:00" is a
one-line assertion and it would have been nearly worthless: it says the headline digit is right and nothing about
whether the other twenty characters survived the extreme case. What a documented departure actually needs is its
SCOPE — the claim that positions 18-19 are the ONLY positions that ever leave the rule, on either sign, which is
what makes this one bounded determination instead of an open-ended divergence. So `R1Violations` transcribes
§15.21.3 rule 1's whole table back out of the rule text — deliberately re-derived, since a validator written from
the formatter could only ever agree with it — and the theory requires the violation set to be EMPTY for every
offset the rule admits and EXACTLY `{18-19}` for the two that exceed it. The table-wide test then folds all nine
fixtures together and asserts the union is `{18-19}` and nothing else. A regression that broke the hundredths at
+14:00, or truncated the value to twenty characters, fails there even while "14" still shows up in the right
place. And because the assertion names a non-empty expected set, a vacuous validator fails it too.

§15.99.3's identical table is the reason one determination covers both rows — one formatter, `CobolDate.Format21`.
But "same formatter" is a claim about two independent seams: WHEN-COMPILED is baked at compile time through
`IntrinsicBinder.CompileClock`, CURRENT-DATE is read at run time through `RunUnit.Current.Clock`. So the test pins
both to one instant and requires the same 21 characters, with positions 17-21 asserted in their own right, and it
reads the baked stamp out of the generated C# BY ITS SHAPE rather than by searching for the value expected —
searching for the expectation would pass on a compiler that baked nothing at all. End to end, `CurrentDateOffsetPinTests`
runs a real COBOL program in a child process under a pinned clock at +14:00, -14:00, +13:00, +05:45, +00:00 and
-09:30, because the determination is a statement about what a PROGRAM sees.

Adding that second mutator of `IntrinsicBinder.CompileClock` raced with `WhenCompiledStampTests` on the first full
run — green in isolation, red in the gate, the older test reading the newer test's injected clock. The fix was
already in the repo: `[Collection("process-globals")]`, the collection PB126 established for classes that mutate a
process-global. Both classes now carry it. Recording it because a new test that touches a shared static is a
concurrency change and not merely a coverage change, and the gate is what noticed.

`docs/CONFORMANCE.md` §3 carries the determination; `RV-15.21.3-1` and `RV-15.99.3-1` move to
DOCUMENTED-NON-SUPPORT with test-refs, GAP 3659 → 3657. Wave-local gate green: Conformance 190/190 on the filter,
Unit 5047/5047, Characterization 33/33. §A.1 item 111 stays open on purpose — it is one of the 166 outstanding
A.1 register obligations, not a defect this wave created.

