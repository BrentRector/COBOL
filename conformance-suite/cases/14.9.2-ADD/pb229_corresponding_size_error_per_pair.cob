      *>--- conformance-case
      *> id: pb229_corresponding_size_error_per_pair
      *> rules: [GR-14.9.2.4-5, GR-14.9.44.4-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB229 - every implied statement of ADD / SUBTRACT
      *>   CORRESPONDING is a separate statement, so a size error in
      *>   one pair never stops the next, and the statement-level
      *>   outcome is taken after all of them.
      *> cite.py --check 14.9.2.4 "The results are the same as if the
      *>   user had referred to each pair of corresponding identifiers
      *>   in separate ADD statements" -> OK 14.9.2.4 5)
      *> cite.py --check 14.7.6 "the imperative-statement in the SIZE
      *>   ERROR phrase is executed after all of the implied statements
      *>   are completed" -> OK 14.7.6
      *> cite.py --check 14.7.6 "the level-3 EC-SIZE exception
      *>   condition for the last of the implied statements that raised
      *>   a size error condition is set to exist after all of the
      *>   implied statements are completed" -> OK 14.7.6
      *> Two kinds of size error, one per pair kind:
      *>   A  FLOAT-LONG 1E308 + 1E308 is past the binary64 range of
      *>      the intermediate: it is raised while the SUM is evaluated
      *>      (EC-SIZE-OVERFLOW, 14.7.5 no-phrase rule 3; the range is
      *>      checked, docs/CONFORMANCE.md DOC-A.1-179).
      *>   C  PIC 9(3) 999 + 1 = 1000 does not fit the receiver: it is
      *>      raised as the result is STORED (EC-SIZE-TRUNCATION,
      *>      14.7.5 no-phrase rule 4).
      *>   B  PIC 9(3) 10 + 5 = 15 raises nothing.
      *> With the phrase (14.7.5 phrase rule 2): A and C keep their
      *> values, B is stored, "SE" is displayed ONCE and the NOT
      *> phrase is ignored - whichever pair erred first. Before PB229
      *> the evaluation-time raise of A abandoned B and C: B stayed 10.
      *> Without the phrase, under EC-SIZE checking: ONE declarative
      *> run, AFTER all pairs (B already 015 inside it), naming the
      *> LAST erring pair's condition: G1 -> G2 orders A B C, so
      *> TRUNCATION; G3 -> G4 orders C B A, so OVERFLOW.
      *> SUBTRACT CORRESPONDING G5 FROM G6: A -1E308 - 1E308 overflows
      *> during evaluation, B 20 - 5 = 15 is still stored.
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB229CORRSIZE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 A USAGE FLOAT-LONG VALUE 1.0E308.
          05 B PIC 9(3) VALUE 5.
          05 C PIC 9(3) VALUE 1.
       01 G2.
          05 A USAGE FLOAT-LONG VALUE 1.0E308.
          05 B PIC 9(3) VALUE 10.
          05 C PIC 9(3) VALUE 999.
       01 G3.
          05 C PIC 9(3) VALUE 1.
          05 B PIC 9(3) VALUE 5.
          05 A USAGE FLOAT-LONG VALUE 1.0E308.
       01 G4.
          05 C PIC 9(3) VALUE 999.
          05 B PIC 9(3) VALUE 10.
          05 A USAGE FLOAT-LONG VALUE 1.0E308.
       01 G5.
          05 A USAGE FLOAT-LONG VALUE 1.0E308.
          05 B PIC 9(3) VALUE 5.
       01 G6.
          05 A USAGE FLOAT-LONG VALUE -1.0E308.
          05 B PIC 9(3) VALUE 20.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       SZ-P.
           DISPLAY "  DECLARATIVE " FUNCTION EXCEPTION-STATUS
                   " B2=" B OF G2 " B4=" B OF G4.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           ADD CORRESPONDING G1 TO G2
               ON SIZE ERROR DISPLAY "SE"
               NOT ON SIZE ERROR DISPLAY "NS"
           END-ADD
           PERFORM SHOW-G2
           MOVE 10 TO B OF G2
           DISPLAY "NO PHRASE G1 TO G2"
           ADD CORRESPONDING G1 TO G2
           PERFORM SHOW-G2
           DISPLAY "NO PHRASE G3 TO G4"
           ADD CORRESPONDING G3 TO G4
           DISPLAY "B=" B OF G4 " C=" C OF G4
           IF A OF G4 = 1.0E308 DISPLAY "A UNCHANGED" END-IF
           SUBTRACT CORRESPONDING G5 FROM G6
               ON SIZE ERROR DISPLAY "SE"
               NOT ON SIZE ERROR DISPLAY "NS"
           END-SUBTRACT
           DISPLAY "B=" B OF G6
           IF A OF G6 = -1.0E308 DISPLAY "A UNCHANGED" END-IF
           STOP RUN.
       SHOW-G2.
           DISPLAY "B=" B OF G2 " C=" C OF G2
           IF A OF G2 = 1.0E308 DISPLAY "A UNCHANGED" END-IF.
