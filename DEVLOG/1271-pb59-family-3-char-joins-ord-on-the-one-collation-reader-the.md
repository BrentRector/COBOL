## Entry 1271 — 2026-08-09 17:15 PDT — PB59 family 3: CHAR joins ORD on the one collation reader — the representative, the tail, and the domain close together

Three defects, one missing structure. CHAR still carried the pre-PB3 native-ordinal fallback ORD lost at
DEVLOG 1121 (the unswept sibling — CHAR(255) under an ALSO alphabet returned the EC default, CHAR(257)
answered two positions low); its representative pick scanned weights[] ascending for the LOWEST code where
§15.15.4 r2 requires "the first character DEFINED" (CHAR(1) = 'A' where literal-1 'C' is owed — the source
order AlphabetBind computed into specOrder and threw away); and its domain screen tested the 256-entry
block where §15.15.3 r2 bounds argument-1 by "the number of positions in the alphanumeric program
collating sequence" — 65,534 under a three-into-one ALSO collapse, so 255 was refused and 65,536 admitted,
the bound inverted on both ends.

The structure is the national twin's, mirrored exactly: AlphanumericCollation (dense-256 positions +
RepByPos + NextFree; Weight/CharAt exact inverses by construction, PositionCount the r2 bound), the
CollatingTable record carrying the two members AlphabetBind now builds (the ALSO arm's next++ guarded like
the national arm's — an all-duplicate group cannot advance past an unoccupied position), and __COLLATE
emitted as the object the way __COLLATE_NAT always was. ORD's landed arithmetic moves INTO Weight — one
implementation where three grew.

The comparison paths deliberately keep the raw ushort[] carrier: CobolString.Weight's `: c` tail is
ORDER-EQUIVALENT to the exact arithmetic (both rules place every above-block code unit after every
positioned one, strictly increasing in code — no pair ever reorders), so relations, SORT/MERGE, indexed
keys and MAX/MIN gain nothing from the object; the proof note now lives at the tail so nobody "fixes" it
into churn. Only the functions exposing the position NUMBER — CHAR and ORD — read the collation.

Golden pb59_char_representative (every value from the python oracle: CH1=[C], ORD∘CHAR identity at 255/
257/65534, the 65535 refusal, ORD('A')=1 sharing position 0, the comparison arm agreeing with ORD).
pb3_ord_collating_tail stays byte-identical (the ORD regression net); ca26 stays green under both rules
and is cited as NOT-evidence. kb/Work/PB3.md's "did NOT need the data-structure change" sentence is
corrected — true for ORD's arithmetic, false for CHAR's representative. RV-15.15.4-1 / RV-15.15.4-2 /
AR-15.15.3-2 → CONFORMS. Gates: Conformance ~Corpus|~Char|~Ord|~Collat|~Sort|~Figurative 734/734 · characterization 33/33 · Unit 63/63 · the new golden asserted present · SpecTraceabilityInventory green · work.py 126 well-formed. GAP 4174 → 4171. NOTE the map’s claimed committed .g.cs pins do NOT exist (git ls-files is ground truth — no re-bakes were needed); one consumer the map missed, EmitCore.CollateArg, was caught by the CLI probe before any test ran.

