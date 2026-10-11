## Entry 1018 — 2026-07-23 10:50 PDT — Conformance fix-queue: CA26 (intrinsics) — the alphanumeric repertoire is UNICODE, so CHAR/ORD/collation span the full UTF-16 range under a non-native PCS (ISO §15.15.3 / §12.3.7 k)3), not aliased to 8 bits

**CA26 (MINOR/S, intrinsics) landed — resolved toward the ESTABLISHED Unicode design, NOT the finding's original
"recommend 256/Latin-1".** Owner correction (this session): Unicode support was always intended (decided months ago) and
the spec permits it — a `PIC X` alphanumeric item holds UTF-16 code units, so the native alphanumeric repertoire is
Unicode (0xFFFF positions). The `Char(long n)` / `Ord(string)` NATIVE paths were already correct (the guard is
`> 0xFFFF`, and ORD is `code point + 1`); the residual Latin-1 assumptions were the ones to reconcile UP to the Unicode
model, not the other way round.

**The residue, fixed.** (1) `CobolString.Compare(…, ushort[] weights)` indexed `weights[c & 0xFF]`, aliasing a code
unit > 0xFF to its low byte's weight under a non-native PROGRAM COLLATING SEQUENCE — replaced by a `Weight(c, weights)`
helper: a code unit within the alphabet's remapped domain (0..weights.Length-1) takes its assigned position; one beyond
it keeps its NATIVE position (returns the code unit itself, ≥ 256), so it collates AFTER the whole positioned set in
code-unit order (§12.3.7 k)3 "unlisted characters follow in native order"), matching `Ord(string, weights)`'s
native-ordinal branch. (2) `Char(long n, ushort[] weights)` returned the §15.3 one-space default for a position beyond
the alphabet's domain — now returns the native code unit `(char)(n-1)` for `n-1 ∈ [weights.Length, 0xFFFF]`, the inverse
of ORD. (3) The `CobolIntrinsics.Text.cs` char-set-model doc now states the alphanumeric REPERTOIRE is Unicode and the
CONVERT BYTE/HEX serialization is a DISTINCT implementor-defined 8-bit Latin-1 mapping (§8.1.2 NOTE 2; the '?'
substitution already sets EC-DATA-CONVERSION, so it stays conformant and unchanged). `CHAR`/`CHAR-NATIONAL` guards (0xFFFF)
are unchanged — they were already right.

**Blast radius.** `Weight(c, weights)` is byte-identical to `weights[c & 0xFF]` for every code unit ≤ 0xFF (`c &
0xFF == c`), so only a code unit > 0xFF under a declared PCS changes — no existing collation test moves. The native
(no-PCS) relation path uses the ordinal `Compare(a,b,pad)`, already Unicode-correct and untouched. **Golden**
`2002/ca26_char_unicode_pcs` — under ALPHABET AL (a non-native PCS), CHAR(257) = U+0100 (position beyond the Latin-1
domain), ORD round-trips it to 257 (`ORD=00257`), and U+0100 collates above CHAR(256)=X"FF" (`P-GT-R`); pre-fix CHAR(257)
was the space default and U+0100 aliased to X"00"'s weight. Expected values SPEC-DERIVED, `--run`-verified. **Gate:** 45
collation/intrinsic/class-condition + manifest 5/5 + characterization 33/33 green; Debug build clean. **The phase-14
INDEPENDENT-MINORS batch is now COMPLETE (8/8).**

