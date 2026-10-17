      *>--- conformance-case
      *> id: l1c21-property-no-get-and-set
      *> rules: [FMT-13.18.42.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.42.2 format — WITH NO names exactly ONE of GET / SET
      *> General format: PROPERTY [ WITH NO { GET | SET } ] [ IS FINAL ]
      *>   cite.py --check 13.18.42.2 "PROPERTY" -> OK (General format)
      *> The { GET | SET } braces carry no choice indicators, so "one of
      *> the alternatives contained within the braces shall be
      *> explicitly specified" (§5.2.6.3; cite.py --check 5.2.6.3 -> OK
      *> §5.2.6.3 (Braces)) — ONE, not both; choice indicators, which
      *> would permit several, are absent (§5.2.6.4). "WITH NO GET SET"
      *> is therefore not a legal phrase. The rest of the program is
      *> valid (a factory property in a class that is never used), so
      *> the only reason to reject is the general format. The violation
      *> is pure syntax, so the diagnostic is the parser's (see .err).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C21R.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "R"
           STOP RUN.
       END PROGRAM L1C21R.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1C21U.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PX PIC 9(3) VALUE 1 PROPERTY WITH NO GET SET.
       PROCEDURE DIVISION.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS L1C21U.
