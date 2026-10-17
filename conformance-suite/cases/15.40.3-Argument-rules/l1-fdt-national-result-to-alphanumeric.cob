*>--- conformance-case
*> id: l1-fdt-national-result-to-alphanumeric
*> rules: [AR-15.40.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r1 admits "a national or alphanumeric literal" for
      *> argument-1, and §15.40.1's type table makes the FUNCTION's type
      *> follow it: "National | National". So a NATIONAL format literal makes
      *> FORMATTED-DATETIME a national function, and §14.9.25.3 rule 10's
      *> Table 16 gives the National sending row a "No" under an
      *> alphanumeric receiver.
      *>
      *> THIS IS THE LINE THAT MAKES r1's NATIONAL LEG OBSERVABLE. Table 16's
      *> Alphanumeric sending row gives National "Yes", so the positive
      *> golden's `MOVE ... (N"...") TO a PIC N item` would compile even if
      *> the result were mis-typed alphanumeric - the accept side cannot tell
      *> the two apart, and only this rejection can. Its FORMATTED-DATE twin
      *> is pb15-formatted-date-national-result-to-an; §15.40.1 is a separate
      *> table from §15.39.1 and had no witness of its own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDTNEG6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-DATETIME(N"YYYYMMDDThhmmss"
               143951 45296) TO R
           STOP RUN.
