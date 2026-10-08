      *> ⛔ THE LINAGE TOP AND BOTTOM MARGINS ARE LINES OF THE LOGICAL
      *> PAGE, ON THE MEDIUM — the byte-asserting fixture kb/Work PB523
      *> owes. Every other LINAGE fixture in this corpus observes only
      *> LINAGE-COUNTER and the END-OF-PAGE branches, and l1_linage_zero_
      *> margins says so in its own words ("Telling a nonzero margin from
      *> a zero one needs a rule that makes the margin observable; that is
      *> not SR4's assertion"). This one makes it observable: each print
      *> file is written, closed, and READ BACK AS LINE SEQUENTIAL, so the
      *> DISPLAYed index of each line IS its physical line number.
      *>
      *> THE RULES.
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The logical
      *>   page size is the sum of the values referenced by each phrase
      *>   except the FOOTING phrase"
      *>   -> OK  §13.18.34.4 1)  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "Integer-3 or
      *>   the value of the data item referenced by data-name-3 specifies
      *>   the number of lines in the top margin on the logical page"
      *>   -> OK  §13.18.34.4 4)  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "Integer-4 or
      *>   the value of the data item referenced by data-name-4 specifies
      *>   the number of lines in the bottom margin on the logical page"
      *>   -> OK  §13.18.34.4 5)  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "Each logical
      *>   page is contiguous to the next with no additional spacing
      *>   provided."   -> OK  §13.18.34.4 8)  (General rules)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the device is
      *>   repositioned to the first line that may be written on the next
      *>   logical page"   -> OK  §14.9.51.4 26)  (General rules)
      *>
      *> THE LOGICAL PAGE BOTH FILES DECLARE. LINAGE IS 2 LINES LINES AT
      *> TOP 3 LINES AT BOTTOM 2. By GR1 the logical page size is the sum
      *> of every phrase but FOOTING: 3 + 2 + 2 = 7 lines. By GR8 the
      *> pages are laid contiguously, so page 1 is physical lines 1-7 and
      *> page 2 begins at physical line 8. Within a page: GR4 gives the
      *> top margin the first 3 lines, GR2 gives the page body the next 2
      *> ("the number of lines that may be written or spaced"), GR5 gives
      *> the bottom margin the last 2. So page-1 body lines 1 and 2 are
      *> physical lines 4 and 5; page-2 body lines 1 and 2 are physical
      *> lines 11 and 12. GR7 d) puts the device on body line 1 at OPEN
      *> OUTPUT (LINAGE-COUNTER = 1).
      *>
      *> FILE A — WRITE ... AFTER ADVANCING 1 LINE, three times.
      *> §14.9.51.4 GR25 f) presents the line AFTER the advance and GR7
      *> c) 2 adds the advance to the counter, so:
      *>   W1: counter 1 -> 2, AAAA on page-1 body line 2  = A05.
      *>   W2: the counter would reach 3, past the page size, which is
      *>       §14.9.51.4 GR26 a)'s page overflow; with the AFTER phrase
      *>       "the device is repositioned to the first line that may be
      *>       written on the next logical page and the logical record is
      *>       presented on that line" — page-2 body line 1 = A11 — and
      *>       GR7 c) 4 resets the counter to one.
      *>   W3: counter 1 -> 2, CCCC on page-2 body line 2 = A12.
      *> FIVE blank lines therefore separate AAAA from BBBB (A06-A10):
      *> page 1's bottom margin (2, GR5) plus page 2's top margin (3,
      *> GR4). A compiler that keeps the margins in the counter but not
      *> on the medium writes ONE line there and fails at A06.
      *>
      *> FILE B — the LINE SEQUENTIAL arm, plain WRITE, three times. The
      *> page geometry is the same, and so is the file: a WRITE without
      *> the ADVANCING phrase acts "as if the user has specified AFTER
      *> ADVANCING 1 LINE" (§14.9.51.4 GR25; kb/Work PB1027)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If the ADVANCING
      *>   phrase is not used, automatic advancing shall be provided by
      *>   the implementor to act as if the user has specified AFTER
      *>   ADVANCING 1 LINE"   -> OK  §14.9.51.4 25)
      *> so file B is file A byte for byte: AAAA on page-1 body line 2 =
      *> B05, BBBB on page-2 body line 1 = B11 (GR26 a) overflow), CCCC on
      *> page-2 body line 2 = B12. (Before PB1027 a plain WRITE presented
      *> where the device stood and advanced after, which put the three
      *> records one line early: B04, B05, B11.) This file exists because
      *> the two WRITE arms are a two-arm dispatch and only the
      *> record-sequential one had been fixed: a line sequential LINAGE
      *> file emitted its record and a bare newline, so no margin ever
      *> reached its medium.
      *>
      *> ⛔ WHY EACH WRITE IS PRECEDED BY ITS OWN MOVE. §14.9.51.4 GR4 —
      *>   python scripts/spec/cite.py --check 14.9.51.4 "The logical
      *>   record released by the successful execution of the WRITE
      *>   statement is no longer available in the record area unless the
      *>   file-name associated with record-name-1 is specified in a SAME
      *>   RECORD AREA clause."   -> OK  §14.9.51.4 4)  (General rules)
      *> Neither file is named in a SAME RECORD AREA clause, so the record
      *> area is unavailable after each WRITE and a second WRITE of the
      *> same item would release bytes the standard does not define.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB523M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTA ASSIGN TO "pb523m-a.prt".
           SELECT PRTB ASSIGN TO "pb523m-b.prt"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT RDA ASSIGN TO "pb523m-a.prt"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT RDB ASSIGN TO "pb523m-b.prt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD PRTA LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2.
       01 A-REC PIC X(4).
       FD PRTB LINAGE IS 2 LINES LINES AT TOP 3 LINES AT BOTTOM 2.
       01 B-REC PIC X(4).
       FD RDA.
       01 RA-REC PIC X(4).
       FD RDB.
       01 RB-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 IDX PIC 99 VALUE 0.
       01 EOF-SW PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRTA.
           MOVE "AAAA" TO A-REC.
           WRITE A-REC AFTER ADVANCING 1 LINE.
           MOVE "BBBB" TO A-REC.
           WRITE A-REC AFTER ADVANCING 1 LINE.
           MOVE "CCCC" TO A-REC.
           WRITE A-REC AFTER ADVANCING 1 LINE.
           CLOSE PRTA.
           OPEN OUTPUT PRTB.
           MOVE "AAAA" TO B-REC.
           WRITE B-REC.
           MOVE "BBBB" TO B-REC.
           WRITE B-REC.
           MOVE "CCCC" TO B-REC.
           WRITE B-REC.
           CLOSE PRTB.
           MOVE 0 TO IDX.
           MOVE 0 TO EOF-SW.
           OPEN INPUT RDA.
           PERFORM UNTIL EOF-SW = 1
               READ RDA
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       ADD 1 TO IDX
                       DISPLAY "A" IDX "=[" RA-REC "]"
               END-READ
           END-PERFORM.
           CLOSE RDA.
           MOVE 0 TO IDX.
           MOVE 0 TO EOF-SW.
           OPEN INPUT RDB.
           PERFORM UNTIL EOF-SW = 1
               READ RDB
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       ADD 1 TO IDX
                       DISPLAY "B" IDX "=[" RB-REC "]"
               END-READ
           END-PERFORM.
           CLOSE RDB.
           STOP RUN.
