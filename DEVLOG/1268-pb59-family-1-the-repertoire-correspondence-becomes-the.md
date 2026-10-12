## Entry 1268 — 2026-08-09 15:40 PDT — PB59 family 1: the repertoire correspondence becomes the declared item-33 TOTAL IDENTITY — seven rows close

The one expression where the runtime contradicted the documented UTF-16 repertoire:
`Repertoire`'s `toNational || c <= 0xFF ? c : sub ?? Sub()` cut NAT→ANUM at 0xFF — DISPLAY-OF(N"Š")
substituted '?' (ORD 64 where 353 is owed), NATIONAL-OF's computed sub was provably dead, and CONVERT's
character legs inherited all of it. The fix is a DECLARATION plus an identity: the Annex A.1 item-33 row
now exists in CONFORMANCE.md §7 (the correspondence between the two UTF-16 repertoires is the total
identity, both directions; argument-2 accepted-and-inert; EC-DATA-CONVERSION unreachable from any
character pathway BY DECLARATION — r1 grants the correspondence to the implementor), audit_annex_a1.py
20/199 with --check clean, and `Repertoire(arg) => arg` is the one-line implementation. The map's caution
held: item 188 answers a DIFFERENT question (§12.3.7.4's ALPHABET-phrase correspondence), so the new row
cites 15.26/15.66/14.9.25/8.8.4.2.11 — the clauses item 33 itself names — and reconciles with 188
rather than filing under it.

What made the identity OBSERVABLE end-to-end: the standard display device now writes BOM-less UTF-8
(ProgramTable.RunMain — item 59's device determination; a total correspondence that loses every wide
character at the OS codepage is not usably total) and the shared test-harness ProcessObserver decodes
UTF-8. Probes: DISPLAY-OF ORD 353 ✓ round trips both directions ✓ MOVE GR6's LEGAL A→N direction
carries the identity (the N→X direction is correctly Table-16-forbidden — COBOLNET0819 — and the row
says so). D-N4's TWO design-doc copies rewritten together; ExpressionBinder/EcFunctions comments swept;
DOC_INDEX's stale 10/199 corrected from the audit's own run.

Goldens: NEW pb59_repertoire_identity (ORD-353 both sides of the declared boundary — encoding-immune);
national_intrinsics RE-DERIVED (SUB legs now prove totality + argument-2 inertness, UTF-8 expected
output); the EC differential test RETARGETED POSITIVELY — the character pathway asserts ORD 353 + EMPTY
status under enabled checking, and a NEW ByteSerializationSetsDataConversion test pins the SURVIVING
'?'+EC on the 8-bit byte-serialization legs (a SEPARATE §8.1.2 determination, open residue
RV-15.19.4-2 — §15.19.4 r2 authorizes no substitution). The round-trip test gains the wide leg.

RV-15.26.4-1 PARTIAL→CONFORMS · RV-15.26.4-3 DIVERGES→CONFORMS · RV-15.66.4-1/-2/-3 PARTIAL→CONFORMS ·
RV-15.19.4-1/-3 PARTIAL→CONFORMS. GAP 4185 → 4178. Gates: Conformance
~Convert|~National|~Intrinsic|~Corpus 762/762 · characterization 33/33 · SpecTraceabilityInventory 10/10
(it caught the renamed test and the deleted Sub before the batch fixed the refs — the gate earning its
keep) · audit --check clean.

