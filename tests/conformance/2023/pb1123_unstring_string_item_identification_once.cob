      *> PB1123 (UNSTRING and STRING arms) - ISO 14.6.4 7): "the identifiers within a statement are evaluated in
      *>   left to right order as the first operation of the execution of that statement."
      *>   (cite.py --check 14.6.4 -> OK 7))  14.9.48.4 (UNSTRING) and 14.9.43.4 (STRING) state no other timing,
      *>   unlike MOVE (14.9.25.4 GR1) or SET, so every subscript of the statement is evaluated BEFORE the first
      *>   store: a subscript that names an EARLIER receiver of the same statement keeps the value it had at the
      *>   start of the statement.
      *> U1 - UNSTRING "3,QQ,RR" INTO I UE(I): the first area stores 3 into I, the second area is UE(1) - I was 1
      *>      when UE(I) was identified - so UE(1) = "QQ" and the table reads QQ------.
      *> U2 - WITH POINTER PE(I) while I is the INTO receiver: the pointer item is PE(1), which receives the
      *>      pointer's final value 3 (the "3," consumed) => 03010101.
      *> U3 - DELIMITER IN DL(I) and COUNT IN CE(I) with I the INTO receiver: DL(1) holds the "," delimiter and
      *>      CE(1) the count 1 => DL = ",---" and CE = 1000.
      *> U4 - TALLYING IN TE(I) with I the INTO receiver: TE(1) is incremented once for the one area
      *>      acted upon => TE = 1000.
      *> S1 - STRING "3" INTO I WITH POINTER PE(I) with I the receiver: PE(1) (identified with I = 1)
      *>      receives the final pointer 2 => PE = 02010101 (PE(3) is not touched).
      *>
      *>   U1=QQ------
      *>   U2=03010101
      *>   U3=,--- 1000
      *>   U4=1000
      *>   S1=02010101
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1123UNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 1.
       01 S PIC X(10) VALUE "3,QQ,RR".
       01 U.
          05 UE PIC X(2) OCCURS 4 VALUE "--".
       01 PT.
          05 PE PIC 99 OCCURS 4 VALUE 1.
       01 DT.
          05 DL PIC X OCCURS 4 VALUE "-".
       01 CT.
          05 CE PIC 9 OCCURS 4 VALUE 0.
       01 TL.
          05 TE PIC 9 OCCURS 4 VALUE 0.
       PROCEDURE DIVISION.
           UNSTRING S DELIMITED "," INTO I UE(I)
           DISPLAY "U1=" U
           MOVE 1 TO I
           UNSTRING S DELIMITED "," INTO I WITH POINTER PE(I)
           DISPLAY "U2=" PT
           MOVE 1 TO I
           UNSTRING S DELIMITED "," INTO I
              DELIMITER IN DL(I) COUNT IN CE(I)
           DISPLAY "U3=" DT " " CT
           MOVE 1 TO I
           UNSTRING S DELIMITED "," INTO I TALLYING IN TE(I)
           DISPLAY "U4=" TL
           MOVE 1 TO I
           MOVE 1 TO PE(1) PE(2) PE(3) PE(4)
           STRING "3" DELIMITED BY SIZE INTO I WITH POINTER PE(I)
           DISPLAY "S1=" PT
           STOP RUN.
