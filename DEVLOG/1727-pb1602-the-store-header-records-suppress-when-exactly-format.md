## Entry 1727 — 2026-09-27 04:07 PDT — PB1602: the store header records SUPPRESS WHEN exactly (format version 2)

**What.** An indexed file declaring `ALTERNATE RECORD KEY … SUPPRESS WHEN "€€"` could not be reopened by the program
that wrote it. OPEN INPUT and OPEN I-O both returned '39'. `RecordFraming.WriteHeader` stored the suppress value
through Latin-1, so the header said `??`. The §12.4.5.3 1) k) fixed-attribute comparison at OPEN then found a
different SUPPRESS WHEN phrase from the one the program declared. That comparison was right; the stored record
of the declaration was wrong.
**Fix.** The header is format VERSION 2: the value is stored as exact UTF-16 code units behind an Int32 count
biased by one. No `Encoding` is used, because an encoder replaces the lone surrogate in a legal `NX"D800"` national
literal. Version-1 stores are refused with '39' as Foreign, and no reader for them is kept (zero back-compat
baggage). `COBOLNET_FILES_DESIGN.md` documents version 2.
**Golden.** `2023/pb1602_suppress_value_beyond_latin1` (alphanumeric `"€€"`, national `N"€€"`, `NX"D800"`: write,
then reopen INPUT, READ, and reopen I-O, all '00'). `FixedFileAttributeFormatTests` pins version byte 2.
**Gate.** Conformance (File, Indexed, Relative, Open, CorpusRunner, Nist, Drift, Call, Pointer) 4327/4327; full Unit
29490/29491, where the one failure was the pinned version byte. After moving the pin, the store-format slice ran
4431/4431, and CI runs the full Unit suite.

