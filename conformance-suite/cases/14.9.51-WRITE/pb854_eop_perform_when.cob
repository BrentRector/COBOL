*>--- conformance-case
*> id: pb854_eop_perform_when
*> rules: [GR-14.9.51.4-27]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>TURN EC-I-O-EOP EC-I-O-EOP-OVERFLOW CHECKING ON
      *> kb/Work PB854 -- §14.9.51.4 GR27 c): the exception-checking
      *> PERFORM's WHEN leg of the end-of-page routing. Its declarative
      *> and phrase legs are pinned at their introducing edition by
      *> conformance:2002/pb854_write_eop_exception_conditions; the
      *> exception-checking PERFORM is COBOL-2023, so this leg lives here.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "and the WRITE
      *>   statement is specified in a statement that is in
      *>   imperative-statement-1 of an exception-checking PERFORM
      *>   statement"                          -> OK  §14.9.51.4 27) c)
      *>
      *> LINAGE IS 3 LINES WITH FOOTING AT 2, counter 1 at OPEN OUTPUT
      *> (§13.18.34.4 GR7 d)), one line per plain WRITE (GR7 c) 3):
      *>   L1 -> 2, GR26 b): EC-I-O-EOP; no END-OF-PAGE phrase, so the
      *>         matching WHEN takes it (c)) and the USE declarative for
      *>         the same name does NOT run (§14.9.28.4 GR17: "Any USE
      *>         declarative that would normally match ... is ignored").
      *>   L2 -> 3, GR26 b): the END-OF-PAGE phrase is specified, so b)
      *>         transfers to it and c) does not apply -- no WHEN line.
      *>   L3 -> 4 > 3, GR26 a): EC-I-O-EOP-OVERFLOW, outside any
      *>         PERFORM, and the only declarative is for EC-I-O-EOP --
      *>         so nothing runs (e)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB854WHN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb854w.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF LINAGE IS 3 LINES WITH FOOTING AT 2.
       01 PREC PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-EOP.
       D1P.
           DISPLAY "DECL-EOP".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT PRTF
           PERFORM
               WRITE PREC FROM "L1"
           WHEN EC-I-O-EOP
               DISPLAY "WHEN-EOP " FUNCTION EXCEPTION-STATUS
           END-PERFORM
           PERFORM
               WRITE PREC FROM "L2" AT END-OF-PAGE DISPLAY "PHRASE"
               END-WRITE
           WHEN EC-I-O-EOP
               DISPLAY "WHEN-EOP-2"
           END-PERFORM
           WRITE PREC FROM "L3"
           CLOSE PRTF
           DISPLAY "END"
           STOP RUN.
