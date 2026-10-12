## Entry 546 — 2026-06-10 13:55 PDT — PROGRAM COLLATING SEQUENCE live: NC215A + NC219A byte-match → 84/95 NC green

**The collating subsystem, implemented solo from the wave-2 scout brief** (the brief workflow lost 2 of 6 scouts to
the monthly spend limit — agent fan-outs are paused; this and the next items proceed serially).

**Model:** `CollatingTable(ushort[256] Positions, char HighValue, char LowValue)` — built per §12.3.7 GR7 k):
k1a numeric literal = 1-based native ordinal; k1b multi-char literal positions each char; k5 THRU expands the
native run EITHER direction; k6 ALSO members share ONE position (operand-1 first — the CHAR()/LOW-VALUE tie
winner); **k3 unspecified characters take DISTINCT ascending positions above the highest specified, native
relative order** — NOT the legacy's shared-255 bucket (the spec-deviation the brief flagged; ORD over unspecified
chars must stay distinct). HIGH/LOW extremes per GR8/GR9 with the last-/first-specified tie rules. Figuratives
WRITTEN INSIDE the clause are the native extremes (GR10 — NC219A's `"N" ALSO HIGH-VALUE ALSO LOW-VALUE`).
NATIVE/STANDARD-1/STANDARD-2 normalize to NO table (identity — the native fast path costs nothing).

**Threading:** `DataBinder.Switches` walks the ALPHABET clauses + the OBJECT-COMPUTER PCS clause and resolves
`DataBinder.Collating` (only the NAMED alphabet is active — NC219A's unreferenced COLLATING-SEQ-2 must not leak);
the emitter renders one `private static readonly ushort[] __COLLATE = {...};` and `EmissionContext.CollateArg`
appends it at every relation / figurative-relational / 88-membership `CobolString.Compare` site (§12.3.6 GR11 —
those three are EXACTLY the comparisons the PCS governs; INSPECT/STRING matching and class conditions stay
native). The new `CobolString.Compare(a, b, ushort[] weights)` is the COBOLNET_DESIGN §18 #14 seam, now real.
**Figurative IDENTITY:** `EmissionContext.FigFill` — under a PCS, HIGH-/LOW-VALUE materialize as the sequence's
extreme CHARACTERS everywhere they're rendered through the context (comparisons, MOVE/group fills, VALUE-clause
initializers, 88 VALUEs): §8.3.3.6 GR6/7. NC219A pins the subtle case — `N = HIGH-VALUE` must be FALSE because
runtime HIGH-VALUE re-derives to 0xFE (0xFF was consumed at position 3), and `NEW-LOW PIC X VALUE LOW-VALUE`
holds 'F' (tie → first-specified). §18 #14 updated in the same change set (deferral superseded).

**84/95 NC GREEN (+NC215A, NC219A; zero lost), 564 conformance, 15 unit.** Residue: NC235A/247A (ODO), NC236A
(spec-pinned), NC105A/224A/401M (louds), NC107A/108M (DECIMAL-POINT COMMA), NC252A (REDEFINES tier), 2 no-golden.
SORT/MERGE keys + CHAR/ORD consume the same table when those subsystems land.

