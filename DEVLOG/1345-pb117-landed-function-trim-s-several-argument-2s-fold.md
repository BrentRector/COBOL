## Entry 1345 — 2026-08-28 23:34 PDT — PB117 LANDED: FUNCTION TRIM's several argument-2s fold SEQUENTIALLY (§15.96.4 r5) — the set union removed interleavings the nesting cannot reach

`CobolIntrinsics.Trim` now folds left — one trim per argument-2, in written order — per r5's "each argument-2 is
processed completely in the order that they are specified" and its NOTE's TRIM(a b c) ≡ TRIM(TRIM(a b) c). The
former one-pass `s.Trim(set)` gave TRIM("bcab" "c" "b") = "a" where the rule requires "ca" (the inner fold
strips nothing — 'c' guards neither edge). ⚠ The NOTE's own example agrees under BOTH readings — a golden built
from it would have passed over the defect (batch 7's refuter said exactly this) — so the golden
`2023/pb117_trim_sequential` (TRIM is 2014, argument-2 2023 — relocated after the edition gate caught the 2002
draft) pins the interleaved edge, the LEADING shape, an ORDER-sensitivity pair ("b","c" → "a" vs "c","b" →
"ca"), the NOTE text, and the space default. RV-15.96.4-5 → CONFORMS (RV-15.96.4-1/2/3 stay PARTIAL on
PB124's keyword-order axis).

