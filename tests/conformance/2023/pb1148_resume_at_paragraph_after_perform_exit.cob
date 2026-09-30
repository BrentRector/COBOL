      *> PB1148 - ISO 14.9.33.4 GR3: "If procedure-name-1 is specified,
      *>   control is transferred to procedure-name-1 as if a GO TO
      *>   procedure-name-1 were executed." A GO TO out of a PERFORM's exit
      *>   paragraph does not return to the PERFORM (14.6.3 rule 1 / 14.9.28.4
      *>   GR5), so a RESUME AT the paragraph that physically follows it does
      *>   not either.
      *> cite.py --check 14.9.33.4 "If procedure-name-1 is specified,
      *>   control is transferred to procedure-name-1 as if a GO TO
      *>   procedure-name-1 were executed" -> OK  14.9.33.4 3)
      *> Derivation: P1 (the PERFORM's exit paragraph) raises
      *>   EC-BOUND-SUBSCRIPT (5 is past the 3 occurrences, checking ON);
      *>   the declarative displays H and resumes AT NEXT-P, the paragraph
      *>   physically next. Control stays at NEXT-P: BACK is never displayed.
      *>   Output: P1, H, NEXT-P.
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1148R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC 9(2) OCCURS 3 TIMES.
       01 IDX PIC 9(2) VALUE 5.
       01 R  PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "H".
           RESUME AT NEXT-P.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           PERFORM P1
           DISPLAY "BACK"
           STOP RUN.
       P1.
           DISPLAY "P1"
           MOVE T (IDX) TO R.
       NEXT-P.
           DISPLAY "NEXT-P"
           STOP RUN.
