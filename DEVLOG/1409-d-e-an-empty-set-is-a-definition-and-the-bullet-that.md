## Entry 1409 — 2026-08-31 02:31 PDT — D-E: an empty set IS a definition — and the bullet that collapsed two statements into one deferral was hiding a conforming answer inside a gap

`GR-14.9.10.4-19` had been looked at twice, by an adjudicator and by an independent refuter, and both stopped in
the same place. §14.9.10.4 GR19 says "The implementor shall define which of the fixed-file attributes are
validated during the execution of the DELETE FILE statement", and Annex A.1 item 50 makes that definition both
required and required to be documented. What `docs/CONFORMANCE.md` §3 actually said was "Documented non-support
until a physical-attribute store exists" — which is a deferral, not a definition, and D13 forbids an adjudicator
from converting one into the other. So the row sat as an owner question.

The answer is that **an empty set is a definition**. GR19's obligation is to DEFINE the set; nothing in the rule
requires the set to be non-empty, and the survey was unanimous among the implementations that have the statement.
COBOL.NET validates none of §9.1.6's attributes on DELETE FILE — not the organization, the record keys, SUPPRESS
WHEN, the code set, the logical or physical record sizes, the record type, the key collating sequence, nor the
record delimiter — and it does so UNIFORMLY. GR19's second sentence permits the set to vary by organization or
storage medium; saying ours does not vary is worth a clause of its own, because "uniformly empty" is something a
program can rely on and "empty for the cases anyone tried" is not.

That reframes GR18's '39' completely. GR18 sets '39' when the connector's attributes and the physical file's do
not match; with nothing validated there is nothing to mismatch, so **'39' is unreachable from DELETE FILE by
definition rather than by omission**. Same observable behaviour, entirely different conformance status — and
that is the whole point of writing the determination down, since the next reader would otherwise re-derive the
absent '39' as a defect for the third time.

The §3 bullet earned a split. It had been one sentence covering "DELETE FILE / OPEN never returns '39'", and
that collapse is what let a conforming determination hide inside a gap. DELETE FILE is now stated as conforming
by definition, with its §7 Annex A.1 item-50 row. **OPEN is not**, and it is now said plainly. Its set cannot
honestly be declared empty: DELETE FILE destroys the file, so validating nothing costs the program nothing,
whereas OPEN is the check every later read depends on — record length, key structure, code set. The survey runs
the other way there (GnuCOBOL validates key count, maximum record size and key descriptors; IBM validates seven
attributes), and declaring ours empty would mean a program opening a file under a contradicting FD gets silently
wrong data instead of '39'. So A.1 item 129 stays undischarged, needs the persisted physical-attribute catalog
the host-file model does not carry, and is now kb/Work PB193 with the `silent` and `under_rejects` flags PB192
correctly does not carry.

The golden derives every expected status from the rules before running anything: `2023/delete_file_attribute_set_empty`
creates a file through a 20-byte record-sequential connector and then deletes it through a LINE SEQUENTIAL
connector with a 40-byte record — contradicting TWO of the attributes §9.1.6 names as fixed, one of them "the
primary attribute" — and requires GR20 a)'s '00'. It then proves the deletion was real rather than merely
reported: GR14's SUCCESSFUL '05' on the re-delete, §9.1.13.4's '35' on the OPEN INPUT. And the golden was proven
to DISCRIMINATE before it was trusted — with '39' substituted for the expected '00' the corpus runner goes red on
that case by name, which is the difference between a test and a test-shaped file.

One piece of collateral worth recording: the §7 preamble's own figures were stale by three. It said "this
register discharges 33 of the 199 ... 166 obligations remain", measured 2026-08-29, while `audit_annex_a1.py`
reported 35 before this change and 36 after — items 48 and 57 had landed without the paragraph being touched.
Refreshed from the script's output, with the sentence that the script and not the paragraph owns the count.
Discharged delta for this landing: 164 → 163 obligations, §7 rows 38 → 39.

`GR-14.9.10.4-19` → CONFORMS with the golden as its test-ref, GAP 3657 → 3656. `GR-14.9.10.4-18` is untouched —
its OVERRIDE half is a grammar gap independent of the attribute store — and so is `delete_file_sharing.cob`'s
`DELFV=52`, which belongs to PB142's wave and lands with that fix rather than around it. Wave-local gate green:
Conformance 934/934 on the filter, Unit 5047/5047, Characterization 33/33.

