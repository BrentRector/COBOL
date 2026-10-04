      *> kb/Work PB1094 - every organization and the sort carry a
      *> record whose dynamic-length members name a DYNAMIC LENGTH
      *> STRUCTURE in the form GR18 and GR19 of ISO 12.3.7.4 give it
      *> (length field before the data, delimiter after), and a key that
      *> follows such a member is found where the record puts it, not
      *> where the unstructured record would (12.4.5.12.3 SR4: a key
      *> lies within the first n bytes, n the minimum record size;
      *> 14.9.40.4 GR2: the key data items order the SORT).  A record
      *> that holds a structured member CONTAINS its length field and
      *> delimiter
      *> (docs/CONFORMANCE.md section 3 D-DL3), so 13.18.43.4 GR8's size
      *> counts them: IX's smallest record is 2 (D3's length field) + 3
      *> (key) + 1 (D4's delimiter) + 10 (PAD) = 16 bytes, and its key
      *> reaches byte 2 + 5 + 3 = 10 at most.  The expected lines
      *> follow from the values written, which every organization must
      *> give back unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1094-ORGS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-DELIM IS DELIMITED
           DYNAMIC LENGTH STRUCTURE DS-SHORT IS SHORT PREFIXED.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "pb1094org.idx"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK.
           SELECT IF30 ASSIGN TO "pb1094org.if30"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS YK.
           SELECT RL ASSIGN TO "pb1094org.rel"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
           SELECT SQ ASSIGN TO "pb1094org.seq"
               ORGANIZATION IS SEQUENTIAL.
           SELECT GV ASSIGN TO "pb1094org.giv"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SF ASSIGN TO "pb1094org.srt".
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 XR.
          05 D3 PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 5.
          05 XK PIC X(3).
          05 D4 PIC X DYNAMIC LENGTH DS-DELIM LIMIT IS 5.
          05 PAD PIC X(10).
       FD IF30 RECORD CONTAINS 40 CHARACTERS.
       01 YR.
          05 D5 PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 5.
          05 YK PIC X(3).
          05 D6 PIC X DYNAMIC LENGTH DS-DELIM LIMIT IS 5.
       FD RL.
       01 RR.
          05 E1 PIC X DYNAMIC LENGTH DS-SHORT.
          05 E2 PIC X(2).
       FD SQ.
       01 SQR.
          05 QA PIC X DYNAMIC LENGTH DS-DELIM LIMIT IS 5.
          05 QK PIC X(3).
          05 QC PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 8.
          05 QPAD PIC X(10).
       FD GV.
       01 GVR.
          05 GA PIC X DYNAMIC LENGTH DS-DELIM LIMIT IS 5.
          05 GK PIC X(3).
          05 GC PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 8.
          05 GPAD PIC X(10).
       SD SF.
       01 SR.
          05 SA PIC X DYNAMIC LENGTH DS-DELIM LIMIT IS 5.
          05 SK PIC X(3).
          05 SC PIC X DYNAMIC LENGTH DS-SHORT LIMIT IS 8.
          05 SPAD PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(2).
       01 MORE PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-SECTION SECTION.
       MAIN-PARA.
      *> INDEXED, variable-length records: the key follows D3.
           OPEN OUTPUT IX.
           MOVE "AB" TO D3.
           MOVE "K01" TO XK.
           MOVE "CD" TO D4.
           WRITE XR.
           MOVE "EFG" TO D3.
           MOVE "K02" TO XK.
           MOVE "H" TO D4.
           WRITE XR.
           MOVE "" TO D3.
           MOVE "K03" TO XK.
           MOVE "IJKLM" TO D4.
           WRITE XR.
           CLOSE IX.
           OPEN I-O IX.
           MOVE "K02" TO XK.
           READ IX KEY IS XK.
           DISPLAY "IX K02 [" D3 "] [" D4 "]".
           MOVE "NEW" TO D4.
           REWRITE XR.
           MOVE "K01" TO XK.
           DELETE IX RECORD.
           MOVE LOW-VALUES TO XK.
           START IX KEY IS NOT LESS THAN XK.
           MOVE "Y" TO MORE.
           PERFORM UNTIL MORE = "N"
               READ IX NEXT RECORD
                   AT END MOVE "N" TO MORE
                   NOT AT END
                       DISPLAY "IX NEXT " XK " [" D3 "] [" D4 "]"
               END-READ
           END-PERFORM.
           CLOSE IX.
      *> INDEXED, fixed-length records (Format 1): the fixed form.
           OPEN OUTPUT IF30.
           MOVE "AB" TO D5.
           MOVE "K01" TO YK.
           MOVE "CD" TO D6.
           WRITE YR.
           MOVE "EFGHI" TO D5.
           MOVE "K02" TO YK.
           MOVE "J" TO D6.
           WRITE YR.
           CLOSE IF30.
           OPEN INPUT IF30.
           MOVE "K02" TO YK.
           READ IF30 KEY IS YK.
           DISPLAY "IF30 K02 [" D5 "] [" D6 "]".
           MOVE "K01" TO YK.
           READ IF30 KEY IS YK.
           DISPLAY "IF30 K01 [" D5 "] [" D6 "]".
           CLOSE IF30.
      *> RELATIVE.
           OPEN OUTPUT RL.
           MOVE 3 TO RK.
           MOVE "RELATIVE" TO E1.
           MOVE "ZZ" TO E2.
           WRITE RR.
           CLOSE RL.
           MOVE "QQ" TO E1.
           OPEN INPUT RL.
           MOVE 3 TO RK.
           READ RL.
           DISPLAY "RL 3 [" E1 "] " E2.
           CLOSE RL.
      *> SORT: USING / GIVING, then an INPUT and OUTPUT PROCEDURE.
           OPEN OUTPUT SQ.
           MOVE "AB" TO QA.
           MOVE "K03" TO QK.
           MOVE "CDE" TO QC.
           WRITE SQR.
           MOVE "FGHIJ" TO QA.
           MOVE "K01" TO QK.
           MOVE "" TO QC.
           WRITE SQR.
           MOVE "" TO QA.
           MOVE "K02" TO QK.
           MOVE "LMNOPQRS" TO QC.
           WRITE SQR.
           CLOSE SQ.
           SORT SF ON ASCENDING KEY SK USING SQ GIVING GV.
           OPEN INPUT GV.
           MOVE "Y" TO MORE.
           PERFORM UNTIL MORE = "N"
               READ GV
                   AT END MOVE "N" TO MORE
                   NOT AT END
                       DISPLAY "GIVING " GK " [" GA "] [" GC "]"
               END-READ
           END-PERFORM.
           CLOSE GV.
           SORT SF ON DESCENDING KEY SK INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           STOP RUN.
       FEED SECTION.
       FEED-PARA.
           OPEN INPUT SQ.
           MOVE "Y" TO MORE.
           PERFORM UNTIL MORE = "N"
               READ SQ
                   AT END MOVE "N" TO MORE
                   NOT AT END
                       MOVE QA TO SA
                       MOVE QK TO SK
                       MOVE QC TO SC
                       RELEASE SR
               END-READ
           END-PERFORM.
           CLOSE SQ.
       DRAIN SECTION.
       DRAIN-PARA.
           MOVE "Y" TO MORE.
           PERFORM UNTIL MORE = "N"
               RETURN SF
                   AT END MOVE "N" TO MORE
                   NOT AT END
                       DISPLAY "RETURN " SK " [" SA "] [" SC "]"
               END-RETURN
           END-PERFORM.
