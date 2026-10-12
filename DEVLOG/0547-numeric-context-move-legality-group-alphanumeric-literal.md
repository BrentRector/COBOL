## Entry 547 — 2026-06-10 14:15 PDT — Numeric-context MOVE legality: group + alphanumeric-literal senders (NC105A RUNERR → DIFF 124/132)

Two Table-16 legalities the numeric render path still loud-guarded (§14.9.25.3 — both are valid moves with the
sender treated as an UNSIGNED integer): a GROUP operand in a numeric context is its alphanumeric IMAGE
(`FieldNum`: a character-image group renders `FromAlphanumeric(group.AsImage())`; mixed-usage groups stay loud —
Tier-C), and an ALPHANUMERIC LITERAL in a numeric context decodes the same way (`AsNum` BoundStringLiteral arm).
NC105A advances RUNERR → DIFF 34 (124/132 rows pass); its remaining 8 rows are REDEFINES-layout/window
misalignments (computed images shifted by one position — the NC252A `REDEF11` class of work), queued with the
REDEFINES-tier item. 566 conformance + 15 unit green; census holds 84/95 GREEN, zero lost.

**Session segment total (DEVLOG 542–547): 54 → 84 NC byte-matches in one day** — EVALUATE, the 6-family verb wave
(parallel agents), the arithmetic waves (parallel worktree), MOVE/PICTURE editing closeout, the collating
subsystem, and these MOVE legalities. NOTE: the Claude monthly spend limit was hit mid-session (2 of 6 wave-2
scouts failed) — further agent fan-outs are blocked until it is raised; wave-2 briefs for CALL/inter-program,
RL/IX files, collating (done), and ODO are saved at `E:\tmp\verb-briefs\wave2-*.md`; SORT/MERGE and
DECIMAL-POINT-COMMA briefs still need scouting (or solo research).

