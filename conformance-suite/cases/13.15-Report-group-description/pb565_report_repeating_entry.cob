*>--- conformance-case
*> id: pb565_report_repeating_entry
*> rules: [GR-13.15.4-3, SR-13.18.38.3-16, SR-13.18.38.3-24, SR-13.18.38.3-25, SR-13.18.38.3-26, SR-13.18.38.3-27, GR-13.18.38.4-10, GR-13.18.38.4-11, GR-13.18.38.4-12, GR-13.18.38.4-13, GR-13.18.63.4-9, GR-13.18.63.4-21, GR-13.18.63.4-22, GR-13.18.64.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ⛔ A REPORT GROUP DESCRIPTION ENTRY MAY BE A REPEATING ENTRY, AND EVERY REPETITION IS A REAL REPORT
      *> ITEM (kb/Work PB565). Before this, an OCCURS clause anywhere in a report group was refused by name
      *> (COBOLNET0899) — conforming source rejected — and with it two general rules had no population at all:
      *> §13.18.63.4 GR21's import of GR9 and GR22's OCCURS-DEPENDING suppressor.
      *>
      *> THE CLAUSE. §13.18.38.2 format 3 (the report-writer format, read off the printed general-format
      *> diagram): OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ].
      *> §13.18.38.4 GR10: "If the OCCURS clause is written without any of the optional phrases, it causes the
      *> entry to define integer-2 distinct report items." GR11: "Any PICTURE, USAGE, SIGN, VALUE, JUSTIFIED,
      *> BLANK WHEN ZERO, or GROUP INDICATE clauses have the same effect on each repetition as they would on a
      *> single data item without the OCCURS clause."
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT, line by line.
      *>
      *> LINE 1 — an absolute COLUMN with STEP. §13.18.38.4 GR12a: "If the entry contains a COLUMN clause, each
      *> successive occurrence is printed at a horizontal distance integer-3 columns to the right of the
      *> preceding occurrence." COLUMN 1, PIC X(3), OCCURS 3 TIMES STEP 4 ⇒ leftmost columns 1, 5, 9, and
      *> §13.18.63.4 GR21's GR9 ("A VALUE clause specified in a data description entry that contains an OCCURS
      *> clause … causes every occurrence of the associated data item to be assigned the specified value")
      *> puts AAA in all three ⇒ `AAA AAA AAA`. (§13.18.38.3 SR25c REQUIRES the STEP phrase here: an absolute
      *> COLUMN would otherwise print every repetition in the same column.)
      *>
      *> LINE 2 — a relative COLUMN, no STEP. GR12's closing sentence: "If no STEP phrase is specified, the
      *> vertical or horizontal interval between successive occurrences is defined by the relative LINE or
      *> COLUMN numbers, respectively, specified in the corresponding report section entries." COLUMN PLUS 1
      *> against the §13.18.14.4 GR7 horizontal counter (0 at line start): leftmost 0+1 = 1, counter becomes
      *> 1+3-1 = 3 (GR9), leftmost 3+1 = 4, counter 6, leftmost 7 ⇒ BBB at 1, 4, 7 ⇒ `BBBBBBBBB`.
      *>
      *> LINE 3 — a repeated GROUP entry: GR9's OTHER leg, "or in an entry that is subordinate to an OCCURS
      *> clause". The 03 is a group entry with two subordinate printable items at COLUMN 1 and COLUMN 5,
      *> OCCURS 2 TIMES STEP 10; GR12b: "printable items in each successive occurrence are positioned
      *> integer-3 columns to the right of the column they occupy in the preceding occurrence" ⇒ DDD at 1 and
      *> 11, EEE at 5 and 15 ⇒ `DDD EEE   DDD EEE`.
      *>
      *> LINE 4 — OCCURS … DEPENDING. §13.18.38.4 GR13: data-name-1 "is evaluated just before the processing
      *> for the first LINE clause of the report group… If the value of data-name-1 is in the range integer-1
      *> to (integer-2 - 1), the OCCURS clause has the same effect as an OCCURS clause with no TO or DEPENDING
      *> phrases and with an integer-2 equal to the current value of data-name-1." WS-N = 2 lies in 1 … 3, so
      *> two of the four repetitions appear: CCC at columns 1 and 5 ⇒ `CCC CCC`. §13.18.63.4 GR22 names "an
      *> OCCURS clause with the DEPENDING phrase" as a suppressor of the item's appearance, and GR23's last
      *> sentence keeps the suppressed ones ASSIGNED ("VALUE operands are nevertheless assigned to them, even
      *> though they are not printed"). §13.18.38.3 SR27 is why this entry is written LAST in the group.
      *>
      *> LINE 5 — VARYING over the OCCURS vehicle. §13.18.64.3 SR1 names the OCCURS clause as a vehicle for the
      *> VARYING clause, and §13.18.64.4 GR3 fixes the counter per repetition: "For the first occurrence, the
      *> value of arithmetic-expression-1 is moved to data-name-1… For the second and subsequent occurrences,
      *> the value of arithmetic-expression-2 is added to data-name-1." FROM 2 BY 3 ⇒ 2, 5, 8; the counter is
      *> the source item (GR4 NOTE) into PIC 9 at columns 1, 4, 7 (STEP 3) ⇒ `2  5  8`.
      *>
      *> THE READ-BACK IS BYTE-WISE ON PURPOSE. A report file's lines are newline-delimited, and
      *> `ORGANIZATION IS LINE SEQUENTIAL` — the natural reader — is a COBOL-2023 introduction (§12.4.5.10.3
      *> GR2), so it cannot appear in a COBOL-2002 program at all (kb/Work PB688 removed exactly that from an
      *> '85 report golden). A one-character record on a second SELECT over the same file is legal at every
      *> edition and reassembles each line, so this golden pins the CONTENT at the rule's INTRODUCING edition
      *> rather than only its acceptance.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565RRE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb565rre.txt".
           SELECT CHK ASSIGN TO "pb565rre.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-RE.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-N   PIC 9 VALUE 2.
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-RE PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) OCCURS 3 TIMES STEP 4 VALUE "AAA".
           02  LINE PLUS 1.
               03  COLUMN PLUS 1 PIC X(3) OCCURS 3 TIMES VALUE "BBB".
           02  LINE PLUS 1.
               03  OCCURS 2 TIMES STEP 10.
                   04  COLUMN 1 PIC X(3) VALUE "DDD".
                   04  COLUMN 5 PIC X(3) VALUE "EEE".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 3
                   VARYING K FROM 2 BY 3 SOURCE K.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) OCCURS 1 TO 4 TIMES
                   DEPENDING ON WS-N STEP 4 VALUE "CCC".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-RE.
           GENERATE DET-A.
           TERMINATE R-RE.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
