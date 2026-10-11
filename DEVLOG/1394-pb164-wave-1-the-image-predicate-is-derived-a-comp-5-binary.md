## Entry 1394 — 2026-08-29 15:18 PDT — PB164 wave 1: the image predicate is DERIVED — a COMP-5/BINARY-* group crosses MOVE, CALL and DISPLAY, and the fleet caught the FIFTH drifted copy emitting uncompilable C#

The COMP-5 half was pure predicate drift, exactly as the note diagnosed (kb/Work PB164):
`PicInfo.ByteForm` pinned COMP-5 and BINARY-CHAR..DOUBLE as `Binary` (big-endian, StorageWidth bytes, A.1
items 205/208/215) while the image predicate was a hand-rolled `{Display, Binary, Packed}` union written
FOUR times — `DataItem.IsImageCapable`, `PhysicalModel`'s numLeaf filter, `OperandText`'s raw-storage arm,
the DataBinder Tier-B window mark — and never widened, so a conforming COMP-5-leaf group loud-staged at RUN
time on MOVE-as-group and CALL BY REFERENCE. The fix is the SHAPE: `PicInfo.HasImageByteForm` is THE ONE
image predicate, DERIVED from the ByteForm table; all four sites read it, and a usage added to ByteForm
(the float IEEE forms, wave 2) widens every consumer in one place.

THE REVIEW FLEET (78 agents, 18 confirmed) then caught what the derivation alone missed. (1) The FIFTH
drifted copy — PhysicalModel's carrier-name list `"long" or "Int128"` — left an UNSIGNED wide COMP-5/
BINARY-DOUBLE leaf (ulong/UInt128 carriers) out of the codec while the widened predicate emitted the codec
anyway: `01 G. 05 A PIC 9(10) COMP-5.` produced UNCOMPILABLE generated C# (a string splice into a ulong
field), unreachable by the corpus only because every wide-unsigned case sat at level 01. numLeaf now
derives from the predicate, and CobolNum gains BIT-IDENTICAL unsigned FormatImage/StoreImage lanes (a
ulong/UInt128 value reinterprets through the signed lane — the two's-complement bytes ARE the unsigned
magnitude bytes). (2) The golden's MOVE leg was GREEN BEFORE the fix — a same-shape group MOVE rides the
memberwise fast path that never consults the predicate. The discriminating legs now force AsImage AND
FromImage through an alphanumeric intermediary (§14.9.25.4 GR4 — a group move "is treated exactly as if it
were an alphanumeric to alphanumeric elementary move … no conversion"), run the unsigned ulong carrier
through the same round trip, and the CALL leg writes BOTH leaves through the shared storage (§14.2.3 GR8 —
"operates as if the formal parameter occupies the same storage area as the argument"). (3) Three stale
truth-texts: TierCIsland's default blame still named COMP-5, A.1 item 208 still DOCUMENTED COMP-5 as
"excluded from character images" (a shipped §4.2.16 claim contradicting the compiler), and the ONE-WIDTH
invariant test's own enumeration was a SIXTH copy of the retired union — all corrected, and the golden's
header citations are cite.py-validated this time.

TWO MEASURED SIDE EFFECTS. The island lock (TierCRejectionTests — "a lock to flip against") flipped its
MOVE arms exactly as designed; its fixture moved to the island's TRUE remaining boundary (a COMP-1 leaf).
And the DISPLAY leg for a COMP-5 leaf CLOSED ITSELF: the group image exists now, and DISPLAY transfers a
group's content VERBATIM — the leaf's two's-complement bytes reach the device raw, exactly as GnuCOBOL
renders such a group (`DisplayComp5Group_RendersVerbatimBytes` pins the bytes; GR-14.9.11.4-4's residue
narrows to float/INDEX groups and the variable-length format). (Process note, logged per the transparency
rule: the island-lock fix was applied while the wave-1 review fleet was still reading the tree — the
freeze-guard hook caught the follow-up build; and one Edit of the new test decoded escape sequences into
raw NUL/BEL bytes in the source — the tool_input_unicode_escapes trap the memory names — repaired
byte-level and byte-verified.)

PB164 stays OPEN: the mixed-usage REDEFINES codec (the class tiering still routes a binary-canonical
REDEFINES to the Tier-C runtime loud — probed), the float IEEE byte-form pin, the INDEX owner decision, and
the remaining DISPLAY legs. Verdict: GR-14.9.11.4-4 stays PARTIAL with the narrowed residue. Wave-local gate GREEN (Conformance in-filter with NIST and the flipped island lock; Unit 4808/4808 - the widened ONE-WIDTH enumeration added ~160 facts; Characterization 33/33), inventory drift gate 10/10, GAP unchanged 3663 (a PARTIAL narrowing). Battery #35 accrues (PB157 + PB164 wave 1).

