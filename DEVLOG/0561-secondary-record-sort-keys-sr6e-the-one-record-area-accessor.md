## Entry 561 — 2026-06-10 19:48 PDT — Secondary-record SORT keys (SR6e) + the ONE record-area accessor (§13.4.2) → ST111A/ST124A locked (800 conformance)

**Two root causes under the ST var-len sort chains, both from the scout brief's §4.2 — the second one deeper
than the brief predicted.**

**1. SR6e key windows (the brief's diagnosis, exact):** ST110A/ST123A declare their SORT keys in the SECOND of
three 01 records under the SD (`SHORT/MEDIUM/LONG-SORT`, 50/75/100). `SortOffsetInRecord` anchored a classed
key at `SortPlainOffset(root, cls.Canonical)` — unreachable when the key's record root is ITSELF a secondary 01
(a sibling member of the synthesized multi-01 REDEFINES class, not a descendant of the canonical) — and the
binder mis-diagnosed legal keys as SR6b/SR6f OCCURS violations (the swept RUNERR). Per ISO §14.9.40.3 SR6e
(spec read in full): keys "need be described in only ONE of the record descriptions. The same byte positions …
are taken as the key in all records of the file" — records leftmost-align in the shared area (§9.1.2), so the
window is simply `target.ClassOffset − root.ClassOffset`. Also added the MISSING SR6g validation as a true
diagnostic (COBOLNET0874): with variable-length records every key must end within the minimum record size
(ST110A's keys end at byte 49 < min 50 — legal).

**2. The record area is the LARGEST record description (ISO §13.4.2) — everywhere, via ONE accessor.** With the
keys fixed, ST111A still failed: its FD declares `SHORT/MEDIUM/LONG-RECORD` (X(50)/X(75)/X(100)) and the
sequential READ stored the read image through `Records[0]`'s view — a 50-wide `RedefViewPlace` window over the
100-wide backing — so positions 51–100 of the area stayed SPACES (the produced breakdown showed exactly that:
first/second twenty right, quote-fill tail blank). The KeyedIo emitter had already hit and fixed this
identically (RL106A's 56/102 pair) with an INLINE widest-record pick — the same bug shape was waiting in
sequential READ, sort RETURN, and the WRITE-file-name fallback. Per the singular-pattern rule the concept now
lives ONCE: **`FileModel.AreaRecord`** (the largest record description — reading/writing the area goes through
ITS view), consumed by sequential `EmitRead`, both KeyedIo sites (replacing the inline copies), `BindReturn`
(GR3 "made available in the record area"), and `BindWrite`'s file-name fallback.

**Locked: ST111A, ST124A** (behind their ST109A→ST110A / ST122A→ST123A chains). **800 conformance + 15 unit
green.** ST golden coverage 23/29; remaining ST: ST108A/127A/133A/134A (Tier-C COMP-leaf SD records), ST131A
(SAME RECORD AREA), ST144A (XXXXX064), ST146A (legacy LOW-VALUE tail hole, swept-only).

