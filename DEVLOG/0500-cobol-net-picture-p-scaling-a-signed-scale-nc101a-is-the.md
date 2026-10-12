## Entry 500 — COBOL.NET: PICTURE P scaling (a signed scale) → NC101A is the FIRST full NC program GREEN

With sequential file I/O landed, NC101A ran end-to-end (Entry 499) and the differential harness surfaced its first
real arithmetic gaps: **11 numeric FAIL\*** in the MULTIPLY tests. All 11 traced to ONE feature — **PICTURE `P`
scaling positions** (ISO §13.18.40), which `PicInfo.Analyze` ignored entirely: the operands `WRK-DS-0201P` (S99P),
`A01ONE-DS-P0801` (SP(8)9), `WRK-DU-4P1-1` (P(4)9) bound at the wrong scale, so a multiply by `.000000001` came out
`0` and `.00001 × 12345.6` came out `0`. (The earlier `0000R` overpunch image was a *symptom* — a FAIL branch
displaying a wrong-but-then-edited value; the golden is all-PASS, so once the computation is right those branches are
never reached.)

**The model — ONE signed scale (singular-pattern).** `P` holds no digit and no storage but shifts the implied point.
Trailing P (`99P`) scales the stored digits UP → a NEGATIVE fraction scale; leading P (`P(4)9`) puts the point left of
every digit → scale = leadingP + the digit count. `PicInfo` now computes a net **signed** `Scale` =
`trailingP>0 ? −trailingP : leadingP>0 ? leadingP+digits : afterV`; `Digits`/`Length` stay the `9` count (P has no
storage). The runtime already rescales via `Pow10` of the always-non-negative scale *difference*, so a negative scale
works natively. The `NumProfile` `FractionScale` getter had a stale `< 0 ? 0` clamp plus two **vestigial** P fields
(`LeadingScaleDigits`/`TrailingScaleDigits` — nothing set them, the runtime never applied the trailing one): removed,
`FractionScale => FractionDigits` (signed, may be negative) — the one canonical representation. `EmitText.UnscaledAtScale`
now handles a negative scale (the `99P(4) VALUE 990000` → 99 init, which previously threw on a negative substring).

**Result — NC101A is GREEN.** It compiles, runs, and its CCVS print-file report **byte-matches the NIST golden**
`tests/nist/valid/NC101A.txt` on the guard acceptance basis (0 FAIL\*, 0 diff lines) — the **first full NC program**
through the COBOL.NET differential harness, the moment the verification net flips from "hand-picked fragments I chose"
to "what the corpus exercises."

**Verification backbone.** A real `NistDifferentialTests` (replacing the build-anchoring scaffold) compiles + runs a
NIST program with CCVS X-card preprocessing, reads its print file (or stdout), normalizes to the guard basis (drop CR,
strip per-line trailing spaces, mask COMPUTED=), and asserts == the golden — `NC101A` is the first `[InlineData]`, and
every program that goes green becomes a permanent regression test by adding its name. Plus 5
`PictureScalingDifferentialTests` pinning the P cases (trailing/leading P in MOVE, MULTIPLY, multi-receiver, and VALUE
init) to the legacy oracle. Conformance 220 → 226; 13 unit; green. Greenfield-only (PicInfo / NumProfile / EmitText
are all Cobol.Net.\*) — the shared front-end and the legacy oracle are untouched.

