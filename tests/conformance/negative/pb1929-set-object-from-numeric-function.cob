      *> reject-at: 2002 2014 2023
      *> kb/Work PB1929 — the negative twin of 2002/pb1929_set_identifier_senders. A function-identifier is an
      *> identifier whose class and category are its RESULT's (§8.4.3.2.1, §15.2), so it satisfies a format's sending
      *> rule only when the result is of the class that rule names. §14.9.39.3 SR9 "Identifier-4 shall be an object
      *> reference": FUNCTION INTEGER(N) is class numeric, so SET B TO FUNCTION INTEGER(N) breaks SR9 (COBOLNET0867).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1929N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE.
       01 N PIC 9(4) VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           SET B TO FUNCTION INTEGER(N)
           STOP RUN.
       END PROGRAM PB1929N1.
