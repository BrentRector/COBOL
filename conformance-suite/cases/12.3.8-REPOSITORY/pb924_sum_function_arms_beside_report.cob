*>--- conformance-case
*> id: pb924_sum_function_arms_beside_report
*> rules: [GR-12.3.8.4-13, SR-13.18.54.3-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> THE OTHER ARM OF ISO 13.18.54.3 SR9 (kb/Work PB924): "Within a
      *> report description entry, if the keyword SUM is preceded by the
      *> keyword FUNCTION, SUM is a reference to the SUM intrinsic
      *> function.  Otherwise, SUM refers to the report writer SUM clause."
      *> The fix makes a bare SUM's '(' an arithmetic parenthesis INSIDE
      *> the report section only; this program pins what that must not
      *> break, in a unit that HAS a report section:
      *>   - FUNCTION SUM(1 2 3) in a report SOURCE clause is the function
      *>     (SR9 first sentence): 1 + 2 + 3 = 6                 => [0006]
      *>   - in the same report, SUM (WS-K * 2) 2 is the clause: one
      *>     GENERATE, 5 * 2 = 10 plus the second addend 2 (GR9) => [0012]
      *>   - after the report section closes, the keyword-omitted SUM(4 5)
      *>     (8.4.3.2.3 SR2, SUM declared in the REPOSITORY) is again the
      *>     intrinsic: 4 + 5 = 9                           => N = 09
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB924SFA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SUM INTRINSIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb924sfa.txt".
           SELECT CHK ASSIGN TO "pb924sfa.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SF.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-K    PIC 99    VALUE 5.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  N       PIC 99    VALUE 0.
       REPORT SECTION.
       RD  R-SF CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC 9999 SOURCE FUNCTION SUM(1 2 3).
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9999 SUM (WS-K * 2) 2.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N = SUM(4 5).
           DISPLAY "N = " N.
           OPEN OUTPUT PRT.
           INITIATE R-SF.
           GENERATE DET.
           TERMINATE R-SF.
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
