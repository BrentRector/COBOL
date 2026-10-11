## Entry 415 — S4 prep: decouple a typed numeric field's digit-count from its byte-width

Groundwork for widening the numeric flip beyond DISPLAY. The typed numeric cells overloaded `IrTypedFieldLocation.Width`
to mean BOTH the byte-window width (scratch allocation, `EncodeNumeric`/`DecodeNumeric` length) AND the PICTURE digit
count (`% 10^n` truncation in MOVE, the `digits` arg to `FormatUnsignedDisplay`). That conflation is only valid for
`USAGE DISPLAY`, where 1 byte == 1 digit. For `COMP`/`BINARY`/packed they differ (e.g. `PIC 9(5) COMP` is 4 bytes, 5
digits). Switched the three digit-count sites — numeric→numeric MOVE truncation, MOVE-literal truncation, and DISPLAY
formatting — to read `Pic.TotalDigits` (already carried on the location) instead of `Width`, leaving `Width` as the
pure byte width. For the existing DISPLAY-only typed fields the two are equal, so this is a **no-op refactor**: the 12
flip tests stay byte-identical (they ARE the proof), and the flag-OFF corpus never reaches these cells. Guard **ALL
GREEN: 1196 unit / 493 integration / 364 NIST**.

Deliberately did NOT yet relax the binder gate to admit COMP/BINARY usages: COMP-5 uses the full native binary range
(no PICTURE truncation), and COMP/COMP-4/BINARY truncation depends on TRUNC semantics — so the compile-time
`% 10^digits` truncations could diverge from the byte path. That needs an empirical byte-path-truncation check first
(NEXT), then the gate relaxes usage-by-usage. After that: signed + scaled → `decimal`, then OCCURS → `CobolTable<T>`.

