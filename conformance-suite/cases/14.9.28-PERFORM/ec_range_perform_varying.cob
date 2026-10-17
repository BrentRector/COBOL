*>--- conformance-case
*> id: ec_range_perform_varying
*> rules: [GR-14.9.28.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> EC-RANGE-PERFORM-VARYING (ISO §14.9.28.4 GR3, spec :29222; Table 13 Fatal): when a PERFORM VARYING/AFTER
      *> initializes an INDEX-NAME from an IDENTIFIER FROM operand whose value is NOT POSITIVE (<= 0). NARROW — only
      *> an index-name target with an IDENTIFIER (not literal, not index-name) FROM; a data-item induction variable
      *> and a literal FROM are out of GR3 scope. Under >>TURN … CHECKING ON the USE declarative catches the fatal EC
      *> and RESUME AT NEXT STATEMENT continues (§14.9.33). The FROM value is tested (GR3 — the item, not the index).
      *>
      *> ⛔ "IDENTIFIER" IS EVERY §8.4.3.1.2 IDENTIFIER FORMAT, NOT ONE OF THEM (kb/Work PB439). P7 is the leg that
      *> says so: a FUNCTION-IDENTIFIER FROM operand — §8.4.3.2.4 GR1, "a function-identifier references a temporary
      *> data item whose value is determined when the function is referenced at runtime" — is an identifier, so GR3
      *> governs it. It used to take the unchecked path (the guard tested the bound node's C# type), so the FATAL EC
      *> was never set to exist and the index was left holding occurrence 0.
      >>TURN EC-RANGE-PERFORM-VARYING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-RNG-PV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 5 TIMES INDEXED BY IX.
       01 WS-U.
          05 WS-F PIC 9 OCCURS 5 TIMES INDEXED BY JX.
       01 WS-ZERO PIC S9 VALUE 0.
       01 WS-NEG  PIC S9 VALUE -1.
       01 WS-POS  PIC S9 VALUE 1.
       01 WS-DVAR PIC S9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-RANGE-PERFORM-VARYING.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
      *> positive control: index-name FROM data item = 1 -> no raise.
           DISPLAY "P1-POS".
           PERFORM VARYING IX FROM WS-POS BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P1-DONE".
      *> RAISE: index-name FROM data item = 0 -> fatal, caught, RESUME.
           DISPLAY "P2-ZERO".
           PERFORM VARYING IX FROM WS-ZERO BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P2-AFTER".
      *> RAISE: index-name FROM data item = -1 -> fatal, caught, RESUME.
           DISPLAY "P3-NEG".
           PERFORM VARYING IX FROM WS-NEG BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P3-AFTER".
      *> control: a LITERAL FROM is out of GR3 scope (GR3's premise is "an identifier is specified in the
      *> associated FROM phrase"), so no raise. ⛔ THE LITERAL IS 1, NOT 0, AND IT HAS TO BE (kb/Work PB432):
      *> §14.9.28.3 SR4 b) says "The literal in the associated FROM phrase shall be a positive integer" whenever an
      *> index-name is varied, so `FROM 0` here is not conforming source at all — this leg used to write it and
      *> assert it compiled clean, which is the SR4 b) hole itself. GR3's exclusion of literals is therefore
      *> unobservable in conforming source when the target is an index-name; what P4 pins is that the literal arm
      *> emits no check, and pb432-varying-operand-rules is the negative that pins SR4 b).
           DISPLAY "P4-LIT".
           PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.
           DISPLAY "P4-DONE".
      *> control: data-item induction variable FROM 0 (not an index-name) -> out of GR3 scope, no raise.
           DISPLAY "P5-DVAR".
           PERFORM VARYING WS-DVAR FROM WS-ZERO BY 1 UNTIL WS-DVAR > 3 CONTINUE END-PERFORM.
           DISPLAY "P5-DONE".
      *> RAISE: AFTER-level index-name FROM data item = 0 -> fatal, caught, RESUME.
           DISPLAY "P6-AFTERLVL".
           PERFORM VARYING IX FROM WS-POS BY 1 UNTIL IX > 1
                   AFTER JX FROM WS-ZERO BY 1 UNTIL JX > 1
               CONTINUE
           END-PERFORM.
           DISPLAY "P6-AFTER".
      *> RAISE: index-name FROM a FUNCTION-IDENTIFIER whose result is 0 -> fatal, caught, RESUME (kb/Work PB439).
           DISPLAY "P7-FN".
           PERFORM VARYING IX FROM FUNCTION INTEGER(WS-ZERO) BY 1 UNTIL IX > 3
               CONTINUE
           END-PERFORM.
           DISPLAY "P7-AFTER".
      *> control: index-name FROM a FUNCTION-IDENTIFIER whose result is POSITIVE -> no raise (the guard is
      *> emitted for every identifier form, and it tests the VALUE).
           DISPLAY "P8-FNPOS".
           PERFORM VARYING IX FROM FUNCTION INTEGER(WS-POS) BY 1 UNTIL IX > 3
               CONTINUE
           END-PERFORM.
           DISPLAY "P8-DONE".
           STOP RUN.
