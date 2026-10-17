      *>--- conformance-case
      *> id: phrase_order_overflow
      *> rules: [GR-14.9.48.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 5.2.6.4 — STRING's and UNSTRING's ON OVERFLOW / NOT ON OVERFLOW pair is enclosed in CHOICE
      *> INDICATORS in the printed general format, so BOTH may be written, each at most once, IN ANY ORDER.
      *> This golden pins the REVERSED order (NOT-then-ON) and proves each phrase keeps its own role.
      *> Regression guard for the defect fixed 2026-07-19 (DEVLOG 927): stringOnOverflow / unstringOnOverflow
      *> admitted only ON-then-NOT, so both statements below were rejected with COBOL0001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PHORDOVFL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC-A   PIC X(5)  VALUE "HELLO".
       01 SRC-B   PIC X(5)  VALUE "WORLD".
       01 ROOMY   PIC X(20).
       01 TIGHT   PIC X(4).
       01 PART-1  PIC X(4).
       01 PART-2  PIC X(4).
       01 SRC-C   PIC X(9)  VALUE "AB,CD,EFG".
       PROCEDURE DIVISION.
       MAIN.
      *> fits -> NOT ON OVERFLOW branch
           MOVE SPACES TO ROOMY.
           STRING SRC-A DELIMITED BY SIZE
                  SRC-B DELIMITED BY SIZE
               INTO ROOMY
               NOT ON OVERFLOW DISPLAY "STR-NOT"
               ON OVERFLOW DISPLAY "STR-ON"
           END-STRING.
           DISPLAY ROOMY.

      *> does not fit -> ON OVERFLOW branch
           MOVE SPACES TO TIGHT.
           STRING SRC-A DELIMITED BY SIZE
                  SRC-B DELIMITED BY SIZE
               INTO TIGHT
               NOT ON OVERFLOW DISPLAY "STR2-NOT"
               ON OVERFLOW DISPLAY "STR2-ON"
           END-STRING.

      *> "AB,CD,EFG" yields three fields into TWO receivers, so all receiving areas are acted upon while
      *> "EFG" remains unexamined -> ISO 14.9.48 GR15(b) says the OVERFLOW condition exists -> ON branch.
      *> (Pinned deliberately: the reversed phrase order must not change WHICH branch the condition selects.)
           MOVE SPACES TO PART-1.
           MOVE SPACES TO PART-2.
           UNSTRING SRC-C DELIMITED BY ","
               INTO PART-1 PART-2
               NOT ON OVERFLOW DISPLAY "UNS-NOT"
               ON OVERFLOW DISPLAY "UNS-ON"
           END-UNSTRING.
           DISPLAY PART-1.
           DISPLAY PART-2.

           STOP RUN.
