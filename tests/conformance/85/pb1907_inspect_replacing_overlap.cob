      *> kb/Work PB1907, docs/CONFORMANCE.md D-INS3. ISO 14.9.22.4 GR18
      *> and Annex A.2 item 21 e): an INSPECT REPLACING whose pattern,
      *> replacement or BEFORE/AFTER identifier occupies the same
      *> storage as identifier-1. The standard leaves the result
      *> undefined:
      *>   cite.py --check 14.9.22.4 "If identifier-3, identifier-4, or
      *>   identifier-5 occupies the same storage area as identifier-1,
      *>   the result of the execution of this statement is undefined,
      *>   even if they are defined by the same data description entry."
      *>   -> OK 14.9.22.4 18)
      *>   cite.py --check A.2 "occupies the same storage area as the
      *>   INSPECT identifier" -> OK A.2 21) e)
      *>   cite.py --check 4.4 "A COBOL run unit that allows these
      *>   situations to happen is a conforming run unit" -> OK 4.4 2)
      *> This golden pins a DOCUMENTED IMPLEMENTOR CHOICE (GnuCOBOL 3.2,
      *> measured): identifier-1's image and every operand value are
      *> taken when the REPLACING pass starts (GR6 item identification;
      *> GR9 fixes each BEFORE/AFTER region before the first cycle);
      *> the comparison cycle matches that image, and the one store at
      *> the end of the statement is the only write. An operand
      *> therefore never observes a replacement made by its own
      *> statement.
      *> EXPECTED (X1 = "QS", X2 = "AB", X3 = "AQQA", R4 = "ABAB" with
      *> R4P its characters 3-4, X9 = "AQAB"):
      *>   E1  ALL "Q" BY "R", ALL "S" BY X1(1:1): the replacement
      *>       X1(1:1) is "Q" as read at the start      [RQ]
      *>   E2  ALL X2 BY "BA": the pattern is "AB"      [BA]
      *>   E3  ALL "A" BY "Z", CHARACTERS BY "*" BEFORE X3(1:1): the
      *>       delimiter is "A" as read at the start, its first
      *>       occurrence is position 1, so CHARACTERS has an empty
      *>       region; ALL "A" is unrestricted         [ZQQZ]
      *>   E4  ALL R4P BY "XY": R4P is "AB", both occurrences are
      *>       replaced from the original image       [XYXY]
      *>   E5  ALL "A" BY "B", FIRST "B" BY X9(1:1): "A" -> "B" at 1
      *>       and 3 matching the original; FIRST "B" is the "B" at 4
      *>       of the original, replaced by X9(1:1) read at the start,
      *>       "A"                                    [BQBA]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907IR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1   PIC X(2) VALUE "QS".
       01 X2   PIC X(2) VALUE "AB".
       01 X3   PIC X(4) VALUE "AQQA".
       01 R4.
          05 R4A PIC X(4) VALUE "ABAB".
       01 R4R REDEFINES R4.
          05 FILLER PIC X(2).
          05 R4P PIC X(2).
       01 X9   PIC X(4) VALUE "AQAB".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT X1 REPLACING ALL "Q" BY "R" ALL "S" BY X1(1:1)
           DISPLAY "E1=[" X1 "]"
           INSPECT X2 REPLACING ALL X2 BY "BA"
           DISPLAY "E2=[" X2 "]"
           INSPECT X3 REPLACING ALL "A" BY "Z"
                      CHARACTERS BY "*" BEFORE X3(1:1)
           DISPLAY "E3=[" X3 "]"
           INSPECT R4 REPLACING ALL R4P BY "XY"
           DISPLAY "E4=[" R4 "]"
           INSPECT X9 REPLACING ALL "A" BY "B" FIRST "B" BY X9(1:1)
           DISPLAY "E5=[" X9 "]"
           STOP RUN.
