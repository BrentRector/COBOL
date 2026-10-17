*>--- conformance-case
*> id: pb764_format_keywords_not_names
*> rules: [SR-8.3.2.1-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB764 - a general-format KEYWORD with no lexer token is a
      *> USE of that word (ISO 5.2.2 "Keywords are reserved words or
      *> context-sensitive words"; 5.2.3 for optional words), never the
      *> user-defined word or system-name 8.3.2.1 1) governs ("Reserved words
      *> shall not be used as user-defined words or system-names"). Every word
      *> below that the grammar recognizes by TEXT is 8.9-RESERVED at 2014
      *> (reserved-words.json r2014): COBOL, NESTED, LOCALE, SYSTEM-DEFAULT,
      *> USER-DEFAULT. Each now parses as `formatWord`, which the 8.9 funnel
      *> never meets, so this program compiles with no COBOLNET0901 - where
      *> the funnel used to keep a ladder of per-slot exemptions instead.
      *>   11.9.7   ENTRY-CONVENTION IS COBOL      (the 2014 clause)
      *>   12.3.6.2 CHARACTER CLASSIFICATION IS SYSTEM-DEFAULT
      *>   12.3.7.2 LOCALE locale-name-1 IS literal-4 / ALPHABET IS LOCALE
      *>   14.9.39  SET ... TO LOCALE LC_ALL / SET LOCALE LC_ALL TO ...
      *>   14.9.4.2 CALL literal-1 AS NESTED (Format 2)
      *> Expected output, from the statements alone: the nested program
      *> displays the BY CONTENT argument (GOT 7), then the outer one its
      *> closing line. No locale-dependent value is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB764FMTKW.
       OPTIONS.
           ENTRY-CONVENTION IS COBOL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CHARACTER CLASSIFICATION IS SYSTEM-DEFAULT.
       SPECIAL-NAMES.
           ALPHABET CUR IS LOCALE EN
           LOCALE EN IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X(1) VALUE SPACE.
       01 SAVED USAGE POINTER.
       01 N PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           SET SAVED TO LOCALE LC_ALL
           SET LOCALE LC_ALL TO USER-DEFAULT
           SET LOCALE LC_ALL TO SAVED
           CALL "PB764IN" AS NESTED USING BY CONTENT N
           DISPLAY "FORMAT KEYWORDS OK"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB764IN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC 9.
       PROCEDURE DIVISION USING A.
       M.
           DISPLAY "GOT " A.
       END PROGRAM PB764IN.
       END PROGRAM PB764FMTKW.
