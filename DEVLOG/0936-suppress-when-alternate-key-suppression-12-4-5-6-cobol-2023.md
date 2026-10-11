## Entry 936 — 2026-07-20 14:40 PDT — SUPPRESS WHEN alternate-key suppression (§12.4.5.6, COBOL-2023) lands on COLLATING's compare seam

Grammar batch position 4, and the second half of the coupled indexed-file batch. The SUPPRESS WHEN phrase of the
ALTERNATE RECORD KEY clause (§12.4.5.6.2, a COBOL-2023 addition — Introduction p.27 / Annex E.3.3 item 42) gives
each alternate key a suppression value: a record whose alternate key equals literal-1 has that alternate access
path WITHHELD (§12.4.5.6.4 GR6), and the record "is not considered to exist" for READ/START on that key — while
the record itself and its prime-key path are untouched.

This landed small precisely because COLLATING went first: the suppression equality is the SAME §14.9.51 GR35
relation condition COLLATING already built (`KeyEq(keyOfRecord, literal, keyIndex)`, which routes through the
per-key weights — so suppression respects a declared COLLATING SEQUENCE for free, and is ordinal otherwise). The
runtime is ONE predicate, `IsSuppressed(image, keyIndex)` (prime key never suppressible, GR6), applied at the
four consumers the spec touches — NOT one, as an early one-liner scout claimed: the `Ordered(keyIndex)` filter
covers READ NEXT/PREVIOUS + START + the GR27 '02' look-ahead (all route through it), but ReadRandom (§14.9.30
GR32), WRITE (§14.9.51 GR41 — a suppressed key never duplicates), and REWRITE (§14.9.35 GR24 — entering/leaving
suppression re-positions) each scan `_recs` directly and needed the predicate at their own site. The alt-key
tuple widened once (Compiler + Runtime) to carry the decoded suppression value; the emitter threads it as a
named `suppress:` argument only when present, so a plain alternate key's registration is unchanged.

Grammar: `alternateKeySuppressWhen : SUPPRESS WHEN literal`, appended after the DUPLICATES phrase (fixed order),
parsed at all editions and 2023-gated by `VisitAlternateKeySuppressWhen` (construct `alternate-key-suppress-when-
2023`, COBOLNET0900 below 2023). No new diagnostic code (the §12.4.5.6.3 SR7 literal-category check is a
registered P14 refinement). Golden `tests/conformance/2023/altkey_suppress_when.cob` (GreenfieldOnly) pins the
exact semantics: record 02 (alt key "XX" = the suppression value) is INVISIBLE on the alternate-key walk (only 01
and 03 return) yet PRESENT on the prime-key walk (01, 02, 03). Ships as its own change set atop COLLATING; both
constructs share the one comprehensive legacy guard for the indexed-file batch (owner's multiple-waves-per-guard
directive).

