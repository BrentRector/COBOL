*> reject-at: 85 2002 2014 2023
*> kb/Work PB397 - ISO/IEC 1989:2023 14.9.17.3 SR2: "If a GO TO statement represented by format 1 appears in a
*> consecutive sequence of imperative statements within a sentence, it shall appear as the last statement in that
*> sequence." No edition qualifier, so all four editions refuse. The sentence below has three statements and the
*> GO TO is the second; the DISPLAY after it can never execute, which is the symptom the rule exists to prevent
*> (a missing period) and which produced no diagnostic of any severity before the rule was asked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB397NEG2.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "START"  GO TO PARA-A  DISPLAY "AFTER-GO-TO".
       PARA-A.
           DISPLAY "A-TAKEN".
           STOP RUN.
