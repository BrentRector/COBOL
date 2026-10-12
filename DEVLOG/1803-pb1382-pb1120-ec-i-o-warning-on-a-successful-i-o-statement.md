## Entry 1803 — 2026-09-29 22:31 PDT — PB1382 + PB1120: EC-I-O-WARNING on a successful I-O statement

One mechanism, two defects. The sequential READ emitter called the USE / EC hook only on its failure branch (PB1382), so a successful read ending 06 never raised EC-I-O-WARNING; and where the hook did run on a success, `__IoCheckEc` answered -1 both for "a WHEN / USE handled it" and "nothing applied", so the statement's NOT phrase ran after a handled warning (PB1120; §14.6.13.1.4 2)/3).

- `__IoCheckEc`'s successful arm returns the new `ResumeSignal.HandledNonfatal` (-4) for a handled warning; `EmitUseHook` returns a `UseHookResult(Terminable, NotPhraseGate)` and every NOT phrase (`EmitNotPhrase`, `EmitInvalid`: sequential/keyed READ, WRITE, REWRITE, DELETE, START, NOT END-OF-PAGE) asks the one gate; the sequential READ success arm now calls the hook (EC only, `successArm: true`). A build with no enabled EC-I-O name emits byte-identical text.
- Four goldens: the READ / 06 / USE case, the USE-declarative and WHEN-phrase cases on an indexed WRITE ending 02, and the unhandled control. Rows GR-14.6.13.1.4-2, GR-14.6.13.1.4-3 and GR-9.1.14-L2.2 re-recorded CONFORMS (GAP 784 -> 781); PB1382 and PB1120 landed.

Batch 9 also read PB1516 and REVERTED a built fix: the capacity implementation (`PicInfo.IntegerCapacity`, `long relativeKeyMax`, 51 test call sites) made RRN 256 return 14 for a BINARY-CHAR UNSIGNED key, but §9.1.13.4 2) / §14.9.30.4 GR21 d) / §14.9.51.4 GR29 c) are written in DIGITS and 256 has three like the item's implementor-defined three; the note's premise (digits 0, check disabled) was also wrong. Recorded on PB1516 as a correction.

