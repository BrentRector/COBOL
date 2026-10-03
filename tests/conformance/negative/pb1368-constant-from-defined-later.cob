      *> reject-at: 2002 2014 2023
      *> 7.3.11.4 GR1 scopes a definition to the "text that follows a DEFINE directive", so a DEFINE written after
      *> the constant entry does not reach it; 13.10.3 SR8 then refuses FROM LATER (kb/Work PB1368).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1368LATER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT FROM LATER.
       >>DEFINE LATER AS 5
       PROCEDURE DIVISION.
           STOP RUN.
