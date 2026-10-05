*> reject-at: 85 2002 2014 2023
      *> kb/Work PB543 - 14.9.43.3 SR7: "Identifier-4 shall be described as an elementary numeric integer data item".
      *> An index data item is class index (8.5.2.1 Table 2), and 13.18.60.3 SR10 lists no STRING statement among the
      *> contexts that may reference one. Its storage profile is category numeric at scale zero, so the pointer screen
      *> accepted it and the statement stored a character count into an index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB543STR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IX USAGE INDEX.
       01 S PIC X(10) VALUE "ABC".
       01 R PIC X(10).
       PROCEDURE DIVISION.
           STRING S DELIMITED BY SIZE INTO R WITH POINTER IX
           STOP RUN.
