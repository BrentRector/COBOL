## Entry 215 — 2026-05-29: SORT … USING infinite loop on an unreadable input file (7 ST hangs fixed)

Surveying the ST (sort/merge) suite showed eight runtime timeouts. The SORT … USING input pass
(`EmitSortUsingFile`) loops READ → RELEASE until the file's AT-END condition, tested by
`FileRuntime.IsAtEnd`. But `IsAtEnd` returned true ONLY for status "10" (AtEnd). When the USING
file could not be opened — e.g. `SORTIN-1B`, which the CCVS produces in a companion program and is
absent when ST102A runs alone — `ReadNext` returns "42" (file not open), never "10", so the loop
spun forever. The same latent hang applies to any plain `READ … AT END` loop over a missing file.

Fixed `IsAtEnd` to report end for every terminal "no further record obtainable" status — AtEnd,
FileNotOpen, FileNotFound, PermanentError — so a read loop always terminates. It cannot affect a
normal read (Success and AtEnd paths are unchanged); it only stops loops that previously processed
garbage or hung. Seven of the eight ST timeouts now complete (ST102A/105A/110A/113M/116A/120A/123A);
ST132A still hangs (a different cause — to investigate). Guard ALL GREEN (1000 / 336 / 149).

