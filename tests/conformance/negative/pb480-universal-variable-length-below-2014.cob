      *> reject-at: 85 2002
      *> kb/Work PB480 - the NEGATIVE twin of tests/conformance/2014/pb480_universal_variable_length. A
      *> dynamic-capacity table (OCCURS DYNAMIC, ISO 13.18.38 Format 4 / 8.5.1.9) is a COBOL-2014 introduction, so
      *> the variable-length group that crosses the universal INVOKE is rejected below 2014 (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480VNG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U USAGE OBJECT REFERENCE.
       01  VG.
           05  VH PIC X(2).
           05  VT PIC X(2) OCCURS DYNAMIC CAPACITY IN VCAP FROM 1 TO 5.
       PROCEDURE DIVISION.
           INVOKE U "TF" USING VG
           STOP RUN.
