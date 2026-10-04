      *> reject-at: 85 2002 2014 2023
      *> ISO 14.9.48.3 SR1 (kb/Work PB1181): "Literal-1 and literal-2 shall be literals of the category alphanumeric or
      *> national and shall be neither a figurative constant that begins with the word ALL nor a zero-length
      *> literal." A numeric literal is none of alphanumeric or national. The program compiled and split the sender on
      *> the digit; it is refused at bind (COBOLNET1651). Every other form of the rule (zero-length, ALL ALL,
      *> boolean, a constant-name) is a row of StringUnstringOperandScreenTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1181NNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(12) VALUE "AB,CD5EF".
       01 A PIC X(4).
       01 B PIC X(4).
       01 C PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           UNSTRING S DELIMITED BY 5 INTO A B C
           STOP RUN.
