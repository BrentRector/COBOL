## Entry 1212 — 2026-08-07 21:09 PDT — R10 float half: the latitude belonged to SMALLEST alone — and the COMP-5 half becomes an owner decision: unsigned carriers

**HIGHEST/LOWEST-ALGEBRAIC take floats.** The old guard rejected every float for all three functions on
§15.83.3 r4's implementor latitude — which Annex A.1 item 180 grants to SMALLEST-ALGEBRAIC ONLY. §15.43.3 /
§15.58.3 carry exactly two mode-aware bars (r2: STANDARD-DECIMAL bars the §3.166 STANDARD BINARY usages; r3:
STANDARD-BINARY bars the §3.167 STANDARD DECIMAL usages) — under NATIVE arithmetic, the default, nothing bars
any float. The fold returns §15.43.4 r2's greatest finite carrier magnitude, negated for LOWEST: COMP-2 →
1.7976931348623157E+308, COMP-1 → 3.4028235E+38 — F72's expected values, probed via the CLI before the facts
were written. **The old guard's message also asserted COMP-1/COMP-2 are barred under STANDARD-DECIMAL — false
(they are native floats, not §3.166 standard binary), and shipped in a user-visible diagnostic.** F72's
estimate-correction held too: `BoundNumLiteral` is a STRING carrier and the E-form literal routes through the
existing `NumX(…, Real: true)` — no structural change was needed.

**The mode-bar fact failed once, correctly:** `ARITHMETIC IS` belongs to the OPTIONS paragraph, not
OBJECT-COMPUTER — the bar didn't fire because the mode was never set, which is exactly the discrimination the
fact exists to provide.

⭐ **THE COMP-5 HALF IS AN OWNER DECISION, TAKEN TODAY: UNSIGNED CARRIERS.** The fold says full container
(2^64−1 / 2^128−1), the storage tier says signed carrier (long / Int128) — F74 explicitly forked it for the
owner. Decision: unsigned COMP-5 emits `ulong` (10–18 digits) / `UInt128` (19–31) — the typed-native answer;
the item owns its full container range. `WrapBinary`'s bits=128 shift-mask bug (modulus collapses to 1) fixes
in the same change set, plus the fold-vs-PicInfo container-list drift test. Implementation is the next item;
the R10 note carries the full plan.

**GATE (wave-local):** intrinsic differential class **154/154** · Unit **4048/4048**.

