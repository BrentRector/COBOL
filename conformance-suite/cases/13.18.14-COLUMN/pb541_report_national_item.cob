*>--- conformance-case
*> id: pb541_report_national_item
*> rules: [GR-13.18.14.4-2, SR-13.18.60.3-7, DOC-A.1-160]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ⛔ A REPORT GROUP ITEM MAY BE NATIONAL, AND A USAGE CLAUSE ON A REPORT GROUP ENTRY IS NOT SCENERY
      *> (kb/Work PB541). ISO/IEC 1989:2023 §13.18.60.3 SR7 names TWO admitted phrases: "Only the DISPLAY or
      *> NATIONAL phrase may be specified in any USAGE clause associated with a report group item." The gate
      *> this golden replaces tested `usage is not DISPLAY` — one alternative narrower than the rule — so every
      *> NATIONAL report item was refused at 2002, 2014 and 2023 under a §13.15 citation that says nothing of
      *> the kind (the nearest real text, §13.18.14.4 GR3, is about the column/character correspondence).
      *>
      *> THREE WAYS AN ENTRY ARRIVES AT ITS USAGE, and SR7 admits all three here:
      *>
      *> LINE 1 — the entry's OWN clause. `USAGE NATIONAL` written on the printable entry itself, beside a
      *>   plain DISPLAY entry on the same line: SR7's two alternatives, coexisting. The national item's image
      *>   occupies one COLUMN per character — §13.18.14.4 GR9 advances the horizontal counter by the item's
      *>   size — so WXYZ fills columns 1-4 and the DISPLAY item at COLUMN 6 lands after one blank column.
      *>
      *> LINE 2 — INHERITED from the group entry above it. §13.18.60.4 GR1: "If the USAGE clause is specified
      *>   or implied at a group level, it applies only to each elementary item in the group." The 03 entry
      *>   carries USAGE NATIONAL and prints nothing itself; its subordinate 05 has no USAGE clause and is a
      *>   national item because of GR1. Before this, a USAGE clause on a report GROUP entry was captured and
      *>   then silently DISCARDED — `03 USAGE COMP.` over a printable item compiled and ran, which is the
      *>   complement pinned by conformance:negative/pb541-report-usage-comp.
      *>
      *> LINE 3 — IMPLIED by the picture character-string. §13.18.60.4 GR8 speaks of "the implicit or explicit
      *>   USAGE NATIONAL clause", and §13.18.40.3 makes a picture of all N symbols a national item, so this
      *>   entry is national with no USAGE clause anywhere. It is the arm a screen written over the USAGE
      *>   CLAUSE alone would miss, which is why the rule is asked once more of the usage the entry settles on.
      *>
      *> EDITION. National data is a COBOL-2002 introduction (§8.5.2 category national), so 2002 is the
      *> introducing edition for a NATIONAL report item and conformance:negative/pb541-report-national-at-85
      *> is the negative below it. The behaviour does not differ across 2002/2014/2023, so there is one copy.
      *>
      *> THE READ-BACK IS BYTE-WISE because ORGANIZATION LINE SEQUENTIAL is gated at 2023 in this compiler; a
      *> one-character record on a second SELECT over the same file reassembles each line at any edition. The
      *> national characters here are all in the 7-bit range on purpose: the print stream maps a character
      *> above it to '?' (SequentialConnector.PrintSafe, the implementor-defined print encoding the NIST golden
      *> corpus pins), which is a separate question from the one SR7 answers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB541RNI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb541rni.txt".
           SELECT CHK ASSIGN TO "pb541rni.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-N.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WN1     PIC N(4)  VALUE N"WXYZ".
       01  WN2     PIC N(3)  VALUE N"PQR".
       01  WN3     PIC N(2)  VALUE N"MN".
       REPORT SECTION.
       RD  R-N PAGE LIMIT IS 10 LINES.
       01  DET-N TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC N(4) USAGE NATIONAL SOURCE WN1.
               03  COLUMN 6 PIC X(3) VALUE "ABC".
           02  LINE PLUS 1.
               03  USAGE NATIONAL.
                   04  COLUMN 1 PIC N(3) SOURCE WN2.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC N(2) SOURCE WN3.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-N.
           GENERATE DET-N.
           TERMINATE R-N.
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
