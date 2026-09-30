      *> PB1663 - ISO 14.9.22.3 SR3: "Each literal shall be an
      *>   alphanumeric, boolean, or national literal." The hexadecimal-
      *>   alphanumeric literal (8.3.3.2.2 Format 2) IS an alphanumeric
      *>   literal (8.3.3.2.1: "Alphanumeric literals are of the class and
      *>   category alphanumeric"), so INSPECT accepts it.
      *> cite.py --check 14.9.22.3 "Each literal shall be an alphanumeric,
      *>   boolean, or national literal" -> OK  14.9.22.3 3)
      *> Derivation: X"41" is "A" and X"42" is "B". W = ABAB.
      *>   TALLYING FOR ALL X"42" counts two; REPLACING ALL X"41" BY "<"
      *>   gives <B<B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1663.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABAB".
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           INSPECT W TALLYING N FOR ALL X"42"
           DISPLAY "N=" N
           INSPECT W REPLACING ALL X"41" BY "<"
           DISPLAY "W=" W
           STOP RUN.
