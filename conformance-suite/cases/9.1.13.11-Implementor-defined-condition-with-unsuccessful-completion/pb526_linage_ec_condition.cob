*>--- conformance-case
*> id: pb526_linage_ec_condition
*> rules: [GR-9.1.13.11-1, GR-13.18.34.4-3, GR-13.18.34.4-6,
*>    DOC-A.1-110]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.34.4 GR6 b) — the LINAGE value rules AND what happens
      *> when they are violated. This fixture pins the SECOND HALF of that
      *> rule, which had no reachable behaviour at all before kb/Work
      *> PB526: the violation was a raw CLR throw that killed the process,
      *> so no declarative ran, no I-O status was set, LINAGE-COUNTER was
      *> never set to 0, and there were no "subsequent WRITE statements"
      *> left to keep raising.
      *>
      *> THE RULE, VERBATIM:
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The page size
      *>   shall be greater than zero."
      *>   -> OK  §13.18.34.4 6) b) 1.  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "The footing
      *>   start shall be greater than zero and not greater than the page
      *>   size."
      *>   -> OK  §13.18.34.4 6) b) 2.  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "If the value
      *>   does not conform to these two rules, the EC-I-O-LINAGE
      *>   exception condition is set to exist."
      *>   -> OK  §13.18.34.4 6) b) 2.  (General rules)
      *>   python scripts/spec/cite.py --check 13.18.34.4 "it continues
      *>   with the statement following the WRITE statement; the
      *>   LINAGE-COUNTER is set to 0 and remains at that value until the
      *>   file is closed; and all subsequent WRITE statements referencing
      *>   the file cause the EC-I-O-LINAGE exception condition to
      *>   continue to exist until the file is closed."
      *>   -> OK  §13.18.34.4 6) b) 2.  (General rules)
      *>
      *> WHAT MAKES IT OBSERVABLE WITH NO >>TURN. EC-I-O-LINAGE is Fatal
      *> (Table 13) and §14.6.13.1.3 3) routes a fatal EC-I-O condition
      *> through §9.1.13's rules, which are rules about the I-O STATUS:
      *>   python scripts/spec/cite.py --check 14.6.13.1.3 "If the
      *>   exception condition is a fatal EC-I-O exception condition, then
      *>   the rules for 9.1.13, I-O status apply, then if the implementor
      *>   has not specified otherwise, the following rules apply for all
      *>   fatal exception conditions."   -> OK  §14.6.13.1.3 3)
      *> No status value corresponds to this condition, so COBOL.NET makes
      *> it §9.1.13.11's implementor-defined one:
      *>   python scripts/spec/cite.py --check 9.1.13.11 "I-O status = 9x.
      *>   An implementor-defined condition exists. This condition shall
      *>   not duplicate any other condition specified by another I-O
      *>   status value. The value of x is defined by the implementor."
      *>   -> OK  §9.1.13.11 1)
      *> The value is '90' and the determination is docs/CONFORMANCE.md §7
      *> DOC-A.1-110. '9' is a FATAL first digit by this implementation's
      *> own determination, which is what Table 13's Fatal requires:
      *>   python scripts/spec/cite.py --check 9.1.13.1 "Certain classes
      *>   of I-O status values indicate fatal exception conditions. These
      *>   are: any that begin with the digit 3, 4, or 7, and any that
      *>   begin with the digit 9 that the implementor defines as fatal."
      *>   -> OK  §9.1.13.1  (General)
      *> An unsuccessful I-O status is what a format-1 USE declarative
      *> fires on, with no >>TURN anywhere:
      *>   python scripts/spec/cite.py --check 14.9.49.4 "the procedures
      *>   associated with a USE statement are executed after completion
      *>   of the standard input-output exception routine upon the
      *>   unsuccessful execution of an input-output operation unless an
      *>   AT END or INVALID KEY phrase takes precedence."
      *>   -> OK  §14.9.49.4 6)  (General rules)
      *> Checking for EC-I-O-LINAGE is NOT enabled here, so §14.6.13.1.3
      *> 8) governs the continuation and this implementation's answer is
      *> the one GR6 b) 2 itself writes down — execution continues with
      *> the statement following:
      *>   python scripts/spec/cite.py --check 14.6.13.1.3 "If checking
      *>   for the exception condition is not enabled, the implementor
      *>   defines whether or not execution will continue, how it will
      *>   continue, and how any receiving operands are affected."
      *>   -> OK  §14.6.13.1.3 8)  (Fatal exception conditions)
      *>
      *> ── LPA: the page size rule, violated at a WRITE ────────────────
      *> LPA is `LINAGE IS WS-SZ LINES`, WS-SZ = 2, no FOOTING phrase.
      *> A-OPEN  GR6 b) 1 reads 2 at the completion of the OPEN OUTPUT;
      *>         the values conform, GR7 d) sets the counter to one, so
      *>         FS=00 LC=001.
      *> A-W1    a plain WRITE adds one (GR7 c) 3): LC=002, still inside
      *>         the 2-line page body, FS=00.
      *> A-W2    WS-SZ has been moved to 0. The counter would pass the
      *>         page body, so this is a page overflow WRITE and GR6 b) 3
      *>         re-reads the operands during it — reading 0, which
      *>         violates value rule 1. The declarative for LPA fires
      *>         (USE-A), reading FS=90 and LC=000 MID-STATEMENT, and on
      *>         its return execution continues with the DISPLAY
      *>         FOLLOWING the WRITE. That DISPLAY is A-W2 and it reads
      *>         the same 90 / 000.
      *> A-W3    "all subsequent WRITE statements referencing the file
      *>         cause the EC-I-O-LINAGE exception condition to continue
      *>         to exist": the same status, the same declarative, the
      *>         counter still 0. No value was re-read — this WRITE
      *>         specifies neither ADVANCING PAGE nor a page overflow, so
      *>         GR6 b) never runs for it; the latch is what raises.
      *> A-CLOSE the CLOSE is a successful operation of its own (FS=00),
      *>         and "until the file is closed" ends here.
      *> A-REOPEN with WS-SZ = 3 the next OPEN OUTPUT re-determines the
      *>         values (GR6 b) 1) and the file works again: FS=00,
      *>         LC=001 by GR7 d). This is the half that proves the latch
      *>         is bounded by the CLOSE and not by the run unit.
      *> A-W4    LC=002, FS=00.
      *>
      *> ── LPB: the footing rule, violated at the OPEN ─────────────────
      *> LPB is `LINAGE IS 5 LINES WITH FOOTING AT WS-FT`, WS-FT = 0.
      *> The FOOTING PHRASE IS SPECIFIED, so GR6 b) 2's value rule applies
      *> to the value it evaluates to and 0 does not conform. GR1's
      *>   python scripts/spec/cite.py --check 13.18.34.4 "If the FOOTING
      *>   phrase is not specified, no end-of-page condition independent
      *>   of the page overflow condition exists."
      *>   -> OK  §13.18.34.4 1)  (General rules)
      *> is the rule for an ABSENT phrase and cannot be reached here
      *> (kb/Work PB525: zero used to be the encoding of "absent", so a
      *> specified 0 silently took GR1's reading and the program ran to
      *> completion printing NO-EOP).
      *> B-OPEN  the violation is detected at the completion of the OPEN
      *>         OUTPUT (GR6 b) 1), so USE-B fires there: FS=90, LC=000.
      *>         The connector STAYS OPEN — GR6 b) 2's "until the file is
      *>         closed" and "all subsequent WRITE statements referencing
      *>         the file" both presuppose it.
      *> B-W1    the latched WRITE. NEITHER end-of-page imperative runs:
      *>   python scripts/spec/cite.py --check 14.9.51.4 "When an
      *>   end-of-page condition occurs, the WRITE statement is successful
      *>   and then the following actions take place:"
      *>   -> OK  §14.9.51.4 27)  (General rules)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "If, during the
      *>   successful execution of a WRITE statement with the NOT
      *>   END-OF-PAGE phrase, the end-of-page condition does not occur,
      *>   then after execution of the input-output operation, control is
      *>   transferred to imperative-statement-2 of the NOT END-OF-PAGE
      *>   phrase."   -> OK  §14.9.51.4 28)  (General rules)
      *>         GR27 makes an end-of-page WRITE successful and GR28
      *>         conditions the NOT arm on successful execution, so an
      *>         unsuccessful WRITE runs neither.
      *> B-REOPEN with WS-FT = 3 the footing area is [3,5] (GR3) and the
      *>         file works: FS=00, LC=001.
      *> B-W2    AFTER ADVANCING 2 LINES puts the counter at 3, which is
      *>         at the footing start, so §14.9.51.4 GR26 b)'s end-of-page
      *>         condition occurs and the AT END-OF-PAGE imperative runs
      *>         on a SUCCESSFUL write: B-EOP2, FS=00, LC=003.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB526EC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPA ASSIGN TO "pb526ec-a.prt"
               FILE STATUS IS FS-A.
           SELECT LPB ASSIGN TO "pb526ec-b.prt"
               FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD LPA LINAGE IS WS-SZ LINES.
       01 A-REC PIC X(4).
       FD LPB LINAGE IS 5 LINES WITH FOOTING AT WS-FT.
       01 B-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-SZ PIC 9(3) VALUE 2.
       01 WS-FT PIC 99  VALUE 0.
       01 FS-A  PIC XX  VALUE "??".
       01 FS-B  PIC XX  VALUE "??".
       01 LA    PIC 9(3).
       01 LB    PIC 9(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-A-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LPA.
       ERR-A-PARA.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "USE-A FS=" FS-A " LC=" LA.
       ERR-B-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LPB.
       ERR-B-PARA.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "USE-B FS=" FS-B " LC=" LB.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-P.
           OPEN OUTPUT LPA.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-OPEN FS=" FS-A " LC=" LA.
           MOVE "AAAA" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W1 FS=" FS-A " LC=" LA.
           MOVE 0 TO WS-SZ.
           MOVE "BBBB" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W2 FS=" FS-A " LC=" LA.
           MOVE "CCCC" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W3 FS=" FS-A " LC=" LA.
           CLOSE LPA.
           DISPLAY "A-CLOSE FS=" FS-A.
           MOVE 3 TO WS-SZ.
           OPEN OUTPUT LPA.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-REOPEN FS=" FS-A " LC=" LA.
           MOVE "DDDD" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W4 FS=" FS-A " LC=" LA.
           CLOSE LPA.
           OPEN OUTPUT LPB.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-OPEN FS=" FS-B " LC=" LB.
           MOVE "EEEE" TO B-REC.
           WRITE B-REC AFTER ADVANCING 2 LINES
               AT END-OF-PAGE DISPLAY "B-EOP1"
               NOT AT END-OF-PAGE DISPLAY "B-NOEOP1"
           END-WRITE.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-W1 FS=" FS-B " LC=" LB.
           CLOSE LPB.
           DISPLAY "B-CLOSE FS=" FS-B.
           MOVE 3 TO WS-FT.
           OPEN OUTPUT LPB.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-REOPEN FS=" FS-B " LC=" LB.
           MOVE "FFFF" TO B-REC.
           WRITE B-REC AFTER ADVANCING 2 LINES
               AT END-OF-PAGE DISPLAY "B-EOP2"
               NOT AT END-OF-PAGE DISPLAY "B-NOEOP2"
           END-WRITE.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-W2 FS=" FS-B " LC=" LB.
           CLOSE LPB.
           STOP RUN.
