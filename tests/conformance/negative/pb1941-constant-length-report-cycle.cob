      *> reject-at: 2002 2014 2023
      *> kb/Work PB1941 - 13.10.3 SR4: "The length of data-name-1 or data-name-2 shall not be dependent, directly
      *> or indirectly, upon the value of constant-name-1". An elementary report item may be measured wherever the
      *> constant is evaluated (conformance/2002/pb1941_constant_length_report_ahead), but A's own PICTURE is
      *> X(KA), so the length KA measures depends on KA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1941RCY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "PB1941RCY.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPF REPORT IS RP.
       REPORT SECTION.
       RD  RP PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3.
       01  DL TYPE DE LINE PLUS 1.
           03  A COLUMN 1 PIC X(KA) VALUE "AAAA".
       01  KA CONSTANT AS LENGTH OF A.
       PROCEDURE DIVISION.
           DISPLAY KA
           STOP RUN.
