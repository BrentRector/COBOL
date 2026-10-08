      *> reject-at: 85
      *> The REJECT half of conformance:2002/pb854_write_eop_exception_
      *> conditions. EC-I-O-EOP is the exception-name ISO §14.9.51.4
      *> GR27 a) sets to exist for a GR26 b) end-of-page condition; it
      *> belongs to the exception-condition facility ISO/IEC 1989:2002
      *> introduced, so naming it in a declarative below COBOL-2002 is
      *> refused with the introduction-band diagnostic COBOLNET0878. No
      *> >>TURN directive, for the reason pb368-flow-use-name-below-2002
      *> gives: the directive's own gate (COBOLNET0900) would answer
      *> first and the case would pin the directive instead of the NAME.
      *> At COBOL-85 the end-of-page condition still reaches the program
      *> through the AT END-OF-PAGE phrase, which is COBOL-85 syntax.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB854N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb854n1.prt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF LINAGE IS 3 LINES WITH FOOTING AT 2.
       01  PREC     PIC X(8).
       WORKING-STORAGE SECTION.
       01  W-LINE   PIC X(4) VALUE "LINE".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DEOP SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-EOP.
       DEOP-P.
           DISPLAY "EOP-HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRTF.
           WRITE PREC FROM W-LINE.
           CLOSE PRTF.
           STOP RUN.
