      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR8: "Identifier-2 shall reference an alphanumeric or
      *> national data item." MN is PIC NN0NN, category NATIONAL-EDITED
      *> (§8.5.2.11), a category of its own in §8.5.2.1 Table 2, so it is not
      *> a national data item and cannot hold the method name. The screen read
      *> the category alone and accepted it (kb/Work PB850).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB850INE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 MN PIC NN0NN.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE U MN.
           STOP RUN.
       END PROGRAM PB850INE.
