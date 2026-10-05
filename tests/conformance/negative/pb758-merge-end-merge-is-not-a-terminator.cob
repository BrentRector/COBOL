      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB758 - END-MERGE is NOT COBOL, so `MERGE ... GIVING OUT1 END-MERGE` names a second
      *> GIVING file that does not exist. ISO §14.5.1 (Table 12): the MERGE row has no conditional phrase
      *> and no explicit scope terminator, so there is no scope for a terminator to delimit (§14.5.3.2);
      *> END-MERGE is in no ISO word list (§8.9) and is a user-defined word (§8.3.2.1, kb/Work PB1689),
      *> which the GIVING phrase's file-name list takes as file-name-4 (§14.9.24.2). Not a declined ISO
      *> facility (§4.2.7) - there is no facility - so the answer is the ordinary undefined-name error.
      *> Control: the same program with END-MERGE removed compiles and prints A1 B2 C1 D2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P758NEGM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MF ASSIGN TO "pb1689mf.tmp".
           SELECT IN1 ASSIGN TO "pb1689in1.tmp".
           SELECT IN2 ASSIGN TO "pb1689in2.tmp".
           SELECT OUT1 ASSIGN TO "pb1689out.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  MF.
       01  MF-REC PIC X(2).
       FD  IN1.
       01  IN1-REC PIC X(2).
       FD  IN2.
       01  IN2-REC PIC X(2).
       FD  OUT1.
       01  OUT1-REC PIC X(2).
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
           OPEN OUTPUT IN1
           MOVE "A1" TO IN1-REC WRITE IN1-REC
           MOVE "C1" TO IN1-REC WRITE IN1-REC
           CLOSE IN1
           OPEN OUTPUT IN2
           MOVE "B2" TO IN2-REC WRITE IN2-REC
           MOVE "D2" TO IN2-REC WRITE IN2-REC
           CLOSE IN2
           MERGE MF ON ASCENDING KEY MF-REC USING IN1 IN2 GIVING OUT1
           END-MERGE
           OPEN INPUT OUT1
           PERFORM UNTIL EOF-FLAG = "Y"
               READ OUT1
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY OUT1-REC
               END-READ
           END-PERFORM
           CLOSE OUT1
           STOP RUN.
