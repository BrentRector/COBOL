*>--- conformance-case
*> id: pb1004_gr14_implicit_push_pop_all
*> rules: [GR-7.3.23.3-1, GR-14.9.28.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1004 - the implicit PUSH ALL / POP ALL that 14.9.28.4 GR14
      *> places around an exception-checking PERFORM's handlers saves and
      *> restores EVERY directive state, not only TURN. Before PB1004 the binder
      *> modelled only the TURN OFF ALL floor, so a directive written in a WHEN
      *> phrase stayed in force after END-PERFORM (L1 printed NONE, L3 OVF=1).
      *>
      *> THE RULES:
      *>   14.9.28.4 GR14 - "An implicit PUSH ALL followed by TURN OFF ALL is
      *>     assumed at the end of imperative-statement-1. Immediately preceding
      *>     the END PERFORM phrase, there is an implicit POP ALL ..."
      *>   7.3.22.4 GR2 - "If ALL is specified, the state of all of the
      *>     directives other than EVALUATE, IF, PAGE, POP, or PUSH are saved."
      *>   7.3.20.4 GR3 - POP ALL restores every directive state "previously
      *>     stored by a PUSH directive and ... not removed by a POP directive".
      *>   7.3.23.3 GR1 - REF-MOD-ZERO-LENGTH omitted or OFF: a zero-length
      *>     reference modification raises EC-BOUND-REF-MOD.
      *> THE OBSERVATIONS: R is RAISED when the probe PERFORM's WHEN
      *> EC-BOUND-REF-MOD caught S(1:N) with N = 0 (the directive is OFF there),
      *> NONE when the reference modification was allowed (ON); the handler
      *> RESUMEs, since EC-BOUND-REF-MOD is fatal. OVF counts the
      *> STRING overflows the USE declarative saw (14.6.13.1.4: unchecked, it
      *> is as if the exception did not occur).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   L1 R=RAISED  REF-MOD-ZERO-LENGTH ON is written in a WHEN phrase,
      *>                after the implicit PUSH ALL; the implicit POP ALL before
      *>                END-PERFORM restores the omitted (OFF) state.
      *>   L2 R=NONE    CONTROL: the same directive written in imperative-
      *>                statement-1 is BEFORE the PUSH ALL, so it is part of the
      *>                saved state and the POP ALL restores it - ON.
      *>   L3 OVF=0     the TURN sibling: a TURN ... CHECKING ON in a WHEN
      *>                phrase is discarded by the same POP ALL, so the STRING
      *>                after END-PERFORM is unchecked (7.3.25.4 GR1 default).
      *>                (That TURN also draws the 7.3.25.3 SR5 warning.)
      *> Directives sit at COLUMN 8 (column 7 is the fixed-form indicator area).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1004G14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3).
       01 S PIC X(5) VALUE "ABCDE".
       01 N PIC 9 VALUE 0.
       01 R PIC X(6).
       01 OVF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-OVF SECTION.
           USE AFTER EXCEPTION CONDITION EC-OVERFLOW-STRING.
       D-OVF-P.
           ADD 1 TO OVF.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
       >>REF-MOD-ZERO-LENGTH ON
               CONTINUE
           END-PERFORM
           MOVE "NONE" TO R
           PERFORM
               MOVE S(1:N) TO WS-D
           WHEN EC-BOUND-REF-MOD
               MOVE "RAISED" TO R
               RESUME NEXT STATEMENT
           END-PERFORM
           DISPLAY "L1 R=" R

           PERFORM
       >>REF-MOD-ZERO-LENGTH ON
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
               CONTINUE
           END-PERFORM
           MOVE "NONE" TO R
           PERFORM
               MOVE S(1:N) TO WS-D
           WHEN EC-BOUND-REF-MOD
               MOVE "RAISED" TO R
               RESUME NEXT STATEMENT
           END-PERFORM
           DISPLAY "L2 R=" R
       >>REF-MOD-ZERO-LENGTH OFF

           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
       >>TURN EC-OVERFLOW-STRING CHECKING ON
               CONTINUE
           END-PERFORM
           MOVE 0 TO OVF
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           DISPLAY "L3 OVF=" OVF
           STOP RUN.
