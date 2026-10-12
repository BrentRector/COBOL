## Entry 1019 — 2026-07-23 11:03 PDT — Conformance fix-queue: DA1 (special-names) — a HEXADECIMAL literal in an ALPHABET clause now decodes to its character value, so a hex THRU range reverses the collating sequence (ISO §8.3.3.2 / §12.3.7.4 GR5)

**DA1 (discovered during CA3, MAJOR) fixed** — verified end-to-end and root-caused before fixing. SYMPTOM: `ALPHABET …
X"FF" THRU X"00"` used as the PROGRAM COLLATING SEQUENCE left the table NATIVE (HIGH-VALUE stayed X"FF") instead of
reversing. VERIFICATION: a char-literal THRU (`"Z" THRU "A"`) reversed correctly (LOW-VALUE = 'Z'), isolating the defect
to HEX decoding, not THRU handling. ROOT CAUSE: `DataBinder.LiteralChars` decoded a quoted string literal
(`CobolLiteral.Decode`, which `IsStringLiteral` gates to `"…"`/`'…'`/`N"…"`/`B"…"`) and an integer ordinal, but a
HEXADECIMAL-format literal (`X"hh…"`, §8.3.3.2) fell through to the raw-text fallback — returning `X"FF"` (length 5), so
the `AlphabetBind` THRU/ALSO guard `operands[0].Length == 1` failed and the range was silently skipped, leaving those
code points to the §12.3.7.4 GR3 native-order default.

**Fix (one arm):** in `LiteralChars`, before the integer/raw fallback, route a hex-shaped literal
(`text[0] is 'X'/'x' && text[1] is '"'/'\''`) through the existing `CobolLiteral.DecodeHex` (each hex-digit pair → one
character). `X"5A"` now decodes to 'Z' (length 1), so `X"5A" THRU X"41"` assigns the descending native run Z..A to
positions 1..26 per §12.3.7.4 GR5. **The same decoder serves the CLASS clause (`SwitchBindClass`), so a hex-literal
CLASS/CLASS-range is fixed too.** The national-alphabet path (`AlphabetOperandsNational`) correctly REJECTS a hex literal
(§12.3.7.3 SR14c2 requires national literals there), so there is no sibling gap.

**Blast radius = additive.** No existing test used a hex-literal alphabet (which is exactly why DA1 was latent); string/
apostrophe/integer/char-literal alphabets decode unchanged. **Golden** `2002/da1_alphabet_hex_thru` — `ALPHABET AL IS
X"5A" THRU X"41"` (descending Z..A): LOW-VALUE = 'Z' (`LOW=Z`) and 'A' (position 26) collates above 'Z' (position 1)
(`A-GT-Z`); pre-fix LOW-VALUE = X"00" and 'A' < 'Z'. Spec-derived, `--run`-verified. **Gate:** 47 alphabet/collation/
class tests + DA1 golden + manifest 5/5 + characterization 33/33 green; Debug build clean.

