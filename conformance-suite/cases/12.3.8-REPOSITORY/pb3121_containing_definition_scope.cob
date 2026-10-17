      *>--- conformance-case
      *> id: pb3121_containing_definition_scope
      *> rules: [SR-12.3.8.3-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3121 - a program-prototype-name in scope with no
      *> REPOSITORY entry: 8.4.6.8 (cite.py --check 8.4.6.8 -> OK)
      *> "Program-prototype-names referenced within a source element
      *> shall be either the program-name of a containing program
      *> definition or a program-prototype-name declared in the
      *> REPOSITORY paragraph". 8.4.6.6 is the same sentence over
      *> functions, and its "the containing function definition" is
      *> the function's own (10.2: only an outermost program
      *> definition includes nested programs), so "containing" names
      *> the element's OWN definition and each one enclosing it.
      *> DERIVATION:
      *>   PS1 / PS2 - each a PROTOTYPE whose formal is a restricted
      *>     program-pointer TO ITSELF, no REPOSITORY entry: legal by
      *>     8.4.6.8. Their signatures are one (13.18.60.4 GR25, no
      *>     finite path reaches a difference), so SET B TO A meets
      *>     14.9.39.3 SR22: legal; MAIN-OK.
      *>   RSELF (RECURSIVE) - CALL RSELF names its own program
      *>     definition as program-prototype-name-1; 11.10.4 GR4 "may
      *>     call itself". N is working storage, shared by the three
      *>     activations: N=3 three times.
      *>   RINNER (contained in RSELF) - TO RSELF names a containing
      *>     program definition: legal; INNER-NULL.
      *>   NRSELF (not recursive) - CALL NRSELF while NRSELF is active:
      *>     14.9.4.4 GR3 f) EC-PROGRAM-RECURSIVE-CALL, the call is
      *>     not successful, ON EXCEPTION runs: NR-EXC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PS1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PTS IS TYPEDEF USAGE PROGRAM-POINTER TO PS1.
       01 L-P TYPE PTS.
       PROCEDURE DIVISION USING L-P.
       END PROGRAM PS1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PS2 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PTT IS TYPEDEF USAGE PROGRAM-POINTER TO PS2.
       01 L-Q TYPE PTT.
       PROCEDURE DIVISION USING L-Q.
       END PROGRAM PS2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3121M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PS1
           PROGRAM PS2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PTS IS TYPEDEF USAGE PROGRAM-POINTER TO PS1.
       01 A TYPE PTS.
       01 PTT IS TYPEDEF USAGE PROGRAM-POINTER TO PS2.
       01 B TYPE PTT.
       PROCEDURE DIVISION.
       MAIN-P.
           SET A TO NULL
           SET B TO A
           IF B = NULL
               DISPLAY "MAIN-OK"
           END-IF
           CALL "RSELF"
           CALL "NRSELF"
           STOP RUN.
       END PROGRAM PB3121M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RSELF RECURSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       RSELF-P.
           ADD 1 TO N
           IF N < 3
               CALL RSELF
           ELSE
               CALL "RINNER"
           END-IF
           DISPLAY "N=" N
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RINNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TO1 IS TYPEDEF USAGE PROGRAM-POINTER TO RSELF.
       01 Q TYPE TO1.
       PROCEDURE DIVISION.
       RINNER-P.
           SET Q TO NULL
           IF Q = NULL
               DISPLAY "INNER-NULL"
           END-IF
           GOBACK.
       END PROGRAM RINNER.
       END PROGRAM RSELF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NRSELF.
       PROCEDURE DIVISION.
       NRSELF-P.
           CALL NRSELF
               ON EXCEPTION DISPLAY "NR-EXC"
               NOT ON EXCEPTION DISPLAY "NR-CALLED"
           END-CALL
           GOBACK.
       END PROGRAM NRSELF.
