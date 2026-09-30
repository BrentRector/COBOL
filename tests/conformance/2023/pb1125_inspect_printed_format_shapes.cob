      *> PB1125 - ISO 14.9.22.2 general formats, read from the rendered PDF (folio 643-644): the printed shapes
      *> are the ones the grammar now spells, and every LEGAL shape must still parse and run.
      *>   tallying-phrase : { identifier-2 FOR { CHARACTERS [abp] | ALL { operand [abp] } ... | LEADING { ... } ... } ... } ...
      *>   replacing-phrase: { CHARACTERS BY ri [abp] | ALL { pair [abp] } ... | LEADING { ... } ... | FIRST { ... } ... } ...
      *>   after-before-phrase: AFTER and BEFORE each at most once, either order (the same rule in all four formats)
      *> GR10/GR16: the ALL and LEADING phrases are transitive across the operands that follow them.
      *>   (cite.py --check 14.9.22.4 "Both the ALL and LEADING phrases are transitive across the operands that follow them" -> OK 10)
      *> X = AABBCCAB (A0 A1 B2 B3 C4 C5 A6 B7).
      *> T1 - `ALL "A" "C"`: the bare "C" inherits ALL: 3 A + 2 C = 5.
      *> T2 - a per-operand delimiter on each operand: ALL "B" BEFORE "C" counts B2 B3 (2); the bare "A" AFTER "C"
      *>      counts A6 (1): 3.
      *> T3 - two counters, the second subscript-free: N FOR CHARACTERS BEFORE "B" (AA = 2), M FOR ALL "C" (2).
      *> T4 - a CHARACTERS phrase and an ALL phrase under ONE counter, in ONE shared cycle: CHARACTERS AFTER "C" is
      *>      tried first at 5, 6, 7 (3, taking the B at 7 before ALL "B" sees it), ALL "B" takes B2 B3 (2): 5.
      *> R1 - `ALL "A" BY "Z" ALL "B" BY "Y"`: the second ALL begins a NEW phrase, not the figurative ALL "B": ZZYYCCZY.
      *> R2 - FIRST is transitive: FIRST "B" (B2 -> 1) and the bare FIRST "C" (C4 -> 2): AA1B2CAB.
      *> R3 - CHARACTERS BY "." BEFORE "B" (A0 A1 -> ..) then ALL "C" BY "c": ..BBccAB.
      *> R4 - both delimiters on one operand, BEFORE first: AFTER "A" starts the region at 1, BEFORE "B" ends it at 2:
      *>      only A1 is replaced: AZBBCCAB.
      *> C1 - CONVERTING "ABC" TO "abc": aabbccab.
      *> C2 - CONVERTING "AB" TO "12" BEFORE "C" AFTER "A": region 1..3 (A B B): A122CCAB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1125FMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(8).
       01 N PIC 99 VALUE 0.
       01 M PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AABBCCAB" TO X.
           INSPECT X TALLYING N FOR ALL "A" "C".
           DISPLAY "T1=" N.
           MOVE 0 TO N.
           INSPECT X TALLYING N FOR ALL "B" BEFORE "C" "A" AFTER "C".
           DISPLAY "T2=" N.
           MOVE 0 TO N.
           INSPECT X TALLYING N FOR CHARACTERS BEFORE "B"
                                M FOR ALL "C".
           DISPLAY "T3=" N "/" M.
           MOVE 0 TO N.
           INSPECT X TALLYING N FOR CHARACTERS AFTER "C" ALL "B".
           DISPLAY "T4=" N.
           INSPECT X REPLACING ALL "A" BY "Z" ALL "B" BY "Y".
           DISPLAY "R1=" X.
           MOVE "AABBCCAB" TO X.
           INSPECT X REPLACING FIRST "B" BY "1" "C" BY "2".
           DISPLAY "R2=" X.
           MOVE "AABBCCAB" TO X.
           INSPECT X REPLACING CHARACTERS BY "." BEFORE "B"
                               ALL "C" BY "c".
           DISPLAY "R3=" X.
           MOVE "AABBCCAB" TO X.
           INSPECT X REPLACING ALL "A" BY "Z" BEFORE "B" AFTER "A".
           DISPLAY "R4=" X.
           MOVE "AABBCCAB" TO X.
           INSPECT X CONVERTING "ABC" TO "abc".
           DISPLAY "C1=" X.
           MOVE "AABBCCAB" TO X.
           INSPECT X CONVERTING "AB" TO "12" BEFORE "C" AFTER "A".
           DISPLAY "C2=" X.
           STOP RUN.
