      *> PB1184 - ISO 14.9.48.4 GR2: "If the data item referenced by
      *>   identifier-1 is a zero-length item, execution of the UNSTRING
      *>   statement terminates immediately."
      *> cite.py --check 14.9.48.4 "If the data item referenced by
      *>   identifier-1 is a zero-length item, execution of the UNSTRING
      *>   statement terminates immediately" -> OK  14.9.48.4 2)
      *> Derivation: GR2 ends the statement before any other rule, so no
      *>   overflow condition exists (GR15 a is never reached) and neither
      *>   the ON nor the NOT ON OVERFLOW imperative runs (GR17 acts after
      *>   a completed transfer, and none took place); A1, the pointer and
      *>   the tally are untouched. T1 and T2 print only their own line.
      *>   T3 is the control: a non-zero-length sender still takes the
      *>   NOT ON OVERFLOW arm (all of "A" is examined, one receiver).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1184.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X DYNAMIC LENGTH.
       01 A1 PIC X(4) VALUE "1111".
       01 P PIC 9 VALUE 1.
       01 T PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           MOVE "" TO WS-D
           UNSTRING WS-D DELIMITED BY "," INTO A1
               ON OVERFLOW DISPLAY "T1 OVF"
               NOT ON OVERFLOW DISPLAY "T1 NOVF"
           END-UNSTRING
           DISPLAY "T1 " A1
           UNSTRING WS-D DELIMITED BY "," INTO A1
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "T2 OVF"
               NOT ON OVERFLOW DISPLAY "T2 NOVF"
           END-UNSTRING
           DISPLAY "T2 " A1 " " P " " T
           MOVE "A" TO WS-D
           UNSTRING WS-D DELIMITED BY "," INTO A1
               ON OVERFLOW DISPLAY "T3 OVF"
               NOT ON OVERFLOW DISPLAY "T3 NOVF"
           END-UNSTRING
           DISPLAY "T3 " A1
           STOP RUN.
