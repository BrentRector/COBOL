## Entry 225 — Fix numeric SORT/MERGE key misclassification (collating Gap 1 follow-up)

The numeric-key test deferred in Entry 224 (SortNumericKey_IgnoresCollatingSequence) exposed a real
pre-existing bug, now fixed. Root cause: `SemanticModel.RegisterPicDescriptor` had ZERO callers, so
`_picDescriptors` was never populated and `GetPicDescriptor` always returned null. Its only consumer,
`FileIoLowerer.BuildKeysSpec`, therefore emitted isNumeric=0 for EVERY sort/merge key. This was
invisible until the collating work: raw-byte order of unsigned DISPLAY digits equals numeric value
order, so numeric keys happened to sort correctly anyway; once a (reversed-digit) collating sequence
could be applied to a key flagged non-numeric, the bug surfaced.

Fix: extracted `FileIoLowerer.BuildKeySpecField(key, baseOffset)` that derives the key PIC from the
LIVE lowering path — `_ctx.Location.ResolveLocation(key.Key)?.GetPic()` — instead of the dead
SemanticModel pic registry. Both `BuildKeysSpec` (file SORT/MERGE) and `BuildTableKeysSpec` now call
it. Bonus: `BuildTableKeysSpec` previously emitted only the legacy 3-field spec (offset,length,asc),
so Format-2 table SORT had the SAME numeric-blindness AND never received a collating sequence; it now
shares the full 11-field encoding, so table sort gets correct numeric handling and collating too.

`SortNumericKey_IgnoresCollatingSequence` un-skipped — PASSES. Guard ALL GREEN: 1000 unit, 342
integration (was 341 with 2 skipped; now 0 skipped), 149 NIST baselines 0 FAIL*.

Dead code noted: RegisterPicDescriptor / GetPicDescriptor / _picDescriptors are now provably unused.
Left in place this commit; flagged for a focused zero-dead-code cleanup (don't mix deletion with a
behavior fix).

Next: Gap 2 — FUNCTION CHAR/ORD honor the program collating sequence (currently native ASCII only).

