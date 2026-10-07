      *> reject-at: 85 2002
      *> kb/Work PB895 - the zero-length literal (ISO §8.3.3.1: "If the opening and closing delimiters are
      *> contiguous, the length of the literal is zero, and it is known as a zero-length literal") is a
      *> COBOL-2014 literal format. VCR row 7.30 states the derived edge (GnuCOBOL's dialect files:
      *> zero-length-literals unconformable at cobol85 and cobol2002, ok at cobol2014). The program is legal
      *> at 2014 and later in every other respect.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB895N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
           MOVE "" TO X
           DISPLAY X
           STOP RUN.
