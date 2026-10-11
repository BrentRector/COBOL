## Entry 1238 — 2026-08-08 17:16 PDT — R27: class INDEX exists now — the lattice's two fail-opens close, and MESSAGE-TAG deliberately stays out

FUNCTION INTEGER(IX) computed the occurrence number silently — §8.5.2.1 Table 2 makes class INDEX
distinct from numeric and §15.44.3 r1 demands numeric. Two fail-opens, measured: an index DATA item is
not PIC-less as the ledger guessed — PicInfo.IndexItem carries category NUMERIC for storage, so
ClassOfPlace answered "numeric" and every 'n' screen passed it — and an index-NAME fell into ClassOf1's
generic computed-operand-is-numeric arm. CobolClass.Index (which no §15 argument rule admits, so
classifying it IS the rejection), a USAGE-keyed arm before the category table, and a BoundIndexRef arm
before the generic computed arm close both.

MESSAGE-TAG — Table 2's other unexpressed class — deliberately got NO member: the MCS facility has no
Usage member to classify, and a dead enum member is a_dead_lookup_is_also_unverified. Instead
EveryUsageMember_HasATable2ClassDisposition enumerates the 25-member Usage inventory against a
dispositioned map: the moment MESSAGE-TAG or any new usage lands, the fact goes red until its Table-2
class is decided and wired — the ledger's "a new PIC-less usage cannot re-open the hole", built as an
inventory rather than a hope.

Probes: both index shapes draw COBOLNET1627 "is of class index"; subscript/SET/relation stay green.
Negative fixture intrinsic-index-argument (both shapes, 2002/2014/2023). Two manifest-order slips from
earlier commits fixed in passing. Gate: Unit intrinsic set 35/35 · Conformance Intrinsic|Corpus 660/660
· the fixture green by name · characterization 33/33.

