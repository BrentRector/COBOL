      *> kb/Work PB1940 - a strongly-typed group with an object-reference
      *> leaf crosses a program activation BY CONTENT and as a RETURNING
      *> item, and is a user-defined function's result.
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>   shall be of the same type" -> OK 14.8.2.2 2)
      *> cite.py --check 14.2.3 "allocated by the activating runtime
      *>   element" -> OK 14.2.3 9)
      *> cite.py --check 14.9.25.3 "If identifier-2 references a
      *>   strongly-typed group item, identifier-1 shall be specified and
      *>   be described as a group item of the same type" -> OK
      *>   14.9.25.3 2)
      *> Each pair is of one type, so the source conforms; before the
      *> fix BY CONTENT drew COBOLNET1688 and RETURNING COBOLNET1736,
      *> and the function result could not be moved.
      *> DERIVATION:
      *>   C  G holds an object and "ABC". BY CONTENT the callee's record
      *>      is a copy (GR9): it sees the object and ABC (C-IN:SET/ABC);
      *>      its SET TA TO NULL and MOVE "XYZ" reach only the copy, so G
      *>      keeps both (C-AFTER:SET/ABC).
      *>   R  the callee creates an object in its returning item and
      *>      moves "RET"; 14.6.5 places that content in R: R:SET/RET.
      *>   F  the function's result holds a new object and "FUN"; MOVE
      *>      of the function-identifier to F2 (same type): F:SET/FUN.
      *>   P  a POINTER leaf BY CONTENT (the retired negative
      *>      pb2087-strong-pointer-group-by-content): the callee sees the
      *>      address of X4 (P-IN:SET/pq); its SET PP TO NULL reaches only
      *>      the copy, so PREC keeps it (P-AFTER:SET).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1940F02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K1940.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
       LINKAGE SECTION.
       01 LF TYPE T.
       PROCEDURE DIVISION RETURNING LF.
           INVOKE K1940 "NEW" RETURNING TA OF LF
           MOVE "FUN" TO TX OF LF
           GOBACK.
       END FUNCTION P1940F02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1940M02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K1940
           FUNCTION P1940F02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
       01 G TYPE T.
       01 R TYPE T.
       01 F2 TYPE T.
       01 PT TYPEDEF STRONG GLOBAL.
          05 PP USAGE POINTER.
          05 PX PIC X(2).
       01 PREC TYPE PT.
       01 X4 PIC X(4) VALUE "DATA".
       PROCEDURE DIVISION.
           INVOKE K1940 "NEW" RETURNING TA OF G
           MOVE "ABC" TO TX OF G
           CALL "P1940C02" AS NESTED USING BY CONTENT G
           IF TA OF G = NULL
               DISPLAY "C-AFTER:NULL/" TX OF G
           ELSE
               DISPLAY "C-AFTER:SET/" TX OF G
           END-IF
           CALL "P1940R02" AS NESTED RETURNING R
           IF TA OF R = NULL
               DISPLAY "R:NULL/" TX OF R
           ELSE
               DISPLAY "R:SET/" TX OF R
           END-IF
           MOVE FUNCTION P1940F02 TO F2
           IF TA OF F2 = NULL
               DISPLAY "F:NULL/" TX OF F2
           ELSE
               DISPLAY "F:SET/" TX OF F2
           END-IF
           SET PP OF PREC TO ADDRESS OF X4
           MOVE "pq" TO PX OF PREC
           CALL "P1940P02" AS NESTED USING BY CONTENT PREC
           IF PP OF PREC = NULL
               DISPLAY "P-AFTER:NULL"
           ELSE
               DISPLAY "P-AFTER:SET"
           END-IF
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1940C02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
       LINKAGE SECTION.
       01 LG TYPE T.
       PROCEDURE DIVISION USING LG.
           IF TA OF LG = NULL
               DISPLAY "C-IN:NULL/" TX OF LG
           ELSE
               DISPLAY "C-IN:SET/" TX OF LG
           END-IF
           SET TA OF LG TO NULL
           MOVE "XYZ" TO TX OF LG
           GOBACK.
       END PROGRAM P1940C02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1940R02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
       LINKAGE SECTION.
       01 LR TYPE T.
       PROCEDURE DIVISION RETURNING LR.
           INVOKE K1940 "NEW" RETURNING TA OF LR
           MOVE "RET" TO TX OF LR
           GOBACK.
       END PROGRAM P1940R02.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1940P02.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           IF PP OF LP = NULL
               DISPLAY "P-IN:NULL/" PX OF LP
           ELSE
               DISPLAY "P-IN:SET/" PX OF LP
           END-IF
           SET PP OF LP TO NULL
           GOBACK.
       END PROGRAM P1940P02.
       END PROGRAM P1940M02.

       IDENTIFICATION DIVISION.
       CLASS-ID. K1940 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS K1940.
