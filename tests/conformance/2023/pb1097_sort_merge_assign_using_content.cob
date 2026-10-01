      *> ISO 1989:2023 §12.4.5.3 GR3 b) and the implementor's content rule DOC-A.1-73 (Annex A.1 item 73; §12.4.5.3 GR4) -
      *> a sort-merge file's ASSIGN USING content is checked at EVERY SORT and MERGE, exactly as at every OPEN.
      *> GR3: "The association occurs at the time of execution of an OPEN, SORT, or MERGE statement that referenced
      *> file-name-1", and "If the association cannot be made because the content of the data item referenced by
      *> data-name-1 is not consistent with the specification for device-name-1 or literal-1, the OPEN, SORT, or
      *> MERGE statement is unsuccessful." DOC-A.1-73 makes content that is empty after the space removal, or that
      *> carries a character below U+0020, inconsistent. §3.176: an unsuccessful execution is one that "does not
      *> result in the execution of all the operations specified by that statement" - so a SORT or MERGE whose sort
      *> file cannot be associated releases nothing, sequences nothing and writes nothing. The GIVING file is the
      *> witness: it is created only by an implicit OPEN OUTPUT that the terminated statement never reaches.
      *>
      *> WHY EACH ARM CAN FAIL on its own:
      *>   ARM 1 - SORT ... USING ... GIVING, all-spaces content: if the statement ran, OUTF would exist (CHK=00).
      *>   ARM 2 - SORT with INPUT and OUTPUT PROCEDURE, a control character in the content: if the statement ran,
      *>           the input procedure would set IP and the output procedure OP.
      *>   ARM 3 - content with leading and trailing spaces around a real name is VALID (DOC-A.1-73: the spaces are
      *>           removed), so the same SORT runs and OUTF holds the two records in key order - the negative arms
      *>           cannot pass by the SORT simply never working.
      *>   ARM 4 - MERGE, all-spaces content: the MERGE is terminated too, OUTF is not created.
      *>   ARM 5 - MERGE with valid content runs and OUTF holds the merged records in key order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1097SM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT ASSIGN USING WS-N.
           SELECT INF ASSIGN TO "pb1097in.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT INF2 ASSIGN TO "pb1097in2.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT INF3 ASSIGN TO "pb1097in3.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT OUTF ASSIGN TO "pb1097out.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
           SELECT OPTIONAL CHK ASSIGN TO "pb1097out.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-C.
       DATA DIVISION.
       FILE SECTION.
       SD  SRT.
       01  SRT-REC PIC X(3).
       FD  INF.
       01  INF-REC PIC X(3).
       FD  INF2.
       01  INF2-REC PIC X(3).
       FD  INF3.
       01  INF3-REC PIC X(3).
       FD  OUTF.
       01  OUT-REC PIC X(3).
       FD  CHK.
       01  CHK-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-N   PIC X(16) VALUE SPACES.
       01  WS-C   PIC XX.
       01  WS-IP  PIC 9 VALUE 0.
       01  WS-OP  PIC 9 VALUE 0.
       01  WS-EOF PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M-1.
      *> The unsorted input for SORT, and the two pre-sorted inputs MERGE requires (§14.9.24.2 general format: USING file-name-2 {file-name-3} ... - two or more files).
           OPEN OUTPUT INF.
           MOVE "BBB" TO INF-REC.
           WRITE INF-REC.
           MOVE "AAA" TO INF-REC.
           WRITE INF-REC.
           CLOSE INF.
           OPEN OUTPUT INF2.
           MOVE "AAA" TO INF2-REC.
           WRITE INF2-REC.
           MOVE "CCC" TO INF2-REC.
           WRITE INF2-REC.
           CLOSE INF2.
           OPEN OUTPUT INF3.
           MOVE "BBB" TO INF3-REC.
           WRITE INF3-REC.
           MOVE "DDD" TO INF3-REC.
           WRITE INF3-REC.
           CLOSE INF3.
           PERFORM CLEAN-OUT.
      *> ARM 1.
           MOVE SPACES TO WS-N.
           SORT SRT ON ASCENDING KEY SRT-REC USING INF GIVING OUTF.
           OPEN INPUT CHK.
           DISPLAY "ARM1 CHK=" WS-C.
           CLOSE CHK.
      *> ARM 2.
           MOVE SPACES TO WS-N.
           MOVE X"01" TO WS-N (1:1).
           SORT SRT ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS IP
               OUTPUT PROCEDURE IS OP.
           DISPLAY "ARM2 IP=" WS-IP " OP=" WS-OP.
      *> ARM 3.
           MOVE "  pb1097w.dat  " TO WS-N.
           SORT SRT ON ASCENDING KEY SRT-REC USING INF GIVING OUTF.
           PERFORM SHOW-OUT.
           DISPLAY "ARM3 done".
      *> ARM 4.
           PERFORM CLEAN-OUT.
           MOVE SPACES TO WS-N.
           MERGE SRT ON ASCENDING KEY SRT-REC USING INF2 INF3 GIVING OUTF.
           OPEN INPUT CHK.
           DISPLAY "ARM4 CHK=" WS-C.
           CLOSE CHK.
      *> ARM 5.
           MOVE "pb1097m.dat" TO WS-N.
           MERGE SRT ON ASCENDING KEY SRT-REC USING INF2 INF3 GIVING OUTF.
           PERFORM SHOW-OUT.
           DISPLAY "ARM5 done".
           PERFORM CLEAN-OUT.
           STOP RUN.
       CLEAN-OUT.
           DELETE FILE OUTF.
       SHOW-OUT.
           OPEN INPUT CHK.
           DISPLAY "CHK=" WS-C.
           MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               READ CHK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "REC=" CHK-REC
               END-READ
           END-PERFORM.
           CLOSE CHK.
       IP SECTION.
       IP-1.
           MOVE 1 TO WS-IP.
           MOVE "AAA" TO SRT-REC.
           RELEASE SRT-REC.
       OP SECTION.
       OP-1.
           MOVE 1 TO WS-OP.
           RETURN SRT AT END CONTINUE END-RETURN.
