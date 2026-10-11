## Entry 479 — COBOL.NET G2d: signed-numeric DISPLAY (over-punch / separate sign / binary minus)

The signed-DISPLAY half of the G2 data-model checkpoint. The current `FormatDisplay` returned the magnitude
(unsigned), so signed items were excluded from the differential net; now a signed item carries its sign in its
DISPLAY image per USAGE + SIGN clause (COBOLNET_DESIGN §6.4), matching the legacy oracle byte-for-byte.

The exact conventions were probed against the legacy (NIST-validated) before implementing — `NumProfile` gains a
**`NumericSign SignKind`** ∈ {TrailingOverpunch, LeadingOverpunch, LeadingSeparate, TrailingSeparate, BinaryMinus},
and `CobolNum.FormatDisplaySigned` applies it:
- **USAGE DISPLAY over-punch** (IBM-ASCII `{A-I` positive / `}J-R` negative) onto the last digit (default) or the
  first digit (SIGN LEADING): `S9(3) -42`→`04K`, `+42`→`04B`, `-150`→`15}`, `S9V99 -3.5`→`35}`, SIGN LEADING
  `-37`→`}37`.
- **SIGN SEPARATE** — an always-present `+`/`-`: LEADING `-37`→`-037`/`+037`, TRAILING `-37`→`037-`.
- **COMP/COMP-3/COMP-5** — a leading `-` only when negative, bare otherwise: `-42`→`-042`, `+42`→`042`,
  COMP-5 `-300`→`-0300`.

`PicInfo` computes `SignKind` from usage + the SIGN clause (`SignKindFor`) and emits it into the item's
`NumProfile`; `DataBinder` parses the `signClause` (LEADING/TRAILING, SEPARATE). `FormatDisplay` is now sign-aware
(delegates to `FormatDisplaySigned` for a signed item), so every DISPLAY/MOVE-to-alphanumeric call site is correct
with no call-site change. (One known follow-up: MOVE of a signed numeric to an alphanumeric receiver should move
the *de-signed* digit string per ISO §14.9.24 GR4d — not yet differentiated; no current test exercises it.)

Verified: full-solution build clean Debug (0/0); Conformance 70 green (17 new signed-DISPLAY differential tests
covering all five conventions + the MOVE/COMPUTE/SUBTRACT store-then-display path) + Unit 3 green; legacy oracle
untouched. RESUME AT → G2c (level-88 → bool properties + CobolCond), then G2-1c (ref-mod), then the formal bound
tree + emitter decomposition (G2-2).

DEVLOG 479.

