*>--- conformance-case
*> id: pb441-f3-perform-below-2023
*> rules: [GR-14.9.28.4-14]
*> source-format: free
*> portable: unverified
*>---
      *> The exception-checking (Format-3) PERFORM is a COBOL-2023 introduction (Annex E.3.3 item 36), so the
      *> §14.9.28.4 GR14 checking window this program's positive twin measures
      *> (conformance:2023/pb441_f3_gr14_window_emit_gated) has NO edition axis below it: the whole statement is
      *> refused at 1985, 2002 and 2014. This negative is that gate's witness — the positive golden runs only at
      *> 2023, and without this nothing would fail if the Format-3 grammar were silently admitted earlier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB441F3GATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM
               CONTINUE
           WHEN EC-USER-DEMO
               CONTINUE
           FINALLY
               ADD 9 TO N
           END-PERFORM.
           STOP RUN.
