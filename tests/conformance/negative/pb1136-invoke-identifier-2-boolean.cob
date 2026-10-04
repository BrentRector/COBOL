      *> reject-at: 2002 2014 2023
      *> ISO §14.9.23.3 SR8: "Identifier-2 shall reference an alphanumeric or
      *> national data item."  MB is PIC 1(8), class boolean, so it cannot
      *> hold the method name even through the universal receiver U that
      *> SR7 requires.  kb/Work PB1136 (the national half of SR8 is the
      *> positive golden pb1136_invoke_selector_forms).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1136N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 MB PIC 1(8).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE U MB.
           STOP RUN.
       END PROGRAM PB1136N3.
