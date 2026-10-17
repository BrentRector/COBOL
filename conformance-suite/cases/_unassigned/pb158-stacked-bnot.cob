      *>--- conformance-case
      *> id: pb158-stacked-bnot
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.8.2 Table 4, row B-NOT x column B-NOT = '-'. THE SAME
      *> CELL AS THE ARITHMETIC ONE, IN THE OTHER FORMATION TABLE, AND
      *> THE GRAMMAR CARRIED A COMMENT ASSERTING TABLE 4 WAS ENFORCED
      *> STRUCTURALLY WHILE 'booleanFactor : B_NOT booleanFactor'
      *> SELF-RECURSED - a green-looking claim holding the gap open.
      *> 8.8.4.11.3's Table 5 NOTE states the same restriction for
      *> conditions outright ("the pair 'NOT NOT' is not permissible")
      *> and THAT tier does exclude it structurally, which is the
      *> inconsistency this fixture closes. Boolean operators are a
      *> COBOL-2002 introduction, so 85 is not a rejecting edition for
      *> THIS code - it rejects there with the 0900 edition gate
      *> instead. kb/Work PB158.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB158N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BX PIC 1(4) USAGE BIT VALUE B"1010".
       01 BR PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE BR = B-NOT B-NOT BX.
           STOP RUN.
