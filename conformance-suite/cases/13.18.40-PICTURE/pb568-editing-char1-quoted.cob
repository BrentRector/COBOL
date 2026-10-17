*>--- conformance-case
*> id: pb568-editing-char1-quoted
*> rules: [SR-13.18.40.3-10, SR-13.18.40.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.2 Format 1 writes character-1 BARE: `EDITING character-1 { IS literal-1 | FOR {
      *> NEGATIVE IS literal-2 | POSITIVE IS literal-3 } }`.  character-1 carries no quotation marks and is
      *> named character-1, exactly as character-string-1 is, while literal-1/-2/-3 are named as literals;
      *> 13.18.40.3 SR8 types it as "any basic letter in the COBOL character set", 13.18.40.4 GR14 and
      *> 13.18.40.5 rule 3 make it a PICTURE SYMBOL occurring in character-string-1, and SR9 - the rule that
      *> types the phrase's literals - enumerates only literal-1, literal-2 and literal-3.
      *> The grammar used to REQUIRE the quoted spelling, so every conforming EDITING phrase was a parse
      *> error and SR8/SR10/SR11 were reachable only through a spelling the standard does not define
      *> (kb/Work PB568).  The quoted form still parses so that it can be refused BY NAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB568EDITINGCHAR1QUOTED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1 PIC 99T99 EDITING "T" IS ":".
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
