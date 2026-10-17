      *>--- conformance-case
      *> id: compute-numeric-receiver
      *> rules: [SR-14.9.8.3-1, GR-14.9.8.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> The control of compute-alphanumeric-receiver: the same program
      *> with its receiver an elementary numeric item, as ISO 14.9.8.3
      *> SR 1 requires ("Identifier-1 shall reference either an
      *> elementary numeric item or an elementary numeric-edited item").
      *> GR 1: the statement determines the numeric value 3; GR 2
      *> stores it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSCNREC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R-NUM PIC 9.
       01 E-NUM PIC Z.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R-NUM = 1 + 2
           MOVE R-NUM TO E-NUM
           DISPLAY "R-NUM=" E-NUM
           STOP RUN.
