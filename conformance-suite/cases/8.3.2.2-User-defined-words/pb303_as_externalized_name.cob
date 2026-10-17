*>--- conformance-case
*> id: pb303_as_externalized_name
*> rules: [GR-8.3.2.2-L2.1, GR-8.3.2.2-L2.2, SR-8.3.2.2-L3.3,
*>    SR-8.3.2.2-L3.4, SR-8.3.2.2-L3.5, GR-8.3.2.2-L5.2, GR-11.5.4-1,
*>    GR-11.10.4-1, DOC-A.1-68]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the identification-division AS externalized-name phrase.
       *> ISO 8.3.2.2 2): "For any externalized user-defined words for which the AS
       *> phrase is specified, the content of the literal specified in that AS phrase
       *> is a name that is externalized to the operating environment."  The phrase is
       *> a COBOL-2002 introduction (the X3.23-1985 PROGRAM-ID paragraph has no AS
       *> phrase and AS is user-definable there); negative/pb303_as_phrase_below_2002
       *> pins that gate.
       *>
       *> Three facts, each derived from a rule, each observable here:
       *>  1. CALL "PB303CBX" reaches PROGRAM-ID PB303CB - ISO 14.9.4.4 GR3 b)
       *>     makes literal-1 "the program-name of the program being called, as
       *>     described in 8.3.2.2", and 8.3.2.2 makes the AS literal that name.  The
       *>     DECLARED word PB303CB is NOT what CALL matches.
       *>  2. END PROGRAM PB303CB names the DECLARED word - ISO 10.7.3 SR2:
       *>     "Program-name-1 shall be identical to the program-name declared in a
       *>     preceding PROGRAM-ID paragraph."  The two names therefore stay distinct;
       *>     a compiler that folded literal-1 onto the program-name could not compile
       *>     this program at all.
       *>  3. FUNCTION PB303FN activates FUNCTION-ID PB303FN AS "PB303FNX" - a
       *>     user-function-name is referenced as a WORD (ISO 8.4.6.6 / 8.4.6.7), so
       *>     the AS literal does not intercept it, while 11.5.4 GR1 still externalizes
       *>     the function under "PB303FNX".

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB303FN AS "PB303FNX".
       DATA DIVISION.
       LINKAGE SECTION.
       01  FN-RESULT PIC 9(4).
       PROCEDURE DIVISION RETURNING FN-RESULT.
       FN-P.
           MOVE 1989 TO FN-RESULT.
           GOBACK.
       END FUNCTION PB303FN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303MN AS "PB303MNX".
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB303FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB303CBX".
           MOVE FUNCTION PB303FN TO WS-N.
           DISPLAY "FN=" WS-N.
           STOP RUN.
       END PROGRAM PB303MN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303CB AS "PB303CBX".
       PROCEDURE DIVISION.
       CB-P.
           DISPLAY "CALLEE-VIA-AS-LITERAL".
           GOBACK.
       END PROGRAM PB303CB.
