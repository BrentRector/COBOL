      *>--- conformance-case
      *> id: da4_function_sending_operand
      *> rules: [SR-14.9.48.3-8, GR-14.9.48.4-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DA4FNSENDING.
      *> DA4 - A FUNCTION-IDENTIFIER IS ADMISSIBLE IN EVERY
      *> STRING/UNSTRING *SENDING* POSITION. 14.9.43.2 AND 14.9.48.2
      *> WRITE THOSE OPERANDS AS IDENTIFIER-N, AND 8.4.3.1.2 FORMAT 1
      *> MAKES function-identifier-1 A FORMAT OF AN IDENTIFIER.
      *>
      *> ⛔ NONE OF THESE EVEN PARSED BEFORE: "no viable alternative
      *> at input 'FUNCTION'". THE REJECTION HAPPENED A WHOLE STAGE
      *> EARLIER THAN THE STRING-CONTEXT RENDERER DA2 FIXED, WHICH IS
      *> WHY DA2 COULD NOT REACH IT.
      *>
      *> ⚠ THE *INTO* PHRASES ARE DELIBERATELY NOT OPENED: 8.4.3.2.3
      *> SR1 - "A function-identifier shall not be specified as a
      *> receiving operand." ALL FOUR CHANGED POSITIONS ARE SENDING.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C PIC X VALUE "A".
       01 S PIC X(11) VALUE "ab cd ef gh".
       01 W PIC X(5) VALUE "hello".
       01 A PIC X(12) VALUE SPACES.
       01 B PIC X(12) VALUE SPACES.
       01 P1 PIC X(4) VALUE SPACES.
       01 P2 PIC X(4) VALUE SPACES.
       01 Q1 PIC X(4) VALUE SPACES.
       01 Q2 PIC X(4) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
      *> STRING identifier-1 - THE ORIGINAL DA4 REPRO. ORD("A") = 66
      *> BY 15.70.1 (ORDINAL POSITION, LOWEST IS 1).
           STRING FUNCTION ORD(C) DELIMITED BY SIZE INTO A.
           DISPLAY "S1=[" A "]".
      *> STRING identifier-2 - THE DELIMITED BY OPERAND. "hello" HAS
      *> NO UPPERCASE "L", SO THE WHOLE SENDER TRANSFERS.
           STRING W DELIMITED BY FUNCTION UPPER-CASE("L") INTO B.
           DISPLAY "S2=[" B "]".
      *> UNSTRING identifier-1 - THE SOURCE.
           UNSTRING FUNCTION UPPER-CASE(S) DELIMITED BY " "
               INTO P1 P2.
           DISPLAY "U1=[" P1 "][" P2 "]".
      *> UNSTRING identifier-2 - THE DELIMITED BY OPERAND.
           UNSTRING W DELIMITED BY FUNCTION LOWER-CASE("L")
               INTO Q1 Q2.
           DISPLAY "U2=[" Q1 "][" Q2 "]".
           STOP RUN.
