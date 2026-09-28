      *> ISO §8.4.6.2.1 3) a) - a contained program's own BASED root
      *> spelled like a container's GLOBAL BASED root is its own item,
      *> with its own data address (kb/Work PB1047, wave 69 Z: both
      *> roots' address pointers were named from the COBOL word, one C#
      *> member, and the program did not build)
      *> RULE §8.4.6.2.1 3) a): "If the name is declared in source
      *>   element B, the item in source element B is the referenced
      *>   item."  cite.py --check 8.4.6.2.1 "If the name is declared in
      *>   source element B, the item in source element B is the
      *>   referenced item" -> OK §8.4.6.2.1 3) a)
      *> RULE §13.18.27.4 GR2: a contained program "may reference that
      *>   name without describing it again" (cite.py --check 13.18.27.4
      *>   "may reference that name without describing it again" -> OK
      *>   §13.18.27.4 2)) - W69ZBJ, which declares no B, reaches the
      *>   container's B and so the storage its address names.
      *> EXPECTED OUTPUT, DERIVED:
      *>   IN B=INNR   W69ZBI's own B, addressed at its own S2.
      *>   J B=OUTR    W69ZBJ's B is the container's, addressed at S1.
      *>   OUT B=OUTR  W69ZBI's SET ADDRESS OF its own B left the
      *>               container's B addressing S1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZBO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X(4) VALUE "OUTR".
       01 B GLOBAL BASED PIC X(4).
       PROCEDURE DIVISION.
       P-MAIN.
           SET ADDRESS OF B TO ADDRESS OF S1.
           CALL "W69ZBI".
           CALL "W69ZBJ".
           DISPLAY "OUT B=" B.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZBI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2 PIC X(4) VALUE "INNR".
       01 B BASED PIC X(4).
       PROCEDURE DIVISION.
       P-MAIN.
           SET ADDRESS OF B TO ADDRESS OF S2.
           DISPLAY "IN B=" B.
           GOBACK.
       END PROGRAM W69ZBI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZBJ.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "J B=" B.
           GOBACK.
       END PROGRAM W69ZBJ.
       END PROGRAM W69ZBO.
