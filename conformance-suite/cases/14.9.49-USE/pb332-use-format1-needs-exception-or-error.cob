*>--- conformance-case
*> id: pb332-use-format1-needs-exception-or-error
*> rules: [FMT-14.9.49.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.49.2 Format 1 prints `{ EXCEPTION | ERROR }` inside BRACES, both words UNDERLINED.
      *> 5.2.6.3: braces mean "one of the options contained within the braces shall be selected";
      *> 5.2.2 makes each underlined word required. PB332 made AFTER, STANDARD, PROCEDURE and ON
      *> omittable because none of them is underlined - it did NOT make the braced choice optional,
      *> and a Format 1 USE that names neither EXCEPTION nor ERROR selects no format at all.
      *> 4.2.2 requires a mechanism "to indicate violations of the general formats and the explicit
      *> syntax rules of standard COBOL"; strict mode is where this compiler provides it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB332N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb332n2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H-SECT SECTION.
           USE PROCEDURE ON F1.
       H-PARA.
           DISPLAY "HANDLER=" ST1.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           CLOSE F1
           STOP RUN.
