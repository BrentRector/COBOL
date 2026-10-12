## Entry 1224 â 2026-08-08 12:11 PDT â R12: the CALL boundary goes typed-native for every carrier â and the probe rewrote the diagnosis before a line changed

R12's note said the wide-tier CALL crossing silently truncated. The probe said something better and worse:
BY REFERENCE of a wide or unsigned COMP-5 leaf emitted UNCOMPILABLE C# â the caller's string-carrier write
lambda assigned a string to the native field, and the callee hardcoded a ManagedPointer<long> cell that its
own binder's carrier-typed reads could not use (the two ABI sides had never agreed for the wide tiers).
Only the BY CONTENT read half matched the "silent truncation" story. Measure first; the note's harm flags
were wrong in the loud direction.

The fix is the doctrine, applied literally: a native fixed-point leaf crosses the boundary in a cell of its
OWN carrier. CallPlaceIsString drops its Digits>18 and ulong-tier legs; RefCarrier and the BY CONTENT/
literal cells emit ManagedPointer<ElementType>; the callee's formal cell IS the formal's carrier, seeded by
PicInfo.DefaultInitializer so type and seed cannot drift; and CobolArgAdapt.Num/NumValue become GENERIC over
INumberBase<T> with one ReadNumericCell/WriteNumericCell pair for the cross-carrier and string-cell views
(the D5 boundary intact, the R10 bits contract at the UInt128 ends). No image, no codecs, nothing to keep
value-exact â the aliasing is exact by construction. A 19+-digit literal argument, previously a LOUD
stage, rides its typed cell for free.

Probes: the 8-byte container max survives the round trip whole (18446744073709551615 back in the caller);
the callee's ADD 1 through a UInt128 cell is visible to the caller. The callee DISPLAYing its formal shows
the picture-digit image â R13's latitude question, explicitly NOT a crossing loss now that the caller-side
after-value proves the value crossed intact.

Golden call_by_reference_wide_comp5. Gate: V59 predicate drift + Call/Linkage/Interop 16/16 Â· wave-local
Call|Interprogram|Linkage|Corpus|Nist 899/899 Â· characterization 33/33. With R15 and R16 landed the same
stretch, the batch (R24 Â· R15 Â· R16 Â· R12) owes its comprehensive battery next. work.py next: R17 Â· R19 Â·
R20, 9 actionable.

