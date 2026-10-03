      *> kb/Work PB1063 -- ISO 14.8.2.3.3 2): "If the formal parameter is
      *> of class pointer or an object reference described without the
      *> ACTIVE-CLASS phrase, the conformance rules shall be the same as
      *> if a SET statement were performed" in the activating runtime
      *> element, the argument SENDING and the formal RECEIVING.
      *> (1) DP, a data-pointer restricted to REC-T, BY CONTENT into LP,
      *>     restricted to an equivalent REC-T (8.5.3.1): 14.9.39.3 SR19
      *>     "identifier-6 shall be the predefined address NULL or shall
      *>     reference a data-pointer restricted to the same type" holds,
      *>     so MR reads the record through LP: MR:OK.
      *> (2) NULL BY CONTENT into LP: SR19's own words admit it: MR:NULL.
      *> (3) PR, a program-pointer restricted to P1063T, BY CONTENT into
      *>     the UNRESTRICTED program-pointer LQ: SR22 conditions only a
      *>     RESTRICTED receiver ("If identifier-7 references a restricted
      *>     program-pointer ..."), and SR21 asks only the category, so
      *>     the SET is valid and MQ runs: MQ:SET.
      *> OO and restricted pointers are COBOL 2002 introductions.
       IDENTIFICATION DIVISION.
       CLASS-ID. P1063K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 A PIC X(2).
       01 DPT IS TYPEDEF USAGE POINTER TO REC-T.
       PROCEDURE DIVISION.
       METHOD-ID. MR.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LR TYPE REC-T BASED.
       LINKAGE SECTION.
       01 LP TYPE DPT.
       PROCEDURE DIVISION USING BY REFERENCE LP.
           IF LP = NULL
               DISPLAY "MR:NULL"
           ELSE
               SET ADDRESS OF LR TO LP
               DISPLAY "MR:" A OF LR
           END-IF
           GOBACK.
       END METHOD MR.
       METHOD-ID. MQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION USING BY REFERENCE LQ.
           IF LQ = NULL
               DISPLAY "MQ:NULL"
           ELSE
               DISPLAY "MQ:SET"
           END-IF
           GOBACK.
       END METHOD MQ.
       END OBJECT.
       END CLASS P1063K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1063M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1063K
           PROGRAM P1063T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 A PIC X(2).
       01 DPT IS TYPEDEF USAGE POINTER TO REC-T.
       01 PPT IS TYPEDEF USAGE PROGRAM-POINTER TO P1063T.
       01 W TYPE REC-T.
       01 DP TYPE DPT.
       01 PR TYPE PPT.
       01 O USAGE OBJECT REFERENCE P1063K.
       PROCEDURE DIVISION.
           MOVE "OK" TO A OF W
           SET DP TO ADDRESS OF W
           SET PR TO ENTRY "P1063T"
           INVOKE P1063K "NEW" RETURNING O
           INVOKE O "MR" USING BY CONTENT DP
           INVOKE O "MR" USING BY CONTENT NULL
           INVOKE O "MQ" USING BY CONTENT PR
           STOP RUN.
       END PROGRAM P1063M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1063T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
           GOBACK.
       END PROGRAM P1063T.
