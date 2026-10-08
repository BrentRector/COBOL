      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.11.3 SR5 — ADDRESS OF identifier-1 as the receiving
      *> operand of SET Format 10 (data-pointer-arithmetic).
      *> SR5: "This identifier format shall not be specified as a
      *>   receiving operand."
      *>   cite.py --check 8.4.3.11.3 "This identifier format shall not
      *>     be specified as a receiving operand" -> OK §8.4.3.11.3 5)
      *> Why SR5 is the ONLY rule broken: ADDRESS OF W is "a unique data
      *> item of class pointer and category data-pointer" (§8.4.3.11.4
      *> GR1, cite.py OK), and SET Format 10's receiving identifier-9
      *> "shall be of category data-pointer" (§14.9.39.3 SR23, cite.py
      *> OK) - so the category fits and only the receiving position is
      *> forbidden. W is BASED, so ADDRESS OF W satisfies SR1 (working-
      *> storage) and every other §8.4.3.11.3 rule. Measured control:
      *> the same program with SET P UP BY 1 (P a data-pointer item)
      *> compiles at 2002 and 2023. Expected: REJECTED at the UP of the
      *> SET statement, at every edition with data-address-identifiers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GN1AB1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 V PIC X(4) VALUE "ABCD".
       01 W PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF V
           SET ADDRESS OF W TO P
           SET ADDRESS OF W UP BY 1
           DISPLAY W
           GOBACK.
