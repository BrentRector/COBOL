      *>--- conformance-case
      *> id: pb347_record_name_identity_85
      *> rules: [SR-14.9.32.3-1, SR-14.9.51.3-5, FMT-14.9.32.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 1989:2023 §14.9.32.3 SR1 / §14.9.51.3 SR5 at the OLDEST supported edition. The rules are
      *> written identically in ANSI X3.23-1985, so there is no edition gate: a qualified record-name-1
      *> binds at --std 85 exactly as at 2023, and the SD returns exactly the records RELEASE named
      *> (§14.9.32.4 GR2). This is the acceptance twin of the pb347-* negative corpus (kb/Work PB347).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB347R8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRTF ASSIGN TO "pb347r8.tmp".
           SELECT OUTF ASSIGN TO "pb347r8.dat"
               ORGANIZATION IS SEQUENTIAL.
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
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRTF ASCENDING KEY SR-KEY
                INPUT PROCEDURE IS IN-PROC
                OUTPUT PROCEDURE IS OUT-PROC.
           DISPLAY "N=" WS-N.
           PERFORM FD-PARA.
           STOP RUN.
       IN-PROC.
           MOVE "BBB" TO SR-KEY.
           MOVE "bbbbb" TO SR-DATA.
           RELEASE SRT-REC OF SRTF.
           MOVE "AAA" TO SR-KEY.
           MOVE "aaaaa" TO SR-DATA.
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
           MOVE "zzzzz" TO OR-DATA.
           WRITE OUT-REC OF OUTF.
           CLOSE OUTF.
           OPEN INPUT OUTF.
           READ OUTF AT END CONTINUE END-READ.
           DISPLAY "F1=[" OUT-REC "]".
           CLOSE OUTF.
