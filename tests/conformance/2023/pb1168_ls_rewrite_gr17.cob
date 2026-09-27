      *> kb/Work PB1168 -- a REWRITE on a LINE sequential file compares
      *> the number of bytes in record-name-1, untrimmed, with the line.
      *>   python scripts/spec/cite.py --check 14.9.35.4 "is greater than
      *>   the number of bytes in the record being replaced, the
      *>   execution of the REWRITE statement is unsuccessful"
      *>                                    -> OK  §14.9.35.4 17) (b)
      *>   python scripts/spec/cite.py --check 14.9.35.4 "then a
      *>   sufficient number of the space character is appended"
      *>                                    -> OK  §14.9.35.4 17) (c)
      *> F is RECORD VARYING, so GR16's equality rule -- stated "For a
      *> record sequential file" -- is not this file's rule: R1's 10
      *> bytes are FEWER than the 20 of the line being replaced, so
      *> GR17 c) space-fills R1 to 20 and the REWRITE succeeds ('00');
      *> the line becomes SHORT followed by 15 spaces. G1 is 10 bytes
      *> and G's line is AB (2 bytes -- the WRITE did not transfer the
      *> trailing spaces, §14.9.51.4 GR21): 10 is GREATER than 2, so
      *> GR17 b) makes the REWRITE unsuccessful ('44') and the line
      *> stays AB. GR17 has no trailing-space rule: comparing HI's
      *> trimmed length (2) instead would have replaced the line.
      *> The lines are read back through a second FD over each file
      *> with a ONE-character record sequential record (§12.4.5.10.3
      *> GR3), counting the characters before the line delimiter.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1168LS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1168lf.txt"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS ST.
           SELECT G ASSIGN TO "pb1168lg.txt"
               ORGANIZATION IS LINE SEQUENTIAL FILE STATUS IS ST.
           SELECT FB ASSIGN TO "pb1168lf.txt"
               ORGANIZATION IS SEQUENTIAL.
           SELECT GB ASSIGN TO "pb1168lg.txt"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS.
       01 R1 PIC X(10).
       01 R2 PIC X(20).
       FD G.
       01 G1 PIC X(10).
       FD FB.
       01 FB-REC PIC X.
       FD GB.
       01 GB-REC PIC X.
       WORKING-STORAGE SECTION.
       01 ST     PIC XX.
       01 WS-EOF PIC X.
       01 WS-N   PIC 99.
       01 WS-TXT PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F
           MOVE ALL "B" TO R2
           WRITE R2
           CLOSE F
           OPEN I-O F
           READ F
           MOVE "SHORT" TO R1
           REWRITE R1
           DISPLAY "LS-VAR-SHORTER=" ST
           CLOSE F
           OPEN OUTPUT G
           MOVE "AB" TO G1
           WRITE G1
           CLOSE G
           OPEN I-O G
           READ G
           MOVE "HI" TO G1
           REWRITE G1
           DISPLAY "LS-10-ON-2=" ST
           CLOSE G
           MOVE "N" TO WS-EOF
           MOVE 0 TO WS-N
           MOVE SPACES TO WS-TXT
           OPEN INPUT FB
           PERFORM UNTIL WS-EOF = "Y"
               READ FB
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       IF FB-REC = X"0D" OR FB-REC = X"0A"
                           MOVE "Y" TO WS-EOF
                       ELSE
                           ADD 1 TO WS-N
                           MOVE FB-REC TO WS-TXT(WS-N:1)
                       END-IF
               END-READ
           END-PERFORM
           CLOSE FB
           DISPLAY "FLINE=" WS-TXT "|" WS-N
           MOVE "N" TO WS-EOF
           MOVE 0 TO WS-N
           MOVE SPACES TO WS-TXT
           OPEN INPUT GB
           PERFORM UNTIL WS-EOF = "Y"
               READ GB
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       IF GB-REC = X"0D" OR GB-REC = X"0A"
                           MOVE "Y" TO WS-EOF
                       ELSE
                           ADD 1 TO WS-N
                           MOVE GB-REC TO WS-TXT(WS-N:1)
                       END-IF
               END-READ
           END-PERFORM
           CLOSE GB
           DISPLAY "GLINE=" WS-TXT "|" WS-N
           STOP RUN.
