      *> ISO §8.4.6.2.1 3) a) - a contained program's own REDEFINES
      *> root spelled like a container's GLOBAL REDEFINES root is its
      *> own item, and the container's root stays reachable through its
      *> subordinates (kb/Work PB1047, wave 69 Z: the two roots' storage
      *> used to be emitted under one C# name and the program did not
      *> build)
      *> RULE §8.4.6.2.1 3) a): "If the name is declared in source
      *>   element B, the item in source element B is the referenced
      *>   item."  cite.py --check 8.4.6.2.1 "If the name is declared in
      *>   source element B, the item in source element B is the
      *>   referenced item" -> OK §8.4.6.2.1 3) a)
      *> RULE §8.4.6.2.2: "All data-names and screen-names subordinate
      *>   to a global name are global names" (cite.py --check
      *>   8.4.6.2.2 -> OK), so G1 stays visible although G is hidden.
      *> RULE §13.18.27.4 GR2: a contained program "may reference that
      *>   name without describing it again" (cite.py --check 13.18.27.4
      *>   "may reference that name without describing it again" -> OK
      *>   §13.18.27.4 2)) - the storage stays the container's.
      *> EXPECTED OUTPUT, DERIVED:
      *>   IN G=INNR G1=OUTR  G is W69ZRI's own group (INNR); G1 is the
      *>                      container's global subordinate (OUTR).
      *>   IN K=1234 G=1234   K REDEFINES the LOCAL G, so the MOVE to K
      *>                      is the local G's storage.
      *>   OUT G=XY12         the contained MOVE to G1 wrote the
      *>                      container's G, which the local K never
      *>                      touched.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZRO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
           05 G1 PIC X(4) VALUE "OUTR".
       01 H REDEFINES G PIC 9(4).
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "W69ZRI".
           DISPLAY "OUT G=" G.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZRI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
           05 G2 PIC X(4) VALUE "INNR".
       01 K REDEFINES G PIC 9(4).
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "IN G=" G " G1=" G1.
           MOVE 1234 TO K.
           DISPLAY "IN K=" K " G=" G.
           MOVE "XY12" TO G1.
           EXIT PROGRAM.
       END PROGRAM W69ZRI.
       END PROGRAM W69ZRO.
