      *> reject-at: 85 2002 2014 2023
      *> ISO 14.9.43.3 SR8 (kb/Work PB1182): "Where identifier-1 or identifier-2 is an elementary numeric data item, it
      *> shall be described as an integer without the symbol 'P' in its picture character-string." N1 is PIC 9V9. The
      *> program compiled and sent "12"; it is refused at bind (COBOLNET1651). The other forms (99P, PP99, the
      *> DELIMITED BY identifier, the UNSTRING 99P / bit-group / strongly-typed-group receivers and a constant-name
      *> sender) are rows of StringUnstringOperandScreenTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1182NSR8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9V9 VALUE 1.2.
       01 OUT1 PIC X(10) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN.
           STRING N1 DELIMITED BY SIZE INTO OUT1
           STOP RUN.
