      *> ISO §8.4.6.2.1 3) a) - INITIALIZE of a name a contained
      *> program declares itself initializes ITS item, never a
      *> container's GLOBAL dynamic-capacity table of the same name
      *> (kb/Work PB1047, wave 69 Z: the whole-table arm picked the
      *> first DYNAMIC candidate across every tier, so the local item
      *> kept its value and the container's table was cleared)
      *> RULE §8.4.6.2.1 3) a): "If the name is declared in source
      *>   element B, the item in source element B is the referenced
      *>   item."  cite.py --check 8.4.6.2.1 "If the name is declared in
      *>   source element B, the item in source element B is the
      *>   referenced item" -> OK §8.4.6.2.1 3) a)
      *> RULE §8.4.6.2.2: "All data-names and screen-names subordinate
      *>   to a global name are global names" (cite.py --check
      *>   8.4.6.2.2 -> OK) - so where no local declaration hides it,
      *>   D-TAB names the container's table.
      *> EXPECTED OUTPUT, DERIVED:
      *>   IN [    ]          W69ZDI's own D-TAB (PIC X(4)) is spaced.
      *>   MID [ZZ|ZZ]        the container's table is untouched.
      *>   OUT [  |  ]        W69ZDJ declares no D-TAB, so its INITIALIZE
      *>                      spaces the container's table elements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZDO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-G GLOBAL.
           05 D-TAB OCCURS DYNAMIC CAPACITY IN D-CAP FROM 1 TO 4.
               10 D-X PIC X(2) VALUES ARE "AB" "CD" FROM (1) TO (3).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE "ZZ" TO D-X(1) D-X(2)
           CALL "W69ZDI"
           DISPLAY "MID [" D-X(1) "|" D-X(2) "]"
           CALL "W69ZDJ"
           DISPLAY "OUT [" D-X(1) "|" D-X(2) "]"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZDI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-TAB PIC X(4) VALUE "QQQQ".
       PROCEDURE DIVISION.
       P-MAIN.
           INITIALIZE D-TAB
           DISPLAY "IN [" D-TAB "]"
           GOBACK.
       END PROGRAM W69ZDI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZDJ.
       PROCEDURE DIVISION.
       P-MAIN.
           INITIALIZE D-TAB
           GOBACK.
       END PROGRAM W69ZDJ.
       END PROGRAM W69ZDO.
