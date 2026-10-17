*>--- conformance-case
*> id: pb792_report_value_then_column
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ⛔ THE REPORT-GROUP CLAUSE ORDER IS FREE, AND THE VALUE OPERAND LIST MUST STOP AT THE NEXT CLAUSE
      *> (kb/Work PB792). ISO §13.15.3 SR2: "If the entry-name clause is specified, the data-name format or
      *> filler format shall be specified. The entry-name clause shall immediately follow the level-number. All
      *> other clauses may be written in any order." So a report group description entry may write its
      *> §13.18.63 VALUE clause BEFORE its §13.18.14 COLUMN clause, or after it, and both orders name the same
      *> entry. NIST RW104A card 024500 writes the first order:
      *>     03  VALUE "DETAIL LINE "  COLUMN 20    PIC X(12).
      *>
      *> ⛔ AND THE VALUE OPERAND LIST IS PLURAL BY DESIGN — DO NOT NARROW IT. §13.18.63.2 format 4
      *> (report-section) prints `{ VALUE IS | VALUES ARE } { literal-1 } …`, and §5.2.7 makes the ellipsis
      *> repeat the braced group, so the clause takes one or MORE literals; §13.18.63.3 SR35 confines the
      *> multi-operand form to a repeating entry ("If the VALUE clause has more than one operand, the entry
      *> shall be a repeating entry or shall be subordinate to a repeating entry"), which is why the entries
      *> here carry exactly one literal each. The list therefore has to be greedy, and what stops it is the §8.9
      *> reservation gate on cobolWord: COLUMN is a reserved word at all four editions (§8.3.2.1 rule 1 —
      *> "Reserved words shall not be used as user-defined words or system-names"), so it can never be a VALUE
      *> operand. Before PB792 the migration mode (--permissive) handed the whole gated set back to cobolWord,
      *> the operand loop ate COLUMN and 20, and this line printed at column 1 with no diagnostic at all.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT. The report has one TYPE DE group with two elementary entries.
      *> §13.18.14.4 GR1/GR2: COLUMN NUMBER integer-1 places the leftmost character of the field in that column
      *> of the print line. Entry 1 is COLUMN 20 PIC X(2) VALUE "AB" — characters 20-21 are "AB". Entry 2 is
      *> COLUMN 30 PIC X(2) VALUE "CD" (the same clauses in the opposite order) — characters 30-31 are "CD".
      *> §13.18.63.4 format 4 makes the literal the presentation value of the field, and §13.18.35 LINE PLUS 1
      *> puts the single GENERATE's detail on one line. The check paragraph reads the report file back and
      *> displays those two two-character windows plus the column numbers it found them at, so the expected
      *> output is AB@20 CD@30.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB792RVC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb792rvc.txt".
           SELECT CHK ASSIGN TO "pb792rvc.txt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-VC.
       FD  CHK.
       01  CHK-REC PIC X(40).
       WORKING-STORAGE SECTION.
       01  WS-EOF   PIC X  VALUE "N".
       01  WS-AT-AB PIC 99 VALUE 0.
       01  WS-AT-CD PIC 99 VALUE 0.
       01  WS-I     PIC 99.
       REPORT SECTION.
       RD  R-VC PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
      *> §13.15.3 SR2 order A — the RW104A shape: VALUE, then the COLUMN clause.
           03  VALUE "AB" COLUMN 20 PIC X(2).
      *> §13.15.3 SR2 order B — the same two clauses, written the other way round.
           03  COLUMN 30 VALUE "CD" PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-VC.
           GENERATE DET.
           TERMINATE R-VC.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM SCAN-LINE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           DISPLAY "AB@" WS-AT-AB " CD@" WS-AT-CD.
           STOP RUN.
       SCAN-LINE.
           PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 39
               IF CHK-REC(WS-I:2) = "AB" AND WS-AT-AB = 0
                   MOVE WS-I TO WS-AT-AB
               END-IF
               IF CHK-REC(WS-I:2) = "CD" AND WS-AT-CD = 0
                   MOVE WS-I TO WS-AT-CD
               END-IF
           END-PERFORM.
