*>--- conformance-case
*> id: pb148_display_forms
*> rules: [GR-14.9.11.4-1, GR-14.9.11.4-6, DOC-A.1-56]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB148DF.
      *> kb/Work PB148 - ISO 14.9.11.4 GR3 sentence 1: ALL literal-1
      *> displays a SINGLE occurrence of the literal (alphanumeric, hex
      *> and boolean spellings); GR1 + A.1 item 56: a signed non-DISPLAY
      *> usage renders a leading '-' when negative and NO sign character
      *> otherwise (a variable-width sending item - the determination in
      *> CONFORMANCE.md item 56); a PICTURE-less float renders the
      *> shortest-round-trip image; a boolean item its '0'/'1' characters.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SC PIC S9(4) COMP VALUE -123.
       01 SP2 PIC S9(4) COMP VALUE 123.
       01 FL USAGE COMP-2 VALUE 1.5.
       01 BL PIC 1(3) VALUE B"101".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "[" ALL "AB" "]"
           DISPLAY "[" ALL X"43" "]"
           DISPLAY "[" ALL B"101" "]"
           DISPLAY "[" SC "]" SP2 "]"
           DISPLAY "[" FL "]"
           DISPLAY "[" BL "]"
           STOP RUN.
