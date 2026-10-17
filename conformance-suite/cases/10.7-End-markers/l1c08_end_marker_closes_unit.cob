      *>--- conformance-case
      *> id: l1c08_end_marker_closes_unit
      *> rules: [GR-10.7.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §10.7.4 GR1 — each END PROGRAM marker ends ITS source unit
      *> GR1: "An end marker indicates the end of the specified source
      *>   unit."
      *> cite.py:
      *>   OK  §10.7.4 1)  (General rule)
      *>   OK  §8.4.6.3 1)  (Scope of program-names) "If the
      *>       program-name is that of a program that does not possess
      *>       the common attribute and that is directly contained
      *>       within another program, that program-name may be
      *>       referenced only by statements included in that
      *>       containing program"
      *> Layout: L1C08C directly contains L1C08D, whose unit is ended by
      *> END PROGRAM L1C08D, and then L1C08E, ended by END PROGRAM
      *> L1C08E; END PROGRAM L1C08C ends the outer unit and L1C08F
      *> follows as a separate program of the same compilation group.
      *> Each program declares its own X (no GLOBAL), so each DISPLAY
      *> of X shows which unit the statement belongs to.
      *> DERIVATION OF EVERY EXPECTED LINE:
      *> D X=DDDD   C calls D (directly contained, 8.4.6.3 1)); D's
      *>            reference to X is to D's own X.
      *> E X=EEEE   END PROGRAM L1C08D ended D (GR1), so E is NOT
      *>            contained in D but directly contained in C; C's CALL
      *>            of E is therefore legal and runs E, whose X is E's.
      *> F X=FFFF   END PROGRAM L1C08C ended C (GR1), so F is a separate
      *>            program, callable by C, with its own X.
      *> C X=OUTER  back in C after the calls: C's own X is untouched.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "OUTER".
       PROCEDURE DIVISION.
       C-MAIN.
           CALL "L1C08D".
           CALL "L1C08E".
           CALL "L1C08F".
           DISPLAY "C X=" X.
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "DDDD".
       PROCEDURE DIVISION.
       D-MAIN.
           DISPLAY "D X=" X.
           EXIT PROGRAM.
       END PROGRAM L1C08D.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "EEEE".
       PROCEDURE DIVISION.
       E-MAIN.
           DISPLAY "E X=" X.
           EXIT PROGRAM.
       END PROGRAM L1C08E.
       END PROGRAM L1C08C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "FFFF".
       PROCEDURE DIVISION.
       F-MAIN.
           DISPLAY "F X=" X.
           EXIT PROGRAM.
       END PROGRAM L1C08F.
