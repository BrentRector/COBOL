*>--- conformance-case
*> id: write_before_and_after
*> rules: [SR-14.9.51.3-17]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.51.2 Format 1 — WRITE … BEFORE AFTER ADVANCING … (COBOL-2023, Annex §E.3.3 item 2).
      *> ⛔ THE PRINTED FORMAT WAS MEASURED OFF THE PDF (page 815, printed folio 785): the choice indicators
      *> — the | bars, enclosed by braces — enclose ONLY the two WORDS BEFORE and AFTER; ADVANCING and the
      *> whole operand group sit OUTSIDE them, ONCE. §5.2.6.4: "one or more of the alternatives contained
      *> within the choice indicators shall be specified, but any single alternative shall be specified only
      *> once. … The alternatives may be specified in any order." So the spellings are BEFORE …, AFTER …,
      *> BEFORE AFTER … and AFTER BEFORE … — never two ADVANCING operands (kb/Work PB712; the two-operand
      *> spelling this golden used to carry is now the negative fixture pb712-write-two-advancing-operands).
      *>
      *> EXPECTED VALUES, DERIVED BEFORE THEY WERE MEASURED:
      *>  • §14.9.51.4 GR25 a) advances the page "the number of lines equal to that value" — ONE advance, of
      *>    the ONE printed operand, however many of the two words precede it. §13.18.34 GR7 c) then increments
      *>    LINAGE-COUNTER by that amount and GR7 d) sets it to 1 at OPEN OUTPUT. So on a 20-line body:
      *>    OPEN 001; BEFORE AFTER ADVANCING 3 -> 004; AFTER BEFORE ADVANCING 2 -> 006; and the CONTROL
      *>    write, a lone BEFORE ADVANCING 3, -> 009. A two-advance reading would read 007 / 011 / 014.
      *>  • §14.9.51.4 GR25 e)/f) place that one advance: e) presents the line BEFORE the page is advanced;
      *>    f) second sentence — "If the AFTER phrase is used and the BEFORE phrase is also used, the printed
      *>    page is advanced … after the line was presented as specified in General rule 25e" — puts the PAIR
      *>    on e)'s side. So the pair presents FIRST, exactly as a lone BEFORE does, and a lone AFTER does not.
      *>    Read back off the medium: the record is on line 001 for the pair, on line 003 for AFTER ADVANCING 2.
      *>  • §14.9.51.4 GR26 a) + §13.18.34 GR7 c) 4: on a 4-line body from counter 1, the pair's advance of 4
      *>    leaves the counter past the page body, so the end-of-page condition occurs (GR27 b) transfers to
      *>    the AT END-OF-PAGE imperative) and the device is repositioned — EOP, then LINAGE-COUNTER 001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB712WBA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "wba-conf.prt".
           SELECT PAIRF ASSIGN TO "wba-pair.prt".
           SELECT AFTF ASSIGN TO "wba-aft.prt".
           SELECT BACKF ASSIGN TO "wba-pair.prt"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT BACK2F ASSIGN TO "wba-aft.prt"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT EOPF ASSIGN TO "wba-eop.prt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF LINAGE IS 20 LINES.
       01 P-REC PIC X(6).
       FD PAIRF.
       01 Q-REC PIC X(6).
       FD AFTF.
       01 A-REC PIC X(6).
       FD BACKF.
       01 R-REC PIC X(6).
       FD BACK2F.
       01 S-REC PIC X(6).
       FD EOPF LINAGE IS 4 LINES.
       01 E-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 LC PIC 9(3).
       01 ORD PIC 9(3).
       01 HIT PIC 9(3).
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
      *>   ── ONE advance for the pair, and the two word orders (§5.2.6.4 "in any order") ──
           OPEN OUTPUT PRTF.
           MOVE LINAGE-COUNTER OF PRTF TO LC.
           DISPLAY "OPEN LC=" LC.
           MOVE "AAA" TO P-REC.
           WRITE P-REC BEFORE AFTER ADVANCING 3 LINES.
           MOVE LINAGE-COUNTER OF PRTF TO LC.
           DISPLAY "PAIR LC=" LC.
           MOVE "BBB" TO P-REC.
           WRITE P-REC AFTER BEFORE ADVANCING 2 LINES.
           MOVE LINAGE-COUNTER OF PRTF TO LC.
           DISPLAY "REVERSED LC=" LC.
           MOVE "CCC" TO P-REC.
           WRITE P-REC BEFORE ADVANCING 3 LINES.
           MOVE LINAGE-COUNTER OF PRTF TO LC.
           DISPLAY "BEFORE-ONLY LC=" LC.
           CLOSE PRTF.
      *>   ── GR25 e)/f): the PAIR presents first, a lone AFTER does not ──
           OPEN OUTPUT PAIRF.
           MOVE "DDD" TO Q-REC.
           WRITE Q-REC BEFORE AFTER ADVANCING 2 LINES.
           CLOSE PAIRF.
           OPEN OUTPUT AFTF.
           MOVE "DDD" TO A-REC.
           WRITE A-REC AFTER ADVANCING 2 LINES.
           CLOSE AFTF.
           MOVE 0 TO ORD.
           MOVE 0 TO HIT.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT BACKF.
           PERFORM UNTIL HIT > 0 OR EOF-FLAG = "Y"
               READ BACKF
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       ADD 1 TO ORD
                       IF R-REC NOT = SPACES
                           MOVE ORD TO HIT
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE BACKF.
           DISPLAY "PAIR ON LINE=" HIT.
           MOVE 0 TO ORD.
           MOVE 0 TO HIT.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT BACK2F.
           PERFORM UNTIL HIT > 0 OR EOF-FLAG = "Y"
               READ BACK2F
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END
                       ADD 1 TO ORD
                       IF S-REC NOT = SPACES
                           MOVE ORD TO HIT
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE BACK2F.
           DISPLAY "AFTER ON LINE=" HIT.
      *>   ── GR26 a) / GR27 b): the pair's one advance can raise end-of-page ──
           OPEN OUTPUT EOPF.
           MOVE "EEE" TO E-REC.
           WRITE E-REC BEFORE AFTER ADVANCING 4 LINES
               AT END-OF-PAGE DISPLAY "EOP"
               NOT AT END-OF-PAGE DISPLAY "NO-EOP"
           END-WRITE.
           MOVE LINAGE-COUNTER OF EOPF TO LC.
           DISPLAY "EOP LC=" LC.
           CLOSE EOPF.
           STOP RUN.
