      *> reject-at: 2002 2014 2023
      *> kb/Work PB2727 - ISO 12.3.8.2 prints the REPOSITORY paragraph's separator period inside the
      *> bracket around its specifiers, once, after the last one; 5.2.1 binds the punctuation a general
      *> format prints.  A period after each specifier compiled clean; it is refused by name at the
      *> inner period.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PI INTRINSIC.
           FUNCTION E INTRINSIC.
       PROCEDURE DIVISION.
           STOP RUN.
