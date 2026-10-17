*>--- conformance-case
*> id: pb347_record_name_identity
*> rules: [SR-14.9.32.3-1, SR-14.9.35.3-1, SR-14.9.51.3-1,
*>    SR-14.9.51.3-5, FMT-14.9.32.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §14.9.32.3 SR1 / §14.9.51.3 SR5 / §14.9.35.3 SR1 - the ACCEPT side of the
      *> record-name-1 operand rule: the operand IS a logical record of a file description entry, and all
      *> three rules end "and it may be qualified", so the OF form binds. Each RELEASE releases exactly the
      *> record named (§14.9.32.4 GR2), so the count is the number of RELEASE statements executed and
      *> nothing else reaches the sort (kb/Work PB347).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB347RI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb347ri.tmp".
           SELECT OUTF ASSIGN TO "pb347ri.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       SD  SRTF.
       01  SRT-REC.
           05  SR-KEY   PIC X(3).
           05  SR-DATA  PIC X(5).
       FD  OUTF.
       01  OUT-REC.
           05  OR-KEY   PIC X(3).
           05  OR-DATA  PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-N    PIC 9(3) VALUE 0.
       01  WS-ST   PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRTF ASCENDING KEY SR-KEY
                INPUT PROCEDURE IS IN-PROC
                OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "N=" WS-N.
           PERFORM FD-PARA.
           STOP RUN.
       IN-PROC.
           MOVE "CCC" TO SR-KEY.
           MOVE "ccccc" TO SR-DATA.
           RELEASE SRT-REC.
           MOVE "AAA" TO SR-KEY.
           MOVE "aaaaa" TO SR-DATA.
           RELEASE SRT-REC OF SRTF.
           MOVE "BBB" TO SR-KEY.
           MOVE "bbbbb" TO SR-DATA.
           RELEASE SRT-REC.
       OUT-PROC.
           PERFORM UNTIL WS-EOF = "Y"
               RETURN SRTF
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "R=[" SRT-REC "]"
                              ADD 1 TO WS-N
               END-RETURN
           END-PERFORM.
       FD-PARA.
           OPEN OUTPUT OUTF.
           MOVE "111" TO OR-KEY.
           MOVE "aaaaa" TO OR-DATA.
           WRITE OUT-REC.
           MOVE "222" TO OR-KEY.
           MOVE "bbbbb" TO OR-DATA.
           WRITE OUT-REC OF OUTF.
           CLOSE OUTF.
           OPEN I-O OUTF.
           READ OUTF AT END CONTINUE END-READ.
           MOVE "999" TO OR-KEY.
           REWRITE OUT-REC OF OUTF.
           DISPLAY "RWST=" WS-ST.
           CLOSE OUTF.
           OPEN INPUT OUTF.
           READ OUTF AT END CONTINUE END-READ.
           DISPLAY "F1=[" OUT-REC "]".
           READ OUTF AT END CONTINUE END-READ.
           DISPLAY "F2=[" OUT-REC "]".
           CLOSE OUTF.
