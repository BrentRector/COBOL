      *> kb/Work PB3268 (review round 2, R2-1) - the intermediate result item of ISO 14.9.25.4 GR1
      *> ("an intermediate result item provided by the implementor") must hand each receiver of a
      *> MOVE the value the MOVE of the same function to that one receiver stores (15.4.1: "the
      *> returned value is the same for all instances of a given function"). A binary32 receiver
      *> (FLOAT-BINARY-32, 13.18.60.4 GR14; FLOAT-SHORT, this compiler's binary32) takes ONE
      *> conversion of the exact value (14.6.8.3 rule 2: "in a manner consistent with the
      *> specifications of ISO/IEC 60559"), so an intermediate that first rounds the value to a
      *> binary64 and then narrows it rounds twice: X = 2**60 + 2**36 + 1 rounds to 2**60 + 2**36
      *> (a binary32 midpoint) in binary64, and the narrowing ties to even, giving 2**60.
      *> Expected: the correctly rounded binary32 of X is 2**60 + 2**37 = 1152921642045800448;
      *> 1.0000000596046447753906251 (a hair above the midpoint 1 + 2**-24) rounds to
      *> 1 + 2**-23 = 1.00000011920928955078125. A binary32 receiver beside a FLOAT-LONG one gets
      *> the same binary32, and the FLOAT-LONG one the binary64 of the value. Each line shows the
      *> several-receiver store, then the one-receiver store. BAD=0000 counts the shapes whose
      *> several-receiver and one-receiver stores differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268B32.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAD PIC 9(4) VALUE 0.
       01 X  PIC 9(19) VALUE 1152921573326323713.
       01 S1 USAGE FLOAT-BINARY-32.
       01 S2 USAGE FLOAT-SHORT.
       01 S3 USAGE FLOAT-BINARY-32.
       01 L1 USAGE FLOAT-LONG.
       01 L3 USAGE FLOAT-LONG.
       01 N1 PIC 9(19).
       01 N3 PIC 9(19).
       01 M1 PIC 9(19).
       01 M3 PIC 9(19).
       01 F1 PIC 9V9(23).
       01 F3 PIC 9V9(23).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION INTEGER(X) TO S1 S2
           MOVE FUNCTION INTEGER(X) TO S3
           PERFORM SHOW-INT
           DISPLAY "INT  " N1 " " N3
           MOVE FUNCTION INTEGER-PART(X) TO S1 S2
           MOVE FUNCTION INTEGER-PART(X) TO S3
           PERFORM SHOW-INT
           DISPLAY "IPRT " N1 " " N3
           MOVE FUNCTION SUM(X) TO S1 S2
           MOVE FUNCTION SUM(X) TO S3
           PERFORM SHOW-INT
           DISPLAY "SUM  " N1 " " N3
           MOVE FUNCTION NUMVAL("1152921573326323713") TO S1 S2
           MOVE FUNCTION NUMVAL("1152921573326323713") TO S3
           PERFORM SHOW-INT
           DISPLAY "NUMV " N1 " " N3
           MOVE FUNCTION NUMVAL("1.0000000596046447753906251")
             TO S1 S2
           MOVE FUNCTION NUMVAL("1.0000000596046447753906251") TO S3
           MOVE S1 TO F1
           MOVE S3 TO F3
           IF S1 NOT = S3 OR S2 NOT = S3 ADD 1 TO BAD END-IF
           DISPLAY "MID  " F1 " " F3
           MOVE FUNCTION INTEGER(X) TO S1 L1
           MOVE FUNCTION INTEGER(X) TO S3
           MOVE FUNCTION INTEGER(X) TO L3
           PERFORM SHOW-MIX
           DISPLAY "IMIX " N1 " " M1 " " N3 " " M3
           MOVE FUNCTION NUMVAL("1152921573326323713") TO S1 L1
           MOVE FUNCTION NUMVAL("1152921573326323713") TO S3
           MOVE FUNCTION NUMVAL("1152921573326323713") TO L3
           PERFORM SHOW-MIX
           DISPLAY "NMIX " N1 " " M1 " " N3 " " M3
           DISPLAY "BAD=" BAD
           STOP RUN.
       SHOW-INT.
           MOVE S1 TO N1
           MOVE S3 TO N3
           IF S1 NOT = S3 OR S2 NOT = S3 ADD 1 TO BAD END-IF.
       SHOW-MIX.
           MOVE S1 TO N1
           MOVE L1 TO M1
           MOVE S3 TO N3
           MOVE L3 TO M3
           IF S1 NOT = S3 OR L1 NOT = L3 ADD 1 TO BAD END-IF.
