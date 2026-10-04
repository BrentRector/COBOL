      *> reject-at: 85
      *> kb/Work PB988 (row FMT-10.7.2) - the END FUNCTION alternative of
      *> ISO/IEC 1989:2023 section 10.7.2 ends a function definition, whose
      *> FUNCTION-ID paragraph (section 11.5) is a COBOL-2002 introduction;
      *> COBOL-85 has neither, so the gate names the edition.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB988FD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9.
       PROCEDURE DIVISION.
       FMAIN.
           EXIT PROGRAM.
       END FUNCTION PB988FD.
