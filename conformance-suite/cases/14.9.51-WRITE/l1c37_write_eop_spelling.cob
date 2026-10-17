      *>--- conformance-case
      *> id: l1c37_write_eop_spelling
      *> rules: [SR-14.9.51.3-20]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.51.3 SR20 — EOP and END-OF-PAGE are equivalent
      *> Rule: "The words END-OF-PAGE and EOP are equivalent."
      *>   cite.py --check 14.9.51.3 "The words END-OF-PAGE and EOP
      *>   are equivalent" -> OK  §14.9.51.3 20)  (Syntax rules)
      *> Supporting rules (cite.py mislabels list items, PB1554; the
      *> numbers below were re-read from specs/ISO_COBOL.md):
      *>   cite.py --check 14.9.51.4 "If the END-OF-PAGE phrase is
      *>   specified, control is transferred to imperative-statement-1"
      *>   -> OK  §14.9.51.4 27) b)
      *>   cite.py --check 14.9.51.4 "control is transferred to
      *>   imperative-statement-2 of the NOT END-OF-PAGE phrase"
      *>   -> OK  §14.9.51.4 28)
      *>   cite.py --check 14.9.51.4 "This occurs when the associated
      *>   LINAGE-COUNTER is equal to or exceeds the current value of
      *>   the footing start and is less than the page size"
      *>   -> OK  §14.9.51.4 26) b)
      *>   cite.py --check 13.18.34.4 "The value of LINAGE-COUNTER is
      *>   automatically set to one at the time an OPEN statement with
      *>   the OUTPUT phrase is executed for the associated file"
      *>   -> OK  §13.18.34.4 7) d)  (cite.py prints "7) a)")
      *>   cite.py --check 13.18.34.4 "When the ADVANCING phrase of the
      *>   WRITE statement is not specified, the LINAGE-COUNTER is
      *>   incremented by the value one"
      *>   -> OK  §13.18.34.4 7) c) 3.  (cite.py prints "7) a)")
      *> DERIVATION. LINAGE 5 WITH FOOTING AT 3: OPEN OUTPUT sets the
      *> counter to 1; each WRITE (no ADVANCING) adds one. The counter
      *> never reaches the page size 5, so only GR26 b) can raise EOP
      *> (no page-size boundary question arises).
      *>   W1 -> 2: 2 < footing 3, no EOP; NOT phrase (spelled EOP)
      *>         runs (GR28)                         -> "W1 NOT 2"
      *>   W2 -> 3: 3 >= 3 and < 5, EOP; AT phrase (spelled EOP) runs
      *>         (GR27 b))                           -> "W2 AT 3"
      *>   W3 -> 4: EOP; AT phrase (spelled EOP, NOT spelled EOP)
      *>                                             -> "W3 AT 4"
      *> LINAGE-COUNTER's size is implementor-defined, so it is MOVEd
      *> to a PIC 9 item before display.
      *> Each statement mixes the two spellings, so an implementation
      *> that did not treat EOP as END-OF-PAGE rejects the program or
      *> routes a phrase to the wrong branch.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C37A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "L1C37A.PRT".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF LINAGE IS 5 LINES WITH FOOTING AT 3.
       01 PREC PIC X(8).
       WORKING-STORAGE SECTION.
       01 LC PIC 9.
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT PRTF
           MOVE "LINE1" TO PREC
           WRITE PREC
               AT END-OF-PAGE
                   MOVE LINAGE-COUNTER TO LC
                   DISPLAY "W1 AT " LC
               NOT AT EOP
                   MOVE LINAGE-COUNTER TO LC
                   DISPLAY "W1 NOT " LC
           END-WRITE
           MOVE "LINE2" TO PREC
           WRITE PREC
               AT EOP
                   MOVE LINAGE-COUNTER TO LC
                   DISPLAY "W2 AT " LC
               NOT AT END-OF-PAGE
                   MOVE LINAGE-COUNTER TO LC
                   DISPLAY "W2 NOT " LC
           END-WRITE
           MOVE "LINE3" TO PREC
           WRITE PREC
               AT EOP
                   MOVE LINAGE-COUNTER TO LC
                   DISPLAY "W3 AT " LC
               NOT EOP
                   MOVE LINAGE-COUNTER TO LC
                   DISPLAY "W3 NOT " LC
           END-WRITE
           CLOSE PRTF
           STOP RUN.
