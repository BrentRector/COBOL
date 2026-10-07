      *> reject-at: 85 2002
      *> kb/Work PB480 - the NEGATIVE twin of tests/conformance/2014/pb480_universal_variable_length_shapes. A
      *> dynamic-length elementary item (the DYNAMIC LENGTH clause, ISO 13.18.19 / 8.5.1.10) is a COBOL-2014
      *> introduction, so the variable-length group that crosses the universal INVOKE is rejected below 2014
      *> (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480SNG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U USAGE OBJECT REFERENCE.
       01  G1.
           05  H1 PIC X(2).
           05  D1 PIC X DYNAMIC LENGTH LIMIT IS 10.
           05  T1 PIC X(5).
       PROCEDURE DIVISION.
           INVOKE U "TA" USING G1
           STOP RUN.
