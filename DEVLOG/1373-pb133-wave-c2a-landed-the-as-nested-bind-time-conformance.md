## Entry 1373 — 2026-08-29 05:31 PDT — PB133 wave C2a LANDED: the AS NESTED bind-time conformance lane — SR19/SR21 mode correspondence and the §14.8.2 BY REFERENCE description check through THE one comparator

With PB131's callee table, three more of §14.8.2's obligations became compile-time diagnostics on the AS
NESTED lane. SR19/SR21 (COBOLNET1687): an EXPLICIT BY CONTENT / BY REFERENCE argument against a BY VALUE
formal, or a written BY VALUE against a by-reference formal — a keyword-less argument derives its mode
FROM the formal (GR9, PB131) and can never disagree, which is why only written phrases need the check.
The §14.8.2.3.2 / §14.8.2.2 BY REFERENCE description conformance (COBOLNET1688) runs through
OoConformance.DescriptionMismatch — the comparator the batch-8 note found reachable from INVOKE only; a
NESTED call is the same rule-2 same-description regime as a method, so the reuse IS the fix (one
mechanism, a second consumer). BY CONTENT / BY VALUE stay in §14.8.2.3.3's MOVE/SET-validity regime —
named residue on SR-14.9.4.3-25 with the RETURNING check and the dynamic Format-1 runtime lane
(EC-PROGRAM-ARG-MISMATCH needs the count/description facts registered with the program table — wave C2b).

Verdicts: SR-14.9.4.3-19 → CONFORMS, SR-14.9.4.3-21 → CONFORMS, SR-14.9.4.3-25 NOT-IMPLEMENTED → PARTIAL.

