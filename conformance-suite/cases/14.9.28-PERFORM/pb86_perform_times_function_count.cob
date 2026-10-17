*>--- conformance-case
*> id: pb86_perform_times_function_count
*> rules: [SR-14.9.28.3-2, GR-14.9.28.4-7, GR-15.2-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB86. §14.9.28.2 Format 2: `PERFORM proc {identifier-1 |
      *> integer-1} TIMES`; §8.4.3.2.4 GR1 makes a function-identifier an
      *> identifier referencing a temporary item, and §14.9.28.3 SR2 asks only
      *> that identifier-1 be an integer — met by an integer-type function
      *> (§15.2 type 5). The keyword-omitted spelling (§8.4.3.2 SR2, FUNCTION ALL
      *> INTRINSIC) bound and ran the body ONCE (the emitter's `_ => "1"`
      *> default); the FUNCTION spelling was a parse error. GR7: the count is
      *> determined once. Expected counts are the functions' §15 values:
      *> INTEGER(3.7) = 3 (§15.44 — the greatest integer ≤ 3.7),
      *> INTEGER-PART(2.9) = 2 (§15.49), LENGTH("ABCD") = 4 (§15.50),
      *> MOD(7, 3) = 1 (§15.64), and the identifier/literal controls. A
      *> trailing-P item (PIC 9P VALUE 20) IS an integer (no digit position right
      *> of the decimal point) and counts by VALUE, 20 — not by its stored digit;
      *> a GO TO … DEPENDING ON selector reads the same way (§14.9.17 — value 10
      *> is out of range for two procedure-names, so control falls through).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB86TIMES.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT     PIC 99 VALUE 0.
       01 N3      PIC 9 VALUE 3.
       01 NP      PIC 9P VALUE 20.
       01 NB      PIC 9(4) COMP-5 VALUE 4.
       01 SEL     PIC 9P VALUE 10.
       PROCEDURE DIVISION.
           PERFORM COUNT-IT INTEGER(3.7) TIMES.
           DISPLAY "T1 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM COUNT-IT FUNCTION INTEGER-PART(2.9) TIMES.
           DISPLAY "T2 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM COUNT-IT LENGTH("ABCD") TIMES.
           DISPLAY "T3 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM COUNT-IT THRU COUNT-IT-X FUNCTION MOD(7, 3) TIMES.
           DISPLAY "T4 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM INTEGER(2.5) TIMES
              ADD 1 TO CNT
           END-PERFORM.
           DISPLAY "T5 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM COUNT-IT N3 TIMES.
           DISPLAY "T6 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM COUNT-IT NP TIMES.
           DISPLAY "T7 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM COUNT-IT NB TIMES.
           DISPLAY "T8 CNT=" CNT.
           MOVE 0 TO CNT.
           PERFORM COUNT-IT 2 TIMES.
           DISPLAY "T9 CNT=" CNT.
           GO TO L1 L2 DEPENDING ON SEL.
           DISPLAY "T10 FELL THROUGH".
           STOP RUN.
       L1.
           DISPLAY "T10 L1".
           STOP RUN.
       L2.
           DISPLAY "T10 L2".
           STOP RUN.
       COUNT-IT.
           ADD 1 TO CNT.
       COUNT-IT-X.
           CONTINUE.
