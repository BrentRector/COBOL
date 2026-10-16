## Work register: leads from the w1097d report filed (PB1547 section, PB3303 analysis)

Two leads from the PB1551 round-2 report (w1097d, PR #145, DEVLOG 2111). `SET ADDRESS OF W UP BY 1` still COBOL0001 is the second SR5 receiving slot PB1547 already names, so it became a dated section in PB1547 with the site `CobolParserCore.g4#setIndexStatement`, not a new note.

`BY REFERENCE FUNCTION F(X)` and `BY REFERENCE O :: "M"` as parse errors in CALL had no open note (PB258 is the ADDRESS OF arm, PB2145 the invokeArgument duplication), so PB3303 holds it as suspected with the §14.9.4.3 SR3/SR5 and general-format clauses to adjudicate; legality was not decided. PB3304 was allocated and is unused.

A third lead, from the PB1099 finisher report (w1095i, PR #149), became a dated section in PB3144: the version-bump half is fixed by d752fc9bf (`{RUNTIME-VERSION}` in the snapshots), the changed-snapshot-file half (`gate_plan.py tier_of`) remains open.
