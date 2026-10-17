*>--- conformance-case
*> id: pb854_write_eop_exception_conditions
*> rules: [GR-14.9.51.4-27]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       >>TURN EC-I-O-EOP EC-I-O-EOP-OVERFLOW CHECKING ON
      *> kb/Work PB854 -- §14.9.51.4 GR27 a)'s two exception conditions
      *> are SET TO EXIST by an end-of-page WRITE, and GR27 b)-d) route
      *> them: to the END-OF-PAGE phrase when it is specified, otherwise
      *> to the applicable USE declarative.
      *>
      *> THE RULES.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   end-of-page condition was caused by the action in General rule
      *>   26a, the EC-I-O-EOP-OVERFLOW exception condition is set to
      *>   exist."                            -> OK  §14.9.51.4 27) a)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   END-OF-PAGE phrase is not specified, and there is an applicable
      *>   USE declarative, control is transferred to that declarative."
      *>                                      -> OK  §14.9.51.4 27) d)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   END-OF-PAGE phrase is specified, control is transferred to
      *>   imperative-statement-1."           -> OK  §14.9.51.4 27) b)
      *>
      *> THE PAGE. LINAGE IS 3 LINES WITH FOOTING AT 2: OPEN OUTPUT sets
      *> LINAGE-COUNTER to 1 (§13.18.34.4 GR7 d)); each plain WRITE is a
      *> one-line advance (§14.9.51.4 GR25, §13.18.34.4 GR7 c) 3).
      *>   W1 -> counter 2: in the footing area, GR26 b) -> EC-I-O-EOP
      *>   W2 -> counter 3: in the footing area, GR26 b) -> EC-I-O-EOP
      *>   W3 -> 4 exceeds the page body: overflow, GR26 a), counter 1
      *>         -> EC-I-O-EOP-OVERFLOW
      *>   W4 -> counter 2: GR26 b) -> EC-I-O-EOP
      *>   W5 (AT END-OF-PAGE phrase) -> counter 3: GR26 b); the PHRASE
      *>         runs (b)) and the declarative does NOT (d) is "If the
      *>         END-OF-PAGE phrase is not specified") -- but the
      *>         condition still EXISTS (a)), so EXCEPTION-STATUS inside
      *>         the phrase names it.
      *>   The counter==page-size boundary is kb/Work PB686's recorded
      *>   determination (docs/CONFORMANCE.md §4).
      *>
      *> WHY EACH LEG CAN FAIL. With no mask bit (the defect), no
      *> DECL line prints and EXCEPTION-STATUS is spaces; with the two
      *> arms confused, W3 prints DECL-EOP; with the phrase not taking
      *> precedence, a DECL line prints before PHRASE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB854EOP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb854.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF LINAGE IS 3 LINES WITH FOOTING AT 2.
       01 PREC PIC X(8).
       WORKING-STORAGE SECTION.
       01 N PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-EOP.
       D1P.
           DISPLAY "DECL-EOP " FUNCTION EXCEPTION-STATUS.
       D2 SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-EOP-OVERFLOW.
       D2P.
           DISPLAY "DECL-OVF " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT PRTF
           PERFORM VARYING N FROM 1 BY 1 UNTIL N > 4
               DISPLAY "W" N
               WRITE PREC FROM "LINE"
           END-PERFORM
           DISPLAY "W5"
           WRITE PREC FROM "EOPPH" AT END-OF-PAGE
               DISPLAY "PHRASE " FUNCTION EXCEPTION-STATUS END-WRITE
           CLOSE PRTF
           STOP RUN.
