      *> ISO 14.2.2 using-phrase / 14.2.3 GR4+GR10 / 8.4.3.2.4 GR5c - BY VALUE formal parameters in
      *> the procedure division header, on BOTH activation paths (a user-defined function reference
      *> and a CALL target - one shared ABI): the activated element operates on a VALUE COPY (a
      *> detached cell conformed to the formal, 14.2.3 GR10), so a change to the caller's argument
      *> NEVER reaches the formal; a BY REFERENCE formal in the same header (GR4 transitivity) occupies
      *> the argument's storage (GR8) and DOES see it. 13.7.3 SR5 forbids a FUNCTION to store into its
      *> formal parameter, so SCALEV changes both arguments through the EXTERNAL items WS-A and WS-B
      *> the caller describes too: R = L-V * 2 + (L-REF - before - 1) is L-V * 2 only if the VALUE copy
      *> kept its 7 / 3 while WS-A became 9999 and the REFERENCE formal followed WS-B up by one.
      *> A literal argument to a BY VALUE formal is 8.4.3.2.3 SR10-legal (numeric).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. SCALEV-P10UV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) EXTERNAL.
       01 WS-B PIC 9(4) EXTERNAL.
       01 W-BEFORE PIC 9(4).
       01 W-DELTA PIC S9(4).
       LINKAGE SECTION.
       01 L-V PIC 9(4).
       01 L-REF PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-V BY REFERENCE L-REF
           RETURNING L-R.
       P.
           MOVE L-REF TO W-BEFORE.
      *>   the argument of the VALUE formal changes - the detached copy must NOT follow (14.2.3 GR10)
           MOVE 9999 TO WS-A.
      *>   the argument of the REFERENCE formal changes - the formal occupies its storage and MUST follow (GR8)
           ADD 1 TO WS-B.
           COMPUTE W-DELTA = L-REF - W-BEFORE - 1.
           COMPUTE L-R = L-V * 2 + W-DELTA.
           GOBACK.
       END FUNCTION SCALEV-P10UV.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UBYVAL-P10UV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SCALEV-P10UV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4) EXTERNAL.
       01 WS-B PIC 9(4) EXTERNAL.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 7 TO WS-A.
           MOVE 100 TO WS-B.
      *>   an identifier argument to the BY VALUE formal: changed while the function runs, the copy keeps 7
           COMPUTE WS-R = FUNCTION SCALEV-P10UV(WS-A, WS-B).
           DISPLAY "R1=" WS-R.
           DISPLAY "A1=" WS-A.
           DISPLAY "B1=" WS-B.
      *>   a literal argument to the BY VALUE formal (SR10 - class numeric)
           COMPUTE WS-R = FUNCTION SCALEV-P10UV(3, WS-B).
           DISPLAY "R2=" WS-R.
           DISPLAY "B2=" WS-B.
      *>   the CALL leg: the same header shape on a called subprogram
           MOVE 7 TO WS-A.
           MOVE 100 TO WS-B.
           CALL "SUBV-P10UV" AS NESTED USING WS-B BY VALUE WS-A.
           DISPLAY "A3=" WS-A.
           DISPLAY "B3=" WS-B.
           STOP RUN.
      *> kb/Work PB131 - AS NESTED requires CONTAINMENT (§14.9.4.3 SR15 sentence 2, enforced at bind).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBV-P10UV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 S-REF PIC 9(4).
       01 S-V PIC 9(4).
       PROCEDURE DIVISION USING S-REF BY VALUE S-V.
       P.
           MOVE 8888 TO S-V.
           ADD 5 TO S-REF.
           GOBACK.
       END PROGRAM SUBV-P10UV.
       END PROGRAM UBYVAL-P10UV.

