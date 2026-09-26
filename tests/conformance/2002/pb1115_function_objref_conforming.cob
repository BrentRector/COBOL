      *> kb/Work PB1115 - a user-defined function's object-reference
      *> arguments meet ISO 14.8.2.3.2 (8.4.3.2.3 SR13 imports it), and a
      *> CONFORMING pair still compiles and crosses BY REFERENCE.
      *> RULE (8.4.3.2.3 SR13): "If function-prototype-name-1 or
      *> function-pointer-name-1 is specified, the rules for conformance
      *> specified in 14.8.2, Parameters and 14.8.3, Returning items,
      *> apply."
      *> RULE (14.8.2.3.2 rule 1): "If either the argument or the formal
      *> parameter is a universal object reference, the corresponding
      *> formal parameter or argument shall be a universal object
      *> reference."
      *> RULE (14.8.2.3.2 rule 3): "If either the argument or the formal
      *> parameter is described with an object-class-name, the
      *> corresponding formal parameter or argument shall be described
      *> with the same object-class-name, and the FACTORY and ONLY
      *> phrases shall be the same."
      *> cite.py --check 8.4.3.2.3 "the rules for conformance specified
      *>   in 14.8.2, Parameters and 14.8.3, Returning items, apply"
      *>   -> OK  8.4.3.2.3 13)  (Syntax rules)
      *> cite.py --check 14.8.2.3.2 "If either the argument or the
      *>   formal parameter is a universal object reference" -> OK
      *>   14.8.2.3.2 1)
      *> cite.py --check 14.8.2.3.2 "and the FACTORY and ONLY phrases
      *>   shall be the same" -> OK  14.8.2.3.2 3)
      *> Each function tests its formal and returns "OBJ " or "NULL";
      *> P1115T also SETs its BY REFERENCE formal to NULL.
      *> DERIVATION of every output line:
      *>  T: W-T (OBJECT REFERENCE P1115C) holds a new instance and meets
      *>     a formal with the same class-name and no ONLY on either side
      *>     (rule 3) -> conforming; BY REFERENCE (8.4.3.2.4 GR5 a), so
      *>     the function sees the object ("OBJ ") and its SET reaches
      *>     W-T. "T=OBJ " then "A=NULL".
      *>  U: W-U is universal and so is the formal (rule 1); it holds the
      *>     same instance. "U=OBJ ".
      *>  O: W-O is OBJECT REFERENCE P1115C ONLY into a formal described
      *>     the same way, ONLY on both sides (rule 3); W-O is NULL.
      *>     "O=NULL".
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1115T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1115C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE P1115C.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-O RETURNING L-R.
           IF L-O = NULL MOVE "NULL" TO L-R ELSE MOVE "OBJ " TO L-R
           END-IF
           SET L-O TO NULL
           GOBACK.
       END FUNCTION P1115T.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1115U.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-O RETURNING L-R.
           IF L-O = NULL MOVE "NULL" TO L-R ELSE MOVE "OBJ " TO L-R
           END-IF
           GOBACK.
       END FUNCTION P1115U.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1115O.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1115C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-O USAGE OBJECT REFERENCE P1115C ONLY.
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-O RETURNING L-R.
           IF L-O = NULL MOVE "NULL" TO L-R ELSE MOVE "OBJ " TO L-R
           END-IF
           GOBACK.
       END FUNCTION P1115O.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P1115M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION P1115T
           FUNCTION P1115U
           FUNCTION P1115O
           CLASS P1115C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-T USAGE OBJECT REFERENCE P1115C.
       01 W-U USAGE OBJECT REFERENCE.
       01 W-O USAGE OBJECT REFERENCE P1115C ONLY.
       01 WS PIC X(4).
       PROCEDURE DIVISION.
           INVOKE P1115C "NEW" RETURNING W-T
           SET W-U TO W-T
           MOVE FUNCTION P1115T(W-T) TO WS
           DISPLAY "T=" WS
           IF W-T = NULL DISPLAY "A=NULL" ELSE DISPLAY "A=OBJ " END-IF
           MOVE FUNCTION P1115U(W-U) TO WS
           DISPLAY "U=" WS
           MOVE FUNCTION P1115O(W-O) TO WS
           DISPLAY "O=" WS
           STOP RUN.
       END PROGRAM P1115M.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1115C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS P1115C.
