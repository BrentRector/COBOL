      *> reject-at: 85 2002 2014 2023
      *> ISO 8.8.4.2.1 defines a numeric operand against an alphanumeric one only as item 6, "Two operands where
      *> one is a numeric integer and the other is class alphanumeric or national", and 8.8.4.2.5: "The numeric
      *> integer operand shall be an integer literal or an integer numeric data item of usage display or
      *> national." NB is USAGE COMP, so the pair has no defined comparison (COBOLNET2532); before kb/Work PB1468
      *> it compiled clean and ran as a character comparison of NB's text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1468-NUM-NOT-INT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NB PIC 9(4) COMP VALUE 12.
       01 XA PIC X(4) VALUE "0012".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF NB = XA DISPLAY "Y" ELSE DISPLAY "N" END-IF
           STOP RUN.
