      *> reject-at: 2002 2014 2023
      *> ISO 13.18.57.3 SR6: 'If type-name-1 is described with the STRONG
      *>   phrase, the subject of the entry shall be specified only with level
      *>   number 1 or be subordinate to a type declaration that includes the
      *>   STRONG phrase.' Y TYPE S2 is, by 13.18.57.4 GR1, described with
      *>   TYPE S (STRONG), at level 05 in an ordinary group (kb/Work PB1301).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1301NLV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S TYPEDEF STRONG.
          05 SA PIC X.
          05 SB PIC X.
       01 S2 TYPEDEF TYPE S.
       01 G.
          05 Y TYPE S2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "UNREACHABLE"
           STOP RUN.
