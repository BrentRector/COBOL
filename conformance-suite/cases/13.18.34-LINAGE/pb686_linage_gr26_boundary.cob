      *>--- conformance-case
      *> id: pb686_linage_gr26_boundary
      *> rules: [GR-13.18.34.4-2, GR-13.18.34.4-3, GR-14.9.51.4-26,
      *>    GR-14.9.51.4-28]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.51.4 GR26 a)/b) AT THE BOUNDARY — a WRITE that lands
      *> the LINAGE-COUNTER exactly ON the page size.
      *>
      *> ⚖ THIS FIXTURE PINS AN ADJUDICATED DETERMINATION, NOT A
      *> LITERAL RULE. docs/CONFORMANCE.md §4, "DETERMINATION — the
      *> §14.9.51.4 GR26 a)/b) boundary at LINAGE-COUNTER = page size"
      *> (kb/Work PB686). GR26's two arms, as PRINTED, cannot both be
      *> honoured at that one counter value; this fixture pins the
      *> reading the determination adopts, and the two lines marked
      *> ⚖ below are the ones that distinguish it from the rejected
      *> literal reading. Do not re-baseline them from an oracle.
      *>
      *> THE CONTRADICTION, IN THE STANDARD'S OWN WORDS.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "an end-of-page
      *>   condition occurs when the lines written by a WRITE statement
      *>   do not fit within the current page body"
      *>   -> OK  §14.9.51.4 26)  (General rules)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "This occurs
      *>   when the associated LINAGE-COUNTER is equal to or exceeds
      *>   the page size."     -> OK  §14.9.51.4 26)  (General rules)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "equal to or
      *>   exceeds the current value of the footing start and is less
      *>   than the page size" -> OK  §14.9.51.4 26)  (General rules)
      *> Arm a) fires at counter >= page size; arm b) is clamped to
      *> counter < page size. Read literally, a write landing ON the
      *> page size is arm a): page overflow, the record pushed to the
      *> first line of the NEXT logical page, counter reset to one.
      *>
      *> WHY THAT LITERAL READING IS REJECTED. It contradicts three
      *> other rules, each of which this fixture also exercises.
      *>   python scripts/spec/cite.py --check 13.18.34.4 "specifies
      *>   the number of lines that may be written or spaced on the
      *>   logical page"       -> OK  §13.18.34.4 2)  (General rules)
      *> GR2 makes all `page size` lines writable. Under the literal
      *> reading the line NUMBERED page size can never receive a
      *> record from an AFTER-advancing write — it is pushed to the
      *> next page — so a 4-line body would hold at most 3 written
      *> lines, on every page, forever.
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The footing
      *>   area is the area of the page body between the footing start
      *>   and the page size, inclusive."
      *>                       -> OK  §13.18.34.4 3)  (General rules)
      *> GR3 puts the page-size line INSIDE the footing area. Arm b)'s
      *> "less than the page size" clamp excludes exactly that line, so
      *> b) could never fire on the last line GR3 gives it.
      *>   python scripts/spec/cite.py --check 13.18.34.4 "the value of
      *>   LINAGE-COUNTER is implicitly incremented to exceed the value
      *>   specified by integer-1"
      *>                       -> OK  §13.18.34.4 7) a)  (General rules)
      *> And GR7's ADVANCING PAGE rule is the standard's own way of
      *> making arm a) fire: it says the counter is implicitly
      *> incremented to EXCEED integer-1 — not to equal it. Equality
      *> would have sufficed had equality been the trigger.
      *>
      *> THE DETERMINATION. Both arms are evaluated on the counter the
      *> write's advancing operation produces, and the boundary is
      *> STRICT: counter > page size is arm a) (the lines do not fit —
      *> GR26's own lead sentence); footing start <= counter <=
      *> page size is arm b) (GR3's inclusive footing area). The arms
      *> stay a partition, and GR1/GR2/GR3/GR7 all come out true.
      *>
      *> WHAT THIS FIXTURE ASSERTS, LINE BY LINE. Two files, one page
      *> size (4), differing only in the FOOTING phrase; four writes
      *> each, every one AFTER ADVANCING 1 LINE so the counter is a
      *> pure function of GR7 and no plain-WRITE equivalence is in
      *> play.
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The value of
      *>   LINAGE-COUNTER is automatically set to one at the time an
      *>   OPEN statement with the OUTPUT phrase is executed for the
      *>   associated file."   -> OK  §13.18.34.4 7) a)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "the
      *>   LINAGE-COUNTER is incremented by the value of the integer
      *>   specified in the ADVANCING phrase"
      *>                       -> OK  §13.18.34.4 7) a)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The value of
      *>   LINAGE-COUNTER is automatically reset to one when the device
      *>   is repositioned to the first line that may be written on for
      *>   each of the succeeding logical pages."
      *>                       -> OK  §13.18.34.4 7) a)
      *> GR7d gives 1 at OPEN OUTPUT; each write adds 1 (GR7 c) 2);
      *> the overflow write resets to 1 (GR7 c) 4).
      *>
      *> FTG (FOOTING AT 3):
      *>   F1 counter 2 — below the footing start, inside the body:
      *>      neither arm. NOT AT END-OF-PAGE runs.
      *>   F2 counter 3 — arm b), the footing area's first line.
      *>   F3 counter 4 — ⚖ THE BOUNDARY. Arm b) under the
      *>      determination (4 is in [3,4]); the counter is NOT reset,
      *>      so LC reads 004. The literal reading would print
      *>      "F3 EOP LC=001".
      *>   F4 counter would be 5 > 4 — arm a). Reposition to line one
      *>      of the next page, LC reads 001.
      *> NFT (no FOOTING phrase):
      *>   python scripts/spec/cite.py --check 13.18.34.4 "If the
      *>   FOOTING phrase is not specified, no end-of-page condition
      *>   independent of the page overflow condition exists."
      *>                       -> OK  §13.18.34.4 1)  (General rules)
      *>   N1, N2 counters 2, 3 — no independent end-of-page exists
      *>      and the lines fit: NOT AT END-OF-PAGE runs.
      *>   N3 counter 4 — ⚖ THE BOUNDARY WITHOUT A FOOTING PHRASE.
      *>      GR1 leaves no independent condition and GR2 says the
      *>      line may be written, so NOTHING is raised. The literal
      *>      reading would print "N3 EOP LC=001", i.e. it would make
      *>      the last line of every page body unwritable even for a
      *>      file that never mentions FOOTING. This is the arm of the
      *>      dispatch a FOOTING-only fixture cannot see.
      *>   N4 counter would be 5 > 4 — arm a), LC reads 001.
      *>
      *> PLN (FOOTING AT 3, driven by PLAIN WRITE statements) — THE
      *> SECOND ENTRY POINT THAT CAN LAND ON THE BOUNDARY. A WRITE
      *> with no ADVANCING phrase reaches the counter by a different
      *> rule:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "When the
      *>   ADVANCING phrase of the WRITE statement is not specified,
      *>   the LINAGE-COUNTER is incremented by the value one."
      *>                       -> OK  §13.18.34.4 7) a)
      *> so P1..P4 must step 2, 3, 4, 1 and raise exactly what F1..F4
      *> raise. The RECORD sits on a different physical line than it
      *> does on FTG — §14.9.51.4 GR25 makes an omitted ADVANCING
      *> phrase act as AFTER ADVANCING 1 LINE, and the connector
      *> reaches that through its print path — but the end-of-page
      *> condition is a function of the COUNTER, so the displayed
      *> answers are identical. Asserting that identity is the point:
      *> this repo's most reproducible defect is a dispatch with two
      *> arms and only one of them fixed, and the boundary comparison
      *> is reached from more than one WRITE entry point.
      *>
      *> observes a completed write and the counter it left behind:
      *>   python scripts/spec/cite.py --check 14.9.51.4 "When an
      *>   end-of-page condition occurs, the WRITE statement is
      *>   successful"        -> OK  §14.9.51.4 27)  (General rules)
      *>
      *> ⛔ WHY EACH WRITE HAS ITS OWN MOVE:
      *>   python scripts/spec/cite.py --check 14.9.51.4 "The logical
      *>   record released by the successful execution of the WRITE
      *>   statement is no longer available in the record area"
      *>                       -> OK  §14.9.51.4 4)  (General rules)
      *> Neither file is in a SAME RECORD AREA clause, so a second
      *> WRITE of the same record item without an intervening MOVE
      *> would release an area the standard has declared unavailable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB686EOPB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FTG ASSIGN TO "pb686b-f.prt".
           SELECT NFT ASSIGN TO "pb686b-n.prt".
           SELECT PLN ASSIGN TO "pb686b-p.prt".
       DATA DIVISION.
       FILE SECTION.
       FD FTG LINAGE IS 4 LINES WITH FOOTING AT 3.
       01 F-REC PIC X(4).
       FD NFT LINAGE IS 4 LINES.
       01 N-REC PIC X(4).
       FD PLN LINAGE IS 4 LINES WITH FOOTING AT 3.
       01 P-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 LF PIC 9(3).
       01 LN PIC 9(3).
       01 LP PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT FTG.
           OPEN OUTPUT NFT.
           OPEN OUTPUT PLN.
           MOVE LINAGE-COUNTER OF FTG TO LF.
           MOVE LINAGE-COUNTER OF NFT TO LN.
           DISPLAY "OPEN F=" LF " N=" LN.
           MOVE "AAAA" TO F-REC.
           WRITE F-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "F1 EOP"
               NOT AT END-OF-PAGE DISPLAY "F1 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF FTG TO LF.
           DISPLAY "F1 LC=" LF.
           MOVE "BBBB" TO F-REC.
           WRITE F-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "F2 EOP"
               NOT AT END-OF-PAGE DISPLAY "F2 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF FTG TO LF.
           DISPLAY "F2 LC=" LF.
           MOVE "CCCC" TO F-REC.
           WRITE F-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "F3 EOP"
               NOT AT END-OF-PAGE DISPLAY "F3 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF FTG TO LF.
           DISPLAY "F3 LC=" LF.
           MOVE "DDDD" TO F-REC.
           WRITE F-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "F4 EOP"
               NOT AT END-OF-PAGE DISPLAY "F4 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF FTG TO LF.
           DISPLAY "F4 LC=" LF.
           MOVE "EEEE" TO N-REC.
           WRITE N-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "N1 EOP"
               NOT AT END-OF-PAGE DISPLAY "N1 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF NFT TO LN.
           DISPLAY "N1 LC=" LN.
           MOVE "FFFF" TO N-REC.
           WRITE N-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "N2 EOP"
               NOT AT END-OF-PAGE DISPLAY "N2 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF NFT TO LN.
           DISPLAY "N2 LC=" LN.
           MOVE "GGGG" TO N-REC.
           WRITE N-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "N3 EOP"
               NOT AT END-OF-PAGE DISPLAY "N3 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF NFT TO LN.
           DISPLAY "N3 LC=" LN.
           MOVE "HHHH" TO N-REC.
           WRITE N-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "N4 EOP"
               NOT AT END-OF-PAGE DISPLAY "N4 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF NFT TO LN.
           DISPLAY "N4 LC=" LN.
           MOVE "IIII" TO P-REC.
           WRITE P-REC
               AT END-OF-PAGE DISPLAY "P1 EOP"
               NOT AT END-OF-PAGE DISPLAY "P1 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF PLN TO LP.
           DISPLAY "P1 LC=" LP.
           MOVE "JJJJ" TO P-REC.
           WRITE P-REC
               AT END-OF-PAGE DISPLAY "P2 EOP"
               NOT AT END-OF-PAGE DISPLAY "P2 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF PLN TO LP.
           DISPLAY "P2 LC=" LP.
           MOVE "KKKK" TO P-REC.
           WRITE P-REC
               AT END-OF-PAGE DISPLAY "P3 EOP"
               NOT AT END-OF-PAGE DISPLAY "P3 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF PLN TO LP.
           DISPLAY "P3 LC=" LP.
           MOVE "LLLL" TO P-REC.
           WRITE P-REC
               AT END-OF-PAGE DISPLAY "P4 EOP"
               NOT AT END-OF-PAGE DISPLAY "P4 NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF PLN TO LP.
           DISPLAY "P4 LC=" LP.
           CLOSE FTG.
           CLOSE NFT.
           CLOSE PLN.
           STOP RUN.
