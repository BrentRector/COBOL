      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB758 - END-SORT is NOT COBOL, so `SORT ... GIVING OUT1 END-SORT` names a second
      *> GIVING file that does not exist. ISO §14.5.1 (Table 12): the SORT row has no conditional phrase
      *> and no explicit scope terminator, so there is no scope for a terminator to delimit (§14.5.3.2);
      *> END-SORT is in no ISO word list (§8.9) and is a user-defined word (§8.3.2.1, kb/Work PB1689),
      *> which the GIVING phrase's file-name list takes as file-name-3 (§14.9.40.2). Not a declined ISO
      *> facility (§4.2.7) - there is no facility - so the answer is the ordinary undefined-name error.
      *> Control: conformance/2023/pb1689_vendor_channel_clause_still_parses is this program without
      *> END-SORT, and prints SORTED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P758NEGS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p758sf.tmp".
           SELECT IN1 ASSIGN TO "p758in.tmp".
           SELECT OUT1 ASSIGN TO "p758out.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SF-REC PIC X(2).
       FD  IN1.
       01  IN1-REC PIC X(2).
       FD  OUT1.
       01  OUT1-REC PIC X(2).
       PROCEDURE DIVISION.
           OPEN OUTPUT IN1
           MOVE "B1" TO IN1-REC WRITE IN1-REC
           MOVE "A1" TO IN1-REC WRITE IN1-REC
           CLOSE IN1
           SORT SF ON ASCENDING KEY SF-REC USING IN1 GIVING OUT1
           END-SORT
           DISPLAY "SORTED"
           STOP RUN.
