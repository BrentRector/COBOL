## Entry 1791 — 2026-09-29 16:57 PDT — PB1184 + PB1179: a zero-length UNSTRING sender terminates the statement; STRING into a national receiver

`EmitUnstring` ran its GR15 a) initiation test unconditionally, so a zero-length identifier-1 (DYNAMIC LENGTH, empty) took ON OVERFLOW and set EC-OVERFLOW-UNSTRING where §14.9.48.4 GR2 terminates the statement immediately. The whole statement after the sender read now sits under `if (src.Length != 0)`; neither overflow imperative runs and the pointer and tally are untouched (the reading GR17 supports: it acts after a completed transfer). Closes GR-14.9.48.4-2 and -15; GAP 805 to 803.

**PB1179** — every STRING into a class-national receiver compiled clean and ABORTED at run time (`StringEmitter.WriteImage`'s loud arm, mislabelled "usage display required, SR1" although SR1 admits national). The arm is narrowed to what SR1 forbids; national elementary, group-national and figurative receivers now store (GR3 a, GR7, GR2), and the stale "data model cannot describe" comments are rewritten. Closes GR-14.9.43.4-2 and -3; the note stays HALF for the PARTIAL rows 1/6/7/8/9. GAP 803 to 801.

PB1152 (INSPECT BACKWARD matching direction) was read and deliberately NOT started: GR8 NOTE 2 and GR8 c) admit two readings of where a multi-character literal sits relative to the current position, and Annex D.25 EXAMPLE 5 carries an erratum in row 2, so the note needs an adjudication of the rule text first.

