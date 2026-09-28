      *> ISO §8.8.4.12 ABBREVIATED COMBINED RELATION CONDITIONS after the COBOL-2023 EXCLUSIVE-OR / XOR
      *> connective (kb/Work PB1390, PB1371, PB1392). §8.8.4.12.2 lists AND, OR, EXCLUSIVE-OR and XOR as the
      *> connective of the ONE repeated group; precedence NOT > AND > XOR > OR (§8.8.4.11.3). Each X case counts
      *> over all 81 tuples of A..D in 1..3 the TRUE tuples and the disagreements with the §8.8.4.12.4 GR1
      *> expansion (must be 00), the expansion writing XOR out as §8.8.4.9 defines it ("true if one but not
      *> both") so the check does not lean on the operator under test; the counts were computed from it:
      *>   X1 A = B XOR < C = (A=B) XOR (A<C)      X2 A = B XOR NOT < C = (A=B) XOR (A NOT < C)
      *>   X3 A = B OR < C XOR = D = (A=B) OR ((A<C) XOR (A=D))
      *>   X4 the GR1 NOTE example NOT (a NOT > b XOR c AND NOT d)
      *>   X5 A = B XOR C (bare object)            X6 A > B XOR < C AND D = (A>B) XOR ((A<C) AND (A<D))
      *>   X7 A = B EXCLUSIVE-OR = C OR > D = ((A=B) XOR (A=C)) OR (A>D)
      *> P1-P3: EVALUATE partial expressions (§14.9.13.3 SR8 - the object preceded by the subject is an
      *> ordinary condition) with XOR and an OR-led abbreviated tail, counted over A in 1..3.
      *> K1-K5: the >>IF constant-conditional-expression is "a complex condition as specified in 8.8.4.9"
      *> (§7.3.8.2 SR1 d)), so XOR / EXCLUSIVE-OR are its connectives too (were COBOLNET1619):
      *>   K1 1 = 2 XOR 1 = 1 = TRUE; K2 1 = 1 EXCLUSIVE-OR 1 = 1 = FALSE;
      *>   K3 1 = 1 OR 1 = 2 XOR 1 = 1 = T OR (F XOR T) = TRUE (OR-first grouping would give FALSE);
      *>   K4 1 = 1 XOR 1 = 1 AND 1 = 2 = T XOR (T AND F) = TRUE (XOR-first grouping would give FALSE);
      *>   K5 NOT 1 = 2 XOR 1 = 2 = (NOT F) XOR F = TRUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68ABX.
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
                A = B XOR < C
                ADD 1 TO N-TRUE
                IF NOT (
                  (((A = B) AND NOT (A < C)) OR (NOT (A = B) AND (A <
                  C)))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (((A = B) AND NOT (A < C)) OR (NOT (A = B) AND (A <
                  C)))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "X1 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B XOR NOT < C
                ADD 1 TO N-TRUE
                IF NOT (
                  (((A = B) AND NOT (A NOT < C)) OR (NOT (A = B) AND
                  (A NOT < C)))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (((A = B) AND NOT (A NOT < C)) OR (NOT (A = B) AND
                  (A NOT < C)))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "X2 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B OR < C XOR = D
                ADD 1 TO N-TRUE
                IF NOT (
                  (A = B) OR (((A < C) AND NOT (A = D)) OR (NOT (A <
                  C) AND (A = D)))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (A = B) OR (((A < C) AND NOT (A = D)) OR (NOT (A <
                  C) AND (A = D)))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "X3 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                NOT (A NOT > B XOR C AND NOT D)
                ADD 1 TO N-TRUE
                IF NOT (
                  NOT (((A NOT > B) AND NOT ((A NOT > C) AND NOT (A
                  NOT > D))) OR (NOT (A NOT > B) AND ((A NOT > C) AND
                  NOT (A NOT > D))))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  NOT (((A NOT > B) AND NOT ((A NOT > C) AND NOT (A
                  NOT > D))) OR (NOT (A NOT > B) AND ((A NOT > C) AND
                  NOT (A NOT > D))))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "X4 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B XOR C
                ADD 1 TO N-TRUE
                IF NOT (
                  (((A = B) AND NOT (A = C)) OR (NOT (A = B) AND (A =
                  C)))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (((A = B) AND NOT (A = C)) OR (NOT (A = B) AND (A =
                  C)))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "X5 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A > B XOR < C AND D
                ADD 1 TO N-TRUE
                IF NOT (
                  (((A > B) AND NOT ((A < C) AND (A < D))) OR (NOT (A
                  > B) AND ((A < C) AND (A < D))))
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (((A > B) AND NOT ((A < C) AND (A < D))) OR (NOT (A
                  > B) AND ((A < C) AND (A < D))))
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "X6 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE N-BAD
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
            AFTER B FROM 1 BY 1 UNTIL B > 3
            AFTER C FROM 1 BY 1 UNTIL C > 3
            AFTER D FROM 1 BY 1 UNTIL D > 3
             IF
                A = B EXCLUSIVE-OR = C OR > D
                ADD 1 TO N-TRUE
                IF NOT (
                  (((A = B) AND NOT (A = C)) OR (NOT (A = B) AND (A =
                  C))) OR (A > D)
                   ) ADD 1 TO N-BAD END-IF
             ELSE
                IF
                  (((A = B) AND NOT (A = C)) OR (NOT (A = B) AND (A =
                  C))) OR (A > D)
                   ADD 1 TO N-BAD END-IF
             END-IF
           END-PERFORM
           DISPLAY "X7 " N-TRUE " " N-BAD
           MOVE 0 TO N-TRUE
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
             EVALUATE A
               WHEN > 1 XOR < 3 ADD 1 TO N-TRUE
               WHEN OTHER CONTINUE
             END-EVALUATE
           END-PERFORM
           DISPLAY "P1 " N-TRUE
           MOVE 0 TO N-TRUE
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
             EVALUATE A
               WHEN < 3 AND 2 OR = 3 ADD 1 TO N-TRUE
               WHEN OTHER CONTINUE
             END-EVALUATE
           END-PERFORM
           DISPLAY "P2 " N-TRUE
           MOVE 0 TO N-TRUE
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
             EVALUATE A
               WHEN = 1 OR > 2 XOR < 3 ADD 1 TO N-TRUE
               WHEN OTHER CONTINUE
             END-EVALUATE
           END-PERFORM
           DISPLAY "P3 " N-TRUE
       >>IF 1 = 2 XOR 1 = 1
           DISPLAY "K1 TRUE"
       >>ELSE
           DISPLAY "K1 FALSE"
       >>END-IF
       >>IF 1 = 1 EXCLUSIVE-OR 1 = 1
           DISPLAY "K2 TRUE"
       >>ELSE
           DISPLAY "K2 FALSE"
       >>END-IF
       >>IF 1 = 1 OR 1 = 2 XOR 1 = 1
           DISPLAY "K3 TRUE"
       >>ELSE
           DISPLAY "K3 FALSE"
       >>END-IF
       >>IF 1 = 1 XOR 1 = 1 AND 1 = 2
           DISPLAY "K4 TRUE"
       >>ELSE
           DISPLAY "K4 FALSE"
       >>END-IF
       >>IF NOT 1 = 2 XOR 1 = 2
           DISPLAY "K5 TRUE"
       >>ELSE
           DISPLAY "K5 FALSE"
       >>END-IF
           STOP RUN.
