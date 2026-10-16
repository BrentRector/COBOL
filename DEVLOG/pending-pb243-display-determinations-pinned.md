## PB243: DISPLAY's transfer-size and ALL-literal determinations get their evidence

kb/Work PB243 said three DISPLAY rows were held open by missing evidence, not by code: GR2's unbounded transfer size,
GR5 a)'s chunking leg (unreachable only because of GR2), and GR3's `ALL literal-1` arm. Re-probed on the worktree build
at 1f5b68af4, every arm already gave the rule's answer, and two of the note's premises no longer held. The GR2
long-operand golden already existed (`2023/l1_display_transfer_size`, which displays 2,000 positions as one line), but
only on the standard device, while GR2 asks for the size "for each device". The GR3 refuter's `DISPLAY NULL` objection
had been fixed by PB1427: the probe is refused with COBOLNET2576, and the fabricated `§8.3.3.7` comment is gone from
`src/`.

**Evidence added.** `85/pb243_display_all_literal` pins every COBOL-85 figurative format in a DISPLAY operand: ALL
"AB" -> `AB` (§14.9.11.4 3), §8.3.3.6.4 3) c)), ALL symbolic-character and the bare one, ALL ZEROES/SPACES/QUOTES one
character each (§8.3.3.6.4 3) b)), and a contrast in which the same ALL "AB" in a 6-position VALUE repeats to `ABABAB`
(§8.3.3.6.4 2)). `2002/pb243_display_all_literal_kinds` covers the literal kinds 2002 introduced: X, N, NX (GR3's
national carve-out), B and BX (the alphanumeric representation, `101` / `1010`), the repeating contrasts, and ALL over a
concatenation expression. ALL "AB" & "C" prints `ABC`: §8.8.3.2 SR1 bars ALL as a concatenation operand, so ALL governs
the whole expression. `DisplayUponTests.Upon_EachOutputDevice_TransfersALongOperandAsOneLine` sends the 2,000-position
operand to the default device, SYSOUT, CONSOLE and SYSERR and asserts one line on each stream. The corpus compares only
standard output, so this test is the only place standard error is observed.

**Docs.** docs/CONFORMANCE.md DOC-A.1-58 now gives the transfer size per device. DOC-A.1-56 now states a boolean
literal's image and corrects the NULL refusal's code, which is COBOLNET2576 and not the COBOLNET1694 it had named.

**Rows.** GR-14.9.11.4-2, -3 and -5 move from PARTIAL to CONFORMS, and DOC-A.1-58 is restated with the per-device
witness; GAP 97 -> 94. PB243 is landed with those three rows in `closes_rows`.

**Self-review.** The change touches no compiler code. The test's expected value is computed from §8.3.3.6.4 GR2 (200
copies of the ten-digit pattern) and not copied from a run. The pattern ends in a digit, so PB150's trailing-space trim
cannot hide a fold.
