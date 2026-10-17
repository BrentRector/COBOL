      *>--- conformance-case
      *> id: pb578_advancing_page_eop_overflow
      *> rules: [GR-8.4.3.14.4-2, GR-13.18.34.4-7, GR-14.9.51.4-27]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-I-O-EOP-OVERFLOW CHECKING ON
      *> kb/Work PB578 -- the implicit-increment sentence of ISO
      *> §13.18.34.4 GR7 c) 1, made observable.
      *>   python scripts/spec/cite.py --check 13.18.34.4 "During the
      *>   resetting of LINAGE-COUNTER to the value one, the value of
      *>   LINAGE-COUNTER is implicitly incremented to exceed the value
      *>   specified by integer-1 or the data item referenced by
      *>   data-name-1."
      *>                       -> OK  §13.18.34.4 7) c) 1.
      *> A counter past the page size is §14.9.51.4 GR26 a)'s trigger:
      *>   python scripts/spec/cite.py --check 14.9.51.4 "This occurs
      *>   when the associated LINAGE-COUNTER is equal to or exceeds the
      *>   page size."           -> OK  §14.9.51.4 26)
      *> and GR27 names what that end-of-page condition sets to exist:
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   end-of-page condition was caused by the action in General
      *>   rule 26a, the EC-I-O-EOP-OVERFLOW exception condition is set
      *>   to exist."            -> OK  §14.9.51.4 27)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the
      *>   END-OF-PAGE phrase is not specified, and there is an
      *>   applicable USE declarative, control is transferred to that
      *>   declarative."         -> OK  §14.9.51.4 27)
      *> So every WRITE ADVANCING PAGE on a logical page enters the
      *> EC-I-O-EOP-OVERFLOW declarative, AFTER the successful write
      *> ("the WRITE statement is successful and then the following
      *> actions take place", GR27), when the counter already reads one
      *> on the next page. docs/CONFORMANCE.md §4 "DETERMINATION --
      *> WRITE ADVANCING PAGE on a logical page is a page overflow".
      *>
      *> LPA, page size 5, no FOOTING phrase:
      *> A-W1  a plain WRITE: counter 2, no end-of-page, no declarative.
      *> A-W2  AFTER ADVANCING PAGE: DECL-OVF EC-I-O-EOP-OVERFLOW 001.
      *> A-W3  BEFORE ADVANCING PAGE: the same.
      *> A-W4  AFTER ADVANCING 1 LINE: counter 2, no declarative.
      *>
      *> LPB -- THE MID-STATEMENT READ the note asked about. The page
      *> size operand is re-read during WRITE ADVANCING PAGE (GR6 b) 2)
      *> and 0 violates GR6 b)'s first value rule:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "it continues
      *>   with the statement following the WRITE statement; the
      *>   LINAGE-COUNTER is set to 0 and remains at that value until
      *>   the file is closed"   -> OK  §13.18.34.4 6) b) 2.
      *> The USE procedure runs after the standard I-O exception
      *> routine, so after the WRITE's own I-O operation:
      *>   python scripts/spec/cite.py --check 14.9.49.4 "the procedures
      *>   associated with a USE statement are executed after completion
      *>   of the standard input-output exception routine"
      *>                       -> OK  §14.9.49.4 6)
      *> and the counter it reads is GR6 b) 2's 0, never the transient
      *> value GR7 c) 1 passes through (the pb526_linage_ec_condition
      *> twin of the overflow arm reads the same 0). The violated WRITE
      *> is unsuccessful, so no end-of-page condition and no DECL-OVF:
      *> B-W1  a plain WRITE: counter 2.
      *> B-W2  USE-B FS=90 LC=000, then B-W2 FS=90 LC=000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB578EOV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPA ASSIGN TO "pb578eova.prt".
           SELECT LPB ASSIGN TO "pb578eovb.prt"
               FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD LPA LINAGE IS 5 LINES.
       01 A-REC PIC X(4).
       FD LPB LINAGE IS WS-SZ LINES.
       01 B-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-SZ PIC 9(3) VALUE 3.
       01 FS-B  PIC XX   VALUE "??".
       01 WS-EC PIC X(19).
       01 LA    PIC 9(3).
       01 LB    PIC 9(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-OVF SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-EOP-OVERFLOW.
       D-OVF-P.
           MOVE FUNCTION EXCEPTION-STATUS TO WS-EC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "DECL-OVF " WS-EC " " LA.
       D-B SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LPB.
       D-B-P.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "USE-B FS=" FS-B " LC=" LB.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-P.
           OPEN OUTPUT LPA.
           MOVE "A1" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W1 " LA.
           MOVE "A2" TO A-REC.
           WRITE A-REC AFTER ADVANCING PAGE.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W2 " LA.
           MOVE "A3" TO A-REC.
           WRITE A-REC BEFORE ADVANCING PAGE.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W3 " LA.
           MOVE "A4" TO A-REC.
           WRITE A-REC AFTER ADVANCING 1 LINE.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W4 " LA.
           CLOSE LPA.
           OPEN OUTPUT LPB.
           MOVE "B1" TO B-REC.
           WRITE B-REC.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-W1 FS=" FS-B " LC=" LB.
           MOVE 0 TO WS-SZ.
           MOVE "B2" TO B-REC.
           WRITE B-REC AFTER ADVANCING PAGE.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-W2 FS=" FS-B " LC=" LB.
           CLOSE LPB.
           STOP RUN.
