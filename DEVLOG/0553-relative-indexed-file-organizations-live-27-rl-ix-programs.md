## Entry 553 — 2026-06-10 18:20 PDT — RELATIVE + INDEXED file organizations live → 27 RL/IX programs byte-match (648 conformance)

**The third wave-2 family.** The KeyedIO agent delivered `StatementBinder.KeyedIo.cs` / `CSharpEmitter.KeyedIo.cs`
/ `Runtime/IO/RelativeFile.cs` + `IndexedFile.cs` + 8 facts and 19 anchored edits (17 applied clean; 2 anchors had
been moved by the SORT/MERGE integration — reconciled by hand, the UnsupportedOrg seam now fences ONLY SD files
and the registration loop routes keyed connectors). Surface per §14.9.30/§14.9.10/§14.9.41: SELECT
RELATIVE/INDEXED with ACCESS SEQUENTIAL/RANDOM/DYNAMIC, RELATIVE/RECORD/ALTERNATE keys captured into FileModel
and post-build-resolved; DELETE and START as new statements; READ F2/F3 with the §9.1.14 status-family branch
rules (INVALID KEY '2x', at-end '1x', success '0x' — a phrase whose family cannot arise is dead, the
leniency-tolerant CCVS rendering); GR25 sequential-relative READ stores the RRN into the key item; the runtime
slot/sorted-key stores persist at CLOSE and run-unit end.

**Integration fixes (mine):** the record AREA is the LARGEST record description (§13.4.2) — a shorter Records[0]
truncated the read splice and left a stale tail (RL106A's 56/102 pair); and the agent's fact programs named a
file `RF` — a RESERVED word (Report Footing) the legacy oracle rightly rejects while the greenfield accepted it
(renamed; the missing reserved-word check is queued for the Phase-2 EditionValidator's word tables).

**27 locked: RL101A 105A–108A 116A–119A 201A 205A 209A 210A 211A 302M; IX101A 105A 107A 108A 111A 201A 208A 209A
211A 212A 213A 302M.** Residuals: RL104A/IX104A need the USE AFTER STANDARD ERROR DECLARATIVES subsystem (absent
in the greenfield — a queued work item); IX102A timeout; the remaining DIFFs are unexamined (next pass). NC + ST
censuses hold with ZERO lost; **648 conformance + 15 unit green.** CALL stays parked — it lands alone next (the
structural static→instance emitter flip).

