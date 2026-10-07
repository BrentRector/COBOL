      *> kb/Work PB912 + PB842 - A CONDITION SELECTION SUBJECT IS ASSIGNED ITS TRUTH VALUE ONCE, and a
      *> user-defined function inside it is activated once per execution of the EVALUATE statement - never
      *> refused, never once per WHEN. Every expected line is derived from the rule text, never from a run.
      *>
      *> ISO 14.9.13.4 GR3 - "At the beginning of the execution of the EVALUATE statement, each selection subject
      *>   is evaluated and assigned a value, a range of values, or a truth value"; GR3 e) - "Any selection
      *>   subject specified by condition-1 is assigned a truth value according to the rules for evaluating
      *>   conditional expressions".
      *> ISO 8.8.4.3.2 - a simple boolean condition is "[ NOT ] boolean-expression-1", so NOT BW is condition-1;
      *>   8.8.4.3.4 GR2 - "The condition NOT boolean-expression-1 evaluates to the reverse truth-value of
      *>   boolean-expression-1".
      *> ISO 13.5.4 GR1 - "Data items in the working-storage section of a program that does not have the initial
      *>   attribute, a function, a factory, or an object are static data", and 14.6.2.3.2 1) places static data
      *>   in the initial state only "The first time the function, method, or program in which it is described is
      *>   activated in a run unit" - so W45CNT's counter counts its own activations across the run unit.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC:
      *> NOTBW=T   BW is boolean 0, so NOT BW is true; the TRUE arm (the second) matches.
      *> U1=T      W45CNT(1) returns its activation count, 1 on the first activation, so the subject
      *>           W45CNT(1) = 1 is TRUE and stays TRUE for every WHEN: the FALSE arm fails, the TRUE arm matches.
      *>           (A per-WHEN re-evaluation would have compared 2 = 1 at the second arm and printed U1=NONE.)
      *> P=EQ2     ISO 14.9.13.3 SR8 - a partial-expression object "is treated as though it were specified as
      *>           condition-2, where condition-2 is the conditional expression that results from preceding
      *>           partial-expression-1 by the selection subject" - the subject GR3 already evaluated once. The
      *>           second activation returns 2: > 5 is false, = 3 is false, = 2 is true. (Re-evaluating the
      *>           subject inside each rewritten condition would compare 3, 4, 5 and print P=OTHER.)
      *> CALLS=003 the two EVALUATEs activated W45CNT exactly once each; the DISPLAY's own reference is the third.
      *> PCLS=NOT  W-NUM's content is "12A": the partial class test NUMERIC reads the subject's CHARACTER CONTENT
      *>           (8.8.4.4), which is not numeric, so the NOT NUMERIC arm matches.
      *> PSIGN=NEG W-S = -2: POSITIVE is false, NEGATIVE is true.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W45CNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-CALLS PIC 9(3) VALUE 0.
       LINKAGE SECTION.
       01 A-IN PIC 9.
       01 R-OUT PIC 9(3).
       PROCEDURE DIVISION USING A-IN RETURNING R-OUT.
           ADD 1 TO W-CALLS
           MOVE W-CALLS TO R-OUT
           GOBACK.
       END FUNCTION W45CNT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB912CSUBJ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION W45CNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BW PIC 1 VALUE B"0".
       01 W-AN PIC X(3) VALUE "12A".
       01 W-NUM REDEFINES W-AN PIC 9(3).
       01 W-S PIC S9 VALUE -2.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE NOT BW
               WHEN FALSE DISPLAY "NOTBW=F"
               WHEN TRUE DISPLAY "NOTBW=T"
           END-EVALUATE.
           EVALUATE FUNCTION W45CNT(1) = 1
               WHEN FALSE DISPLAY "U1=F"
               WHEN TRUE DISPLAY "U1=T"
               WHEN OTHER DISPLAY "U1=NONE"
           END-EVALUATE.
           EVALUATE FUNCTION W45CNT(1)
               WHEN > 5 DISPLAY "P=GT5"
               WHEN = 3 DISPLAY "P=EQ3"
               WHEN = 2 DISPLAY "P=EQ2"
               WHEN OTHER DISPLAY "P=OTHER"
           END-EVALUATE.
           DISPLAY "CALLS=" FUNCTION W45CNT(0).
           EVALUATE W-NUM
               WHEN NUMERIC DISPLAY "PCLS=NUM"
               WHEN NOT NUMERIC DISPLAY "PCLS=NOT"
           END-EVALUATE.
           EVALUATE W-S
               WHEN POSITIVE DISPLAY "PSIGN=POS"
               WHEN NEGATIVE DISPLAY "PSIGN=NEG"
           END-EVALUATE.
           STOP RUN.
       END PROGRAM PB912CSUBJ.

