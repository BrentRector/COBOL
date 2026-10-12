## Entry 1210 — 2026-08-07 20:29 PDT — R08: FIND-STRING's argument-3 stays in the long domain — the wrap that sampling could never catch

**R08 is LANDED, and Phase-B F25's diagnosis held exactly as written** — the second premise of sixteen to
survive contact (it had the repro, the root line, and the fix in hand). `FindString` narrowed the long `skip`
with an unchecked `(int)` cast AFTER the negative clamp, so any argument-3 whose LOW 32 BITS fell in
0..Count-1 returned a match position where §15.37.4 r2 leaves no matches and r3 requires ZERO: 2^32 printed 1
(and 7 for the LAST variant). **The finding's sampling note is the part worth keeping:** 2^31 and 2^32-1 both
miss the wrap by luck — only a value whose low bits land inside the match count reproduces, which is why no
boundary sweep ever caught it. The fix keeps the comparison in the LONG domain: `skip >= positions.Count`
returns 0 before any narrowing, and idx is a long. Three theory rows pin 2^32 (both variants) and 2^32+3 (low
bits == the match count — the boundary the wrap lands on). Distinct from the AsInt `(long)` GAP
(AR-15.22.3-1), which bites above 2^63 and stays open.

**GATE (wave-local):** FindString rows 14/14 · the intrinsic differential class + full Unit green (background
run). R06+R07's CI run in flight at commit time — this commit is HELD locally until that verdict lands.

