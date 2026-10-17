*>--- conformance-case
*> id: pb73-move-numeric-function-to-alphanumeric
*> rules: [SR-14.9.25.3-10, GR-15.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.25.3 SR10 / Table 16: a Noninteger numeric sender shall not be moved to an alphanumeric receiver.
      *> A NUMERIC function (15.2 item 4) is the Noninteger row - 8.4.3.2.3 SR11's principle ("a numeric function shall
      *> not be specified where an integer operand is required, even though a particular reference ... might yield
      *> an integer value"). kb/Work PB73: admitted before the adjudication; --permissive keeps the admission with a
      *> warning (the item-92 text form).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB73NFX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X10 PIC X(10).
       PROCEDURE DIVISION.
           MOVE FUNCTION SQRT(2) TO X10.
           STOP RUN.
