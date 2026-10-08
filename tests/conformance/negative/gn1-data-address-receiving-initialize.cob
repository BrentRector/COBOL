      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.11.3 SR5 — ADDRESS OF identifier-1 as the receiving
      *> operand of INITIALIZE.
      *> SR5: "This identifier format shall not be specified as a
      *>   receiving operand."
      *>   cite.py --check 8.4.3.11.3 "This identifier format shall not
      *>     be specified as a receiving operand" -> OK §8.4.3.11.3 5)
      *> Why SR5 is the ONLY rule broken: INITIALIZE accepts an
      *> identifier-1 "of class ... pointer" (§14.9.20.3 SR1, cite.py
      *> OK), ADDRESS OF W is of class pointer (§8.4.3.11.4 GR1,
      *> cite.py OK), and "The data item referenced by identifier-1 is
      *> the receiving operand" (§14.9.20.3 SR7, cite.py OK). W is
      *> BASED, so ADDRESS OF W satisfies every other §8.4.3.11.3 rule.
      *> Measured control: the same program with INITIALIZE P (P a
      *> data-pointer item) compiles and runs at 2002 and 2023.
      *> Expected: REJECTED, at every edition with
      *> data-address-identifiers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GN1AB2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 V PIC X(4) VALUE "ABCD".
       01 W PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF V
           SET ADDRESS OF W TO P
           INITIALIZE ADDRESS OF W
           DISPLAY W
           GOBACK.
