       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON WITH LOCATION
      *> kb/Work PB2745 - the statement and location FUNCTION EXCEPTION-STATEMENT
      *> and EXCEPTION-LOCATION answer belong to the statement that RAISED the
      *> condition, under the TURN in force at THAT statement's source text:
      *>   ISO 15.32.3 r2 (cite.py OK) "the name of the statement that caused the
      *>     exception condition to be raised"; r1 (cite.py OK) when LOCATION "is
      *>     not specified and the implementor does not save the location
      *>     information, the returned value is 63 spaces" (this implementation
      *>     saves nothing then - CONFORMANCE.md);
      *>   ISO 15.30.3 r1 (cite.py OK) "the returned value is one alphanumeric
      *>     space character";
      *>   ISO 7.3.25.4 GR6 (cite.py OK) checking "is enabled for the procedure
      *>     division statements and procedure division headers that follow in
      *>     the compilation group".
      *> The first TURN (WITH LOCATION) covers MAIN-P's statements; the second
      *> (no LOCATION) covers P-LATER and the CALLed program PB2745S. Each
      *> MOVE E(I) TO Y subscripts out of range (I = 5, OCCURS 3), raising
      *> EC-BOUND-SUBSCRIPT; each program's declarative displays the pair and
      *> RESUMEs at the next statement.
      *> DERIVATION:
      *>   line 1 (PB2745S's MOVE, no LOCATION): 63 spaces; line 2: one space.
      *>     Before the fix the CALL statement's context leaked across the
      *>     activation: "CALL" and the CALL's location.
      *>   lines 3-4 (P-LATER's MOVE, PERFORMed from a WITH LOCATION statement,
      *>     no LOCATION itself): 63 spaces and one space (the PERFORM's context
      *>     leaked before the fix).
      *>   lines 5-6 (the control: MAIN-P's MOVE, WITH LOCATION): "MOVE" and its
      *>     location in this implementation's form "<program>; <paragraph> OF
      *>     <section>; <line>" (15.30.3 r2): "PB2745M; MAIN-P OF MAIN-S; 51".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2745M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 E PIC X VALUE "A" OCCURS 3 TIMES.
       01 I PIC 99 VALUE 5.
       01 Y PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-M SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-M-P.
           DISPLAY "M S=[" FUNCTION EXCEPTION-STATEMENT "]"
           DISPLAY "M L=[" FUNCTION EXCEPTION-LOCATION "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           CALL "PB2745S"
           PERFORM P-LATER
           MOVE E(I) TO Y
           STOP RUN.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       P-LATER.
           MOVE E(I) TO Y.
       END PROGRAM PB2745M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2745S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 E PIC X VALUE "A" OCCURS 3 TIMES.
       01 I PIC 99 VALUE 5.
       01 Y PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-S SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D-S-P.
           DISPLAY "S S=[" FUNCTION EXCEPTION-STATEMENT "]"
           DISPLAY "S L=[" FUNCTION EXCEPTION-LOCATION "]"
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           MOVE E(I) TO Y
           GOBACK.
       END PROGRAM PB2745S.
