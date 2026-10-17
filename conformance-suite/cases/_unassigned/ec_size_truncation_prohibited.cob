*>--- conformance-case
*> id: ec_size_truncation_prohibited
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> EC-SIZE-TRUNCATION (ISO 14.7.5 / VCR row 53): ROUNDED MODE IS
      *> PROHIBITED raises EC-SIZE-TRUNCATION (fatal) when the scaled result
      *> is inexact; the receiver is left UNCHANGED. Observed via ON SIZE
      *> ERROR + FUNCTION EXCEPTION-STATUS under >>TURN EC-SIZE CHECKING ON.
      >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-SIZE-TRUNC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9V9 VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-X ROUNDED MODE IS PROHIBITED = 0.35
               ON SIZE ERROR
                   DISPLAY "EC=" FUNCTION EXCEPTION-STATUS
           END-COMPUTE.
           DISPLAY "X=" WS-X.
           STOP RUN.
