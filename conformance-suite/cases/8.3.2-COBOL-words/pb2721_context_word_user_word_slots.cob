      *>--- conformance-case
      *> id: pb2721_context_word_user_word_slots
      *> rules: [SR-8.3.2.1-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2721 + PB2732 - a user-defined word that is a
      *> context-sensitive word with a lexer token is the same word
      *> in its REFERENCE slot as in its DECLARING slot, and an
      *> index-name is a DEFINING word.
      *>  §8.3.2.1 3): "Context-sensitive words may be used as
      *>    user-defined words and system-names in contexts other
      *>    than the language construct in which they are defined"
      *>    (§8.10: CAPACITY OCCURS clause, CYCLE EXIT statement,
      *>    BYTES RECORD clause, FOREVER RETRY phrase).
      *>  §13.18.57.2 Format 1: TYPE TO type-name-1.
      *>  §8.4.3.2.2: [ FUNCTION ] function-prototype-name-1 ...
      *>  §13.18.38.2: [ INDEXED BY { index-name-1 } ... ]
      *> cite.py --check:
      *>  OK  §8.3.2.1 3)  (General)
      *>  OK  §8.10   (Context-sensitive words) | CAPACITY | CYCLE |
      *>      | BYTES | FOREVER |
      *>  OK  §13.18.57.2   (General formats)  TO type-name-1
      *>  OK  §8.4.3.2.2   (General format)  function-prototype-name-1
      *>  OK  §13.18.38.2   (General formats)  BY { index-name-1 }
      *> Derivation: TYPE CYCLE makes T7 a PIC 9(3) item, so MOVE 7
      *>  gives 007; TYPE TO BYTES makes T4 a PIC X(4) item.
      *>  FUNCTION CAPACITY(3) returns 3 * 2 = 6 into a PIC 9 item.
      *>  INDEXED BY FOREVER, I2 declares two indexes (the comma is
      *>  a separator, §8.3.5): SET FOREVER TO 2, SET I2 TO FOREVER,
      *>  MOVE "Q" TO TB (I2) changes the 2nd of "ZZZ" -> ZQZ.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. CAPACITY.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC 9.
       01 R PIC 9.
       PROCEDURE DIVISION USING A RETURNING R.
           COMPUTE R = A * 2
           GOBACK.
       END FUNCTION CAPACITY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2721US.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION CAPACITY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CYCLE TYPEDEF PIC 9(3).
       01 BYTES TYPEDEF PIC X(4).
       01 T7 TYPE CYCLE.
       01 T4 TYPE TO BYTES.
       01 V PIC 9 VALUE 3.
       01 G.
          05 TB PIC X OCCURS 3 INDEXED BY FOREVER, I2 VALUE "Z".
       PROCEDURE DIVISION.
           MOVE 7 TO T7
           MOVE "WXYZ" TO T4
           DISPLAY "TYPE=" T7 " " T4
           DISPLAY "FN=" FUNCTION CAPACITY(V)
           SET FOREVER TO 2
           SET I2 TO FOREVER
           MOVE "Q" TO TB (I2)
           DISPLAY "IX=" TB (1) TB (2) TB (3)
           STOP RUN.
       END PROGRAM PB2721US.
