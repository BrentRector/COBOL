      *> reject-at: 2002 2014 2023
      *> kb/Work PB3287 - ISO 8.4.3.11.3 SR1: identifier-1 "shall reference a data item defined in the file section,
      *> working-storage section, local-storage section, or linkage section".  C1 is a constant-name (13.10), which
      *> substitutes a literal and is no data item, so ADDRESS OF C1 is refused.  The refusal names what C1 IS: a
      *> constant-name, never "not defined" - the reporter of an unidentified reference (ReportUnidentified) had an arm
      *> for an alphabet-name, an index-name and a condition-name but none for a constant-name, so a constant-name
      *> handed to a position that needs a data item was told its own declaration did not exist.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3287N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C1 CONSTANT 5.
       01  WS-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET WS-P TO ADDRESS OF C1.
           STOP RUN.
