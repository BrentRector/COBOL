      *>--- conformance-case
      *> id: pb524_linage_operand_shapes
      *> rules: [SR-13.18.34.3-2, SR-13.18.34.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB524 - ISO 13.18.34.3, the LINAGE clause syntax rules,
      *> witnessed from the LEGAL side: every operand below satisfies them,
      *> so the program shall compile, and what it prints is fixed by the
      *> general rules.
      *>   python scripts/spec/cite.py --check 13.18.34.3 "Data-name-1,
      *>   data-name-2, data-name-3, and data-name-4 shall reference
      *>   elementary unsigned numeric integer data items"
      *>   -> OK  13.18.34.3 2)  (Syntax rules)
      *>   python scripts/spec/cite.py --check 13.18.34.3 "Integer-2
      *>   shall not be greater than integer-1."
      *>   -> OK  13.18.34.3 3)  (Syntax rules)
      *> SR2 is met by a USAGE DISPLAY PIC 9(3) (PAGE-A's page size) and
      *> by USAGE COMP PIC 99 items (PAGE-B's margins): "numeric integer"
      *> is a class-and-scale rule, not a usage rule. SR3 is met at its
      *> boundary by PAGE-B - FOOTING AT 4 on a page of 4 - because the
      *> rule forbids only GREATER. Neither SR says anything about what a
      *> signed, fractional, alphanumeric or group operand would do,
      *> because each of those is refused at compile time (the negatives
      *> pb524-linage-operand-not-unsigned-integer and
      *> pb524-linage-footing-beyond-page).
      *>
      *> THE EXPECTED LINES. 13.18.34.4 GR7: LINAGE-COUNTER is set to one
      *> by OPEN OUTPUT and "When the ADVANCING phrase of the WRITE
      *> statement is not specified, the LINAGE-COUNTER is incremented by
      *> the value one" (cite.py --check 13.18.34.4 -> OK 7)). So each
      *> WRITE below leaves the counter at 2, then 3.
      *> 14.9.51.4 GR26 b): with a FOOTING phrase an end-of-page condition
      *> occurs "when the associated LINAGE-COUNTER is equal to or exceeds
      *> the current value of the footing start and is less than the page
      *> size" (cite.py --check 14.9.51.4 -> OK 26)).
      *>   PAGE-A: page 7 (PG-SIZE), footing 3. Counter 2 < 3 -> NO-EOP;
      *>           counter 3 >= 3 and < 7 -> EOP.
      *>   PAGE-B: page 4, footing 4. Counters 2 and 3 are both below the
      *>           footing start -> NO-EOP, NO-EOP.
      *> The counters are MOVEd to a work item before DISPLAY so the
      *> printed image is the PIC 9(3) edit of the value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB524POS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PAGE-A ASSIGN TO "pb524pa.prt".
           SELECT PAGE-B ASSIGN TO "pb524pb.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PAGE-A LINAGE IS PG-SIZE LINES WITH FOOTING AT 3.
       01 A-REC PIC X(8).
       FD PAGE-B LINAGE IS 4 LINES WITH FOOTING AT 4
                 LINES AT TOP MARGIN-T LINES AT BOTTOM MARGIN-B.
       01 B-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 PG-SIZE  PIC 9(3) VALUE 7.
       01 MARGIN-T PIC 99 COMP VALUE 1.
       01 MARGIN-B PIC 99 COMP VALUE 2.
       01 W-LC     PIC 9(3).
       01 W-EOP    PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PAGE-A PAGE-B.
           MOVE "A1" TO A-REC.
           WRITE A-REC
               AT END-OF-PAGE MOVE "EOP" TO W-EOP
               NOT AT END-OF-PAGE MOVE "NO-EOP" TO W-EOP
           END-WRITE.
           MOVE LINAGE-COUNTER OF PAGE-A TO W-LC.
           DISPLAY "A " W-EOP " " W-LC.
           MOVE "A2" TO A-REC.
           WRITE A-REC
               AT END-OF-PAGE MOVE "EOP" TO W-EOP
               NOT AT END-OF-PAGE MOVE "NO-EOP" TO W-EOP
           END-WRITE.
           MOVE LINAGE-COUNTER OF PAGE-A TO W-LC.
           DISPLAY "A " W-EOP " " W-LC.
           MOVE "B1" TO B-REC.
           WRITE B-REC
               AT END-OF-PAGE MOVE "EOP" TO W-EOP
               NOT AT END-OF-PAGE MOVE "NO-EOP" TO W-EOP
           END-WRITE.
           MOVE LINAGE-COUNTER OF PAGE-B TO W-LC.
           DISPLAY "B " W-EOP " " W-LC.
           MOVE "B2" TO B-REC.
           WRITE B-REC
               AT END-OF-PAGE MOVE "EOP" TO W-EOP
               NOT AT END-OF-PAGE MOVE "NO-EOP" TO W-EOP
           END-WRITE.
           MOVE LINAGE-COUNTER OF PAGE-B TO W-LC.
           DISPLAY "B " W-EOP " " W-LC.
           CLOSE PAGE-A PAGE-B.
           STOP RUN.
