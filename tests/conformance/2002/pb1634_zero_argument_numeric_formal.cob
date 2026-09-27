      *> kb/Work PB1634 - a figurative ZERO argument into a NUMERIC formal
      *> is the numeric value zero. ISO 14.8.2.3.3 2) a): into a numeric
      *> formal the conformance rules are a COMPUTE's with the argument as
      *> the sending operand, and a COMPUTE reads ZERO as zero (8.8.1.1;
      *> 8.3.3.6.3 SR1 a)). Before the fix, ZERO crossed as the one-
      *> character fill "0" and a BINARY formal received its character
      *> code: 0048, BY VALUE, BY CONTENT and keyword-less alike.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1634G.
       PROCEDURE DIVISION.
           CALL "PB1634V" AS NESTED USING BY VALUE ZERO
           CALL "PB1634V" AS NESTED USING ZEROS
           CALL "PB1634C" AS NESTED USING BY CONTENT ZEROES
           CALL "PB1634D" AS NESTED USING BY CONTENT ZERO
           CALL "PB1634V" AS NESTED USING BY VALUE 0
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1634V.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(4) BINARY.
       PROCEDURE DIVISION USING BY VALUE N.
           DISPLAY "VALUE " N.
           GOBACK.
       END PROGRAM PB1634V.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1634C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(4) BINARY.
       PROCEDURE DIVISION USING N.
           DISPLAY "CONTENT " N.
           GOBACK.
       END PROGRAM PB1634C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1634D.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(3)V99 PACKED-DECIMAL.
       PROCEDURE DIVISION USING N.
           DISPLAY "PACKED " N.
           GOBACK.
       END PROGRAM PB1634D.
       END PROGRAM PB1634G.
