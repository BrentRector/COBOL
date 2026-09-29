      *> PB1078 - ISO 12.4.5.2 SR4: literal-1 of the ASSIGN clause is an
      *>   alphanumeric literal. The hexadecimal-alphanumeric literal
      *>   (8.3.3.2.2 Format 2) and a concatenation expression of
      *>   alphanumeric operands (8.8.3.3 GR3, "may be used anywhere a
      *>   literal of that class may be used") are alphanumeric literals.
      *> cite.py --check 12.4.5.2 "Literal-1 shall be an alphanumeric
      *>   literal and shall be neither a figurative constant nor a
      *>   zero-length literal" -> OK  12.4.5.2 4)
      *> cite.py --check 8.8.3.3 "A concatenation expression shall be
      *>   equivalent to a literal of the same class and value" -> OK
      *>   8.8.3.3 GR3
      *> Derivation: X"706231303738612E646174" is "pb1078a.dat" and
      *>   "pb1078" & "b.dat" is "pb1078b.dat", so records written
      *>   through F1 and F2 are read back through G1 and G2, which name
      *>   the same files by plain literals: ABC then XYZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1078.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO X"706231303738612E646174".
           SELECT F2 ASSIGN TO "pb1078" & "b.dat".
           SELECT G1 ASSIGN TO "pb1078a.dat".
           SELECT G2 ASSIGN TO "pb1078b.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(3).
       FD F2.
       01 R2 PIC X(3).
       FD G1.
       01 Q1 PIC X(3).
       FD G2.
       01 Q2 PIC X(3).
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           WRITE R1 FROM "ABC"
           CLOSE F1
           OPEN OUTPUT F2
           WRITE R2 FROM "XYZ"
           CLOSE F2
           OPEN INPUT G1
           READ G1
           DISPLAY Q1
           CLOSE G1
           OPEN INPUT G2
           READ G2
           DISPLAY Q2
           CLOSE G2
           STOP RUN.
