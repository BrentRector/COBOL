*>--- conformance-case
*> id: pb429-line-counter-receiving
*> rules: [SR-8.4.3.15.3-3, SR-14.9.25.3-4]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ LINE-COUNTER IS THE HALF OF §8.4.3.15 THE STANDARD TAKES AWAY (kb/Work PB429).
*> ISO/IEC 1989:2023 §8.4.3.15.3 SR1 admits BOTH report counters wherever an integer data item may appear:
*> "In the procedure division, PAGE-COUNTER and LINE-COUNTER may be referenced in any context where an
*> integer data item may appear." SR3 then removes exactly one of them from one position:
*> "LINE-COUNTER shall not be referenced as a receiving operand."
*>
*> This is the COMPLEMENT of conformance:85/pb429_page_counter_receiving, and it is what makes that golden
*> mean something: the two counters travel the same grammar, the same resolver and the same receiving
*> chokepoint, so a compiler that admitted PAGE-COUNTER by weakening the receiving screen would admit this
*> too. The rule is edition-invariant — the counters and SR3 are in the '85 standard unchanged — so every
*> edition rejects.
*>
*> ⚠ THE CODE IS THE POINT AS MUCH AS THE REJECTION. The refusal used to be emitted as COBOLNET0899, the
*> "recognized but not implemented" band, which tells the reader their LEGAL program is unsupported when in
*> fact their program is illegal; it now carries its own rule code, COBOLNET2197.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB429LCR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb429lcr.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS RPT1.
       WORKING-STORAGE SECTION.
       01  W PIC 9999 VALUE 0.
       REPORT SECTION.
       RD  RPT1 PAGE LIMIT 60 LINES.
       01  DETAIL-LINE TYPE IS DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE RPT1.
           GENERATE DETAIL-LINE.
           MOVE 5 TO LINE-COUNTER OF RPT1.
           MOVE LINE-COUNTER TO W.
           DISPLAY "W=" W.
           TERMINATE RPT1.
           CLOSE RPT.
           STOP RUN.
