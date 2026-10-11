## Entry 217 — 2026-05-29: CCVS column-7 optional-line indicators P/J excluded (file-I/O groundwork)

The file-I/O suites (SQ/IX/RL) were ~144/162 COMPILE_FAIL. A first cause: CCVS tags auxiliary/
alternate-configuration source in the indicator column (col 7). `ConvertFixedToFree` already
treated `D`/`S`/`Y` as optional (comment) lines — which is why NC/IF compile despite their `S`/`Y`
lines — but the file-I/O suites add `P` (an optional scratch file, e.g. the INDEXED `RAW-DATA`
member assigned to an X-card the program header does not declare) and `J` (an alternate ASSIGN
target beside the primary space-indicator one). These fell through to the code path, injecting a
spurious second SELECT / FD that derailed the parse (e.g. SQ102A's non-standard `RECORD-KEY IS`,
IX101A's double ASSIGN target). Added `P`/`J` to the excluded indicators; `A`/`B`/`C`/`G` stay
code (NC/SM use them). `P`/`J` appear only in SQ/IX, never in the guarded NC/IF/SM, so this is
guard-safe.

This is necessary but not sufficient: the SELECT/FD content still has grammar gaps (a bare
`SEQUENTIAL` organization clause without `ORGANIZATION IS`, `STATUS data-name` without `FILE`,
multi-line clause forms). SQ102A advanced past the P-block error to those. Guard ALL GREEN
(1000 / 336 / 149). The file-I/O FILE-CONTROL grammar and the indexed/relative runtime remain a
substantial, focused effort.

