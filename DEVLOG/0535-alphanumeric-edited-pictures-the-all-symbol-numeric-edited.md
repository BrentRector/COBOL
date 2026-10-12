## Entry 535 — 2026-06-10 08:58 PDT — ALPHANUMERIC-EDITED pictures + the all-symbol numeric-edited classification fix → NC126A green (45 total)

NC126A's two residual failures exposed two PICTURE-classification gaps (both ISO §13.18.40):
- **Alphanumeric-edited** (`XBXBXBX`, `XX/XX/XX`, `990099` with X/A present): `PicInfo` classified them PLAIN
  alphanumeric and — worse — counted only X/A/9 in the Length, DROPPING the B 0 / insertion positions (the field
  was 4 wide instead of 7). Now: Length counts every position; `EditMask` is set when insertion symbols are
  present; a new `CobolEdit.FormatAlphanumeric` places source characters into the X/A/9 positions with B 0 /
  insertion on MOVE (§14.9.25.4 GR5).
- **All-symbol numeric-edited masks** (`PIC ****`, `$$$$`): the NumericEdited classification required
  `digits > 0` ('9' count), so a mask whose digit positions are all Z/*/floating symbols fell through to PURE
  NUMERIC (a Digits-0 long → empty output). The guard is now just `anyEdit` — the §13.18.40 digit positions
  include the suppression/floating symbols themselves.
**NC126A byte-matches → 45 NC locked in. 431 conformance + 15 unit green.** NC114M's diff narrowed 28→16 (its
remaining rows look like numeric-edited corner cases); NC215A/NC250A now run with diffs to chase.

