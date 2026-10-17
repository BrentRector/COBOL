*>--- conformance-case
*> id: pb303_as_externalized_name_2014
*> rules: [SR-8.3.2.2-L3.3, SR-8.3.2.2-L3.4, SR-8.3.2.2-L3.5, GR-11.5.4-1, GR-11.10.4-1]
*> accept: [2014]
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
       *>  1. CALL "PB34CBX" reaches PROGRAM-ID PB34CB - ISO 14.9.4.4 GR3 b)
       *>     makes literal-1 "the program-name of the program being called, as
       *>     described in 8.3.2.2", and 8.3.2.2 makes the AS literal that name.  The
       *>     DECLARED word PB34CB is NOT what CALL matches.
       *>  2. END PROGRAM PB34CB names the DECLARED word - ISO 10.7.3 SR2:
       *>     "Program-name-1 shall be identical to the program-name declared in a
       *>     preceding PROGRAM-ID paragraph."  The two names therefore stay distinct;
       *>     a compiler that folded literal-1 onto the program-name could not compile
       *>     this program at all.
       *>  3. FUNCTION PB34FN activates FUNCTION-ID PB34FN AS "PB34FNX" - a
       *>     user-function-name is referenced as a WORD (ISO 8.4.6.6 / 8.4.6.7), so
       *>     the AS literal does not intercept it, while 11.5.4 GR1 still externalizes
       *>     the function under "PB34FNX".

       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB34FN AS "PB34FNX".
       DATA DIVISION.
       LINKAGE SECTION.
       01  FN-RESULT PIC 9(4).
       PROCEDURE DIVISION RETURNING FN-RESULT.
       FN-P.
           MOVE 1989 TO FN-RESULT.
           GOBACK.
       END FUNCTION PB34FN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB34MN AS "PB34MNX".
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB34FN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB34CBX".
           MOVE FUNCTION PB34FN TO WS-N.
           DISPLAY "FN=" WS-N.
           STOP RUN.
       END PROGRAM PB34MN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB34CB AS "PB34CBX".
       PROCEDURE DIVISION.
       CB-P.
           DISPLAY "CALLEE-VIA-AS-LITERAL".
           GOBACK.
       END PROGRAM PB34CB.
