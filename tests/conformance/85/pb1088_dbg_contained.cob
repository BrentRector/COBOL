      *> kb/Work PB1088 — a contained program inherits its container's SOURCE-COMPUTER ... WITH DEBUGGING MODE.
      *> ISO/IEC 1989:2023 §12.3.5.4 GR1: "All clauses of the SOURCE-COMPUTER paragraph apply to the source unit in
      *> which they are explicitly or implicitly specified and to any source unit contained within that source
      *> unit", and §12.3.3 SR1 forbids the contained program its own configuration section, so inheritance is the
      *> clause's only route into it (the X3.23-1985 debug module carries the same containment rule).
      *> With the switch inherited, the contained program's USE FOR DEBUGGING section is COMPILED (not comment
      *> lines); the object-time switch is ON (the wavef_dbg_proc posture), so the declarative runs before the
      *> contained program's first nondeclarative procedure. Hand-derived stdout:
      *>   OUTER                                  (the container, before the CALL)
      *>   N=P-IN ... C=START PROGRAM             (first execution of SCINNER's first nondeclarative procedure)
      *>   INNER                                  (P-IN's own DISPLAY)
      *> Before the fix the contained unit reset the switch to its own (absent) clause: OUTER / INNER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1088-OUTER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       P-OUT.
           DISPLAY "OUTER".
           CALL "PB1088-INNER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1088-INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM   PIC X(30).
       01 CONT PIC X(13).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ALL PROCEDURES.
       DBG-BODY.
           MOVE DEBUG-NAME     TO NM.
           MOVE DEBUG-CONTENTS TO CONT.
           DISPLAY "N=" NM "C=" CONT.
       END DECLARATIVES.
       MAIN SECTION.
       P-IN.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM PB1088-INNER.
       END PROGRAM PB1088-OUTER.
