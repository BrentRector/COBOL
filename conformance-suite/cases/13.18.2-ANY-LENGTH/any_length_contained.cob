*>--- conformance-case
*> id: any_length_contained
*> rules: [SR-13.18.2.3-3, GR-13.18.2.4-1, FMT-13.18.2.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §13.18.2 ANY LENGTH — a CONTAINED program's LINKAGE formal (SR2/SR3): two CALLs
      *> with different-width BY REFERENCE arguments; the callee's FUNCTION LENGTH tracks each caller
      *> argument (GR1 — the callee sees the CALLER's full string, never a Pic.Length window).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALCALLP9AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A3 PIC XXX VALUE "XYZ".
       01 A8 PIC X(8) VALUE "WXYZABCD".
       PROCEDURE DIVISION.
       MAIN.
           CALL "ALSUBP9AL" USING A3.
           CALL "ALSUBP9AL" USING A8.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALSUBP9AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 99.
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
       SUBMAIN.
           MOVE FUNCTION LENGTH(L) TO N.
           DISPLAY "LEN=" N " VAL=" L.
           EXIT PROGRAM.
       END PROGRAM ALSUBP9AL.
       END PROGRAM ALCALLP9AL.
