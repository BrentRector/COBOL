*>--- conformance-case
*> id: pb60_nested_configuration_inheritance
*> rules: [GR-11.9.4-1, GR-12.3.4-1, GR-12.3.6.4-1, GR-12.3.6.4-11, AR-15.67.3-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB60 (AR-15.67.3-5) - a container's CONFIGURATION SECTION and OPTIONS apply to its contained
      *> programs. 12.3.4 GR1: "The entries explicitly or implicitly specified in the configuration section
      *> of a source unit that contains other source units apply to each directly or indirectly contained
      *> source unit"; 12.3.3 SR1 forbids the containee a configuration section of its own, so it CANNOT
      *> restate them. 11.9.4 GR1: OPTIONS clauses "apply to the source element in which they are specified
      *> and to all source elements contained in that source element unless overridden by a clause in an
      *> OPTIONS paragraph in a contained source element". Before this landing only the REPOSITORY sets were
      *> inherited: inside a contained program of a DECIMAL-POINT IS COMMA unit NUMVAL("123,45") valued 0
      *> and NUMVAL("123.45") valued 123.45 - the exact inversion of 15.67.3 r5, undiagnosed.
      *> INNER (no OPTIONS, no configuration section):
      *>   NV:  15.67.3 r5 - under the inherited DECIMAL-POINT IS COMMA the comma is NUMVAL's decimal
      *>        separator: NUMVAL("123,45") = 123.45, edited through a comma-decimal PICTURE (GR14b).
      *>   NVP: "123.45" does not conform in comma mode (the period is not a NUMVAL character) - EC default 0.
      *>   ED:  the numeric literal 1234,5 (GR14a) edited by PIC ##.##9,99 with the inherited CURRENCY
      *>        SIGN "#": #1.234,50 (grouping '.', decimal ',').
      *>   CLASS: the inherited CLASS HEXDIG (8.8.4.1.4) - "1A9F" is all hex digits.
      *>   PCS: the inherited PROGRAM COLLATING SEQUENCE REV ("B" before "A", 12.3.6 GR11) makes "A" > "B".
      *>   SW:  the inherited switch mnemonic/condition (12.3.7 GR2/GR3) - SET SW1 TO ON, then SW1-ON.
      *>   ARITH: the inherited OPTIONS ARITHMETIC IS STANDARD-DECIMAL - 2 / 7 * 7 is exact 2,000000 in
      *>        SDIDI arithmetic (8.8.1.5) where native clips 2/7 to the working scale first (1,999999).
      *> INNER2 (its own OPTIONS. ARITHMETIC IS NATIVE.) - 11.9.4 GR1's override: 1,999999; the SPECIAL-NAMES
      *>   still inherit (comma decimal in the edited image).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60CFGOUTER.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF
           ALPHABET REV IS "B" "A"
           CLASS HEXDIG IS "0" THRU "9" "A" THRU "F"
           CURRENCY SIGN IS "#"
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-DUMMY PIC X.
       PROCEDURE DIVISION.
           CALL "PB60CFGINNER".
           CALL "PB60CFGINNER2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60CFGINNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC S9(9)V9(6).
       01 E    PIC -(9)9,9(6).
       01 ED   PIC ##.##9,99.
       01 X    PIC X(4) VALUE "1A9F".
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION NUMVAL("123,45").
           MOVE R TO E.
           DISPLAY "NV=" E.
           COMPUTE R = FUNCTION NUMVAL("123.45").
           MOVE R TO E.
           DISPLAY "NVP=" E.
           MOVE 1234,5 TO ED.
           DISPLAY "ED=" ED.
           IF X IS HEXDIG
               DISPLAY "CLASS=YES"
           ELSE
               DISPLAY "CLASS=NO"
           END-IF.
           IF "A" > "B"
               DISPLAY "PCS=REV"
           ELSE
               DISPLAY "PCS=NATIVE"
           END-IF.
           SET SW1 TO ON.
           IF SW1-ON
               DISPLAY "SW=ON"
           ELSE
               DISPLAY "SW=OFF"
           END-IF.
           COMPUTE R = 2 / 7 * 7.
           MOVE R TO E.
           DISPLAY "ARITH=" E.
           GOBACK.
       END PROGRAM PB60CFGINNER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB60CFGINNER2.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC S9(9)V9(6).
       01 E    PIC -(9)9,9(6).
       PROCEDURE DIVISION.
           COMPUTE R = 2 / 7 * 7.
           MOVE R TO E.
           DISPLAY "ARITH2=" E.
           GOBACK.
       END PROGRAM PB60CFGINNER2.
       END PROGRAM PB60CFGOUTER.
