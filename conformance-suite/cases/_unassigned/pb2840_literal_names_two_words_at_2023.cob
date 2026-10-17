*>--- conformance-case
*> id: pb2840_literal_names_two_words_at_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2840 - the 2023 side of pb2840_literal_names_edition_spelled:
      *> Annex E.2 item 14 deleted (03C2,03C3) at 2023, so FINAL SIGMA and
      *> SIGMA are two words (ISO 8.1.3.2 GR4 b), and a literal naming a
      *> word names exactly the word it is spelled as:
      *>   1. CALL "Zς" finds no program (PROGRAM-ID. Zσ is another
      *>      word); 14.9.4.4 GR3 b: the program cannot be located, the
      *>      ON EXCEPTION imperative runs.
      *>   2. CALL "Zσ" finds it (the control: the same literal spelled
      *>      as the word).
      *>   3. CALL "Wσ" finds no program: PROGRAM-ID. PB2840V AS
      *>      "Wς" is externalized as the final-sigma name (8.3.2.2 2).
      *> Each leg fails if the pre-2023 spelling were applied at 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2840N.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "Zς"
               ON EXCEPTION DISPLAY "NOT FOUND FINAL"
               NOT ON EXCEPTION DISPLAY "FOUND FINAL"
           END-CALL.
           CALL "Zσ"
               ON EXCEPTION DISPLAY "NOT FOUND SIGMA"
               NOT ON EXCEPTION DISPLAY "FOUND SIGMA"
           END-CALL.
           CALL "Wσ"
               ON EXCEPTION DISPLAY "NOT FOUND AS"
               NOT ON EXCEPTION DISPLAY "FOUND AS"
           END-CALL.
           STOP RUN.
       END PROGRAM PB2840N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. Zσ.
       PROCEDURE DIVISION.
       MAIN-P.
           GOBACK.
       END PROGRAM Zσ.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2840V AS "Wς".
       PROCEDURE DIVISION.
       MAIN-P.
           GOBACK.
       END PROGRAM PB2840V.
