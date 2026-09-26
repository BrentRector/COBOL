      *> ISO/IEC 1989:2023 §8.4.3.2 — a function-identifier written with FUNCTION-POINTER-NAME-1, the consumer
      *> SET Format 8 (kb/Work PB452) landed without. kb/Work PB847.
      *>
      *> §8.4.3.2.2 — the function-identifier's name is function-pointer-name-1, function-prototype-name-1 or
      *>   intrinsic-function-name-1; §8.4.3.2.3 SR4 — "Function-pointer-name-1 shall be defined as a
      *>   function-pointer data item"; SR2 — with function-pointer-name-1 "the word FUNCTION may be omitted";
      *>   SR5 — "If function-pointer-name-1 is specified, the parentheses shall be specified."
      *> §8.4.3.2.4 GR1 — the result's description is that of the RETURNING item "of the function prototype
      *>   identified by the TO phrase of the USAGE clause in the definition of function-pointer-name-1".
      *> §8.4.3.2.4 GR4 — that prototype determines "the characteristics of the activated element", and GR6c —
      *>   "the runtime system attempts to execute the function at the address pointed to by
      *>   function-pointer-name-1. If function-pointer-name-1 is NULL, the EC-FUNCTION-PTR-NULL exception
      *>   condition is set to exist, no function is activated".
      *> §14.9.39.4 GR14 — SET Format 8 stores a same-signature function's address; PBTRP has PBDBP's signature.
      *>
      *> EXPECTED OUTPUT, derived line by line:
      *>   A=000000010  FP holds PBDBP's address; FUNCTION FP(5) activates it: 5 * 2.
      *>   B=000000015  FP re-SET to PBTRP (same signature, GR14); FP(5) — FUNCTION omitted (SR2) — activates
      *>                the function the pointer HOLDS (GR6c), not the prototype its TO phrase names: 5 * 3.
      *>   C=IF         FP(2) = 6 in an IF condition (the per-evaluation activation path): 2 * 3 = 6.
      *>   D=3          PERFORM VARYING IA FROM 1 UNTIL FP(IA) > 7 — the condition is evaluated before each pass
      *>                (§14.9.28.4), activating PBTRP each time: 3, 6, 9 — so the loop stops with IA = 3 (moved to I).
      *>   Z=000000077  FUNCTION FZ() — a zero-argument function through a pointer; SR5's parentheses written.
      *>   H: EC-FUNCTION-PTR-NULL  FP is SET TO NULL; FP(5) raises GR6c's condition (checking is ON), and the
      *>                declarative associated with it runs (GR6f), reporting it through EXCEPTION-STATUS …
      *>   AFTER        … and RESUME AT NEXT STATEMENT continues after the statement that raised it (§14.9.33.4
      *>                GR2 a) 2.).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBDBP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 2
           GOBACK.
       END FUNCTION PBDBP.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBTRP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 3
           GOBACK.
       END FUNCTION PBTRP.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PBZRP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION RETURNING L-RES.
           MOVE 77 TO L-RES
           GOBACK.
       END FUNCTION PBZRP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB847FP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PBDBP
           FUNCTION PBTRP
           FUNCTION PBZRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO PBDBP.
       01 FZ USAGE FUNCTION-POINTER TO PBZRP.
       01 R PIC 9(9).
       01 I PIC 9.
      *> IA is passed BY REFERENCE (8.4.3.2.4 GR5 a - a receivable
      *> identifier) to the formal L-ARG PIC S9(4), and 8.4.3.2.3 SR13
      *> imports 14.8.2.3.2 rule 2: the two shall have the same PICTURE and
      *> SIGN clauses. The loop varied I PIC 9 itself until kb/Work PB1418
      *> made the function arm enforce the rule (COBOLNET2470); I still
      *> receives the final value, so the output is unchanged.
       01 IA PIC S9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-NULL SECTION.
           USE AFTER EXCEPTION CONDITION EC-FUNCTION-PTR-NULL.
       D-NULL-P.
           DISPLAY "H: " FUNCTION TRIM(FUNCTION EXCEPTION-STATUS)
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           >>TURN EC-FUNCTION-PTR-NULL CHECKING ON
           SET FP TO ADDRESS OF FUNCTION PBDBP
           COMPUTE R = FUNCTION FP(5)
           DISPLAY "A=" R
           SET FP TO ADDRESS OF FUNCTION PBTRP
           COMPUTE R = FP(5)
           DISPLAY "B=" R
           IF FP(2) = 6
               DISPLAY "C=IF"
           ELSE
               DISPLAY "C=ELSE"
           END-IF
           PERFORM VARYING IA FROM 1 BY 1 UNTIL FP(IA) > 7
               CONTINUE
           END-PERFORM
           MOVE IA TO I
           DISPLAY "D=" I
           SET FZ TO ADDRESS OF FUNCTION PBZRP
           COMPUTE R = FUNCTION FZ()
           DISPLAY "Z=" R
           SET FP TO NULL
           COMPUTE R = FP(5)
           DISPLAY "AFTER"
           STOP RUN.
       END PROGRAM PB847FP.
