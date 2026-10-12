## Entry 279 — Column-7 X-card matched-variant 'U' excluded → IX207A/IX208A (alt-key offset corruption)

Tackling IX208A (alternate-key relational START) led to the same root cause as IX207A — and it was NOT the
START logic. A handler-side debug showed the START's search key correctly formatted (`0000000053`) but the
stored alternate key reading back as `00300␣␣␣␣␣` (the alt *number* + spaces, extracted from a shifted
offset). The records are written through the fixed-width `FILE-RECORD-INFO` work area (`ALTERNATE-KEY1 PIC
X(29)`) but read through the FD record's `IX-FS1-ALTKEY1`, and the two had diverged.

Cause: the CCVS column-7 `T`/`U` indicators are a **matched alternate pair** that completes an intentionally
incomplete record layout. The base (space-indicator) FD record omits the key/alternate-key filler bytes;
base+`T` and base+`U` each total the declared RECORD length, but the preprocessor kept BOTH (only D/S/Y/P/J/
H/E were excluded — DEVLOG-era), overflowing the record by 10 bytes and shifting `IX-FS1-ALTKEY1`/`IX-FS2-
ALTKEY1` off the work-area offset. The tests' own working-storage key images use the `T` form, so `T` is the
active configuration; added `U`/`u` to the excluded-indicator set in `ReferenceFormatProcessor` (kept `T` as
ordinary code). Now base+`T` is the layout — `IX-FS1-ALTKEY1` is the 29-char text+number matching
`FILE-RECORD-INFO.ALTERNATE-KEY1`, so WRITE-through and read agree.

Blast radius: of all 265 baselines only IX107A carries column-7 `U` lines, and its output is byte-identical
after the exclusion (its `U` lines were non-critical). IX207A (duplicate-alternate-key read) and IX208A
(alternate-key relational START — GREATER/NOT LESS/EQUAL on a duplicate alt key) → CLEAN, baselined. Guard
ALL GREEN: 1000 unit / 347 integration / **265 NIST** (94 NC + 42 IF + 12 SM + 4 IC + 59 SQ + 19 RL + 35 IX),
0 regressions. IX now 35/42; remaining: IX215A (REDEFINES-of-key + three identically-named qualified keys),
IX216A/217A/218A (SELECT-OPTIONAL absent-file isolation — the optional file maps to a shared TF### a producer
already created; the shared-TF-by-number model can't distinguish an intentional P→D chain from an accidental
cross-program P/P collision without breaking SQ203A's optional *consumer*), IX301M/IX401M (flagging, excluded).

