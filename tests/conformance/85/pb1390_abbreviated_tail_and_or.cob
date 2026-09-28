      *> ISO §8.8.4.12 ABBREVIATED COMBINED RELATION CONDITIONS with an abbreviated relation after OR that the
      *> sequence then CONTINUES with AND (kb/Work PB1390). §8.8.4.12.1: "any relation condition except the first
      *> may be abbreviated"; §8.8.4.12.2 repeats ONE group {AND|OR} {NOT|relational-operator} object-1 with any
      *> connective in any position. Each case runs the abbreviated form over all 81 tuples of A..D in 1..3 and
      *> counts (a) the TRUE tuples and (b) the tuples where it disagrees with the §8.8.4.12.4 GR1 expansion
      *> ("as if the last preceding stated subject were inserted in place of the omitted subject, and the last
      *> stated relational operator ... in place of the omitted relational operator"). The TRUE counts were
      *> computed from the expansions, never read off a run; every disagreement count must be 00.
      *>   E1 A = B OR < C AND D        = (A=B) OR ((A<C) AND (A<D))       -- bare object after an OR-led
      *>                                  abbreviated relation (was COBOL0001 "unexpected D")
      *>   E2 ... AND NOT D             = ... AND NOT (A<D)
      *>   E3 A = B OR < D AND C = 1    = (A=B) OR ((A<D) AND (C=1))       -- GR1 termination: the stated
      *>                                  relation C = 1 ends the insertion
      *>   E4, E5 two of the GR1 NOTE examples (a > b AND NOT < c OR d; NOT (a NOT > b AND c AND NOT d))
      *>   E6 A > B OR < C AND < D OR = C;  E7 A = B OR = C AND D AND NOT = B
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68ABT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9.
       01 B PIC 9.
       01 C PIC 9.
       01 D PIC 9.
       01 N-TRUE PIC 99.
       01 N-BAD  PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B OR < C AND D
                ADD 1 TO N-TRUE
                IF NOT (
                  (A = B) OR ((A < C) AND (A < D))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (A = B) OR ((A < C) AND (A < D))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "E1 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B OR < C AND NOT D
                ADD 1 TO N-TRUE
                IF NOT (
                  (A = B) OR ((A < C) AND NOT (A < D))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (A = B) OR ((A < C) AND NOT (A < D))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "E2 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B OR < D AND C = 1
                ADD 1 TO N-TRUE
                IF NOT (
                  (A = B) OR ((A < D) AND (C = 1))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (A = B) OR ((A < D) AND (C = 1))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "E3 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A > B AND NOT < C OR D
                ADD 1 TO N-TRUE
                IF NOT (
                  ((A > B) AND (A NOT < C)) OR (A NOT < D)
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  ((A > B) AND (A NOT < C)) OR (A NOT < D)
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "E4 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                NOT (A NOT > B AND C AND NOT D)
                ADD 1 TO N-TRUE
                IF NOT (
                  NOT (((A NOT > B) AND (A NOT > C)) AND (NOT (A NOT >
                  D)))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  NOT (((A NOT > B) AND (A NOT > C)) AND (NOT (A NOT >
                  D)))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "E5 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A > B OR < C AND < D OR = C
                ADD 1 TO N-TRUE
                IF NOT (
                  (A > B) OR ((A < C) AND (A < D)) OR (A = C)
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (A > B) OR ((A < C) AND (A < D)) OR (A = C)
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "E6 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B OR = C AND D AND NOT = B
                ADD 1 TO N-TRUE
                IF NOT (
                  (A = B) OR ((A = C) AND (A = D) AND (A NOT = B))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (A = B) OR ((A = C) AND (A = D) AND (A NOT = B))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "E7 " N-TRUE " " N-BAD
           STOP RUN.
