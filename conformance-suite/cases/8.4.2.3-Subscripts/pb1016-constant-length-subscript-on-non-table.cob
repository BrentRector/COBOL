*>--- conformance-case
*> id: pb1016-constant-length-subscript-on-non-table
*> rules: [SR-8.4.2.3.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1016 - ISO 8.4.2.3.3 SR2 over a constant entry's LENGTH OF operand (13.10, 2002+).
      *> SR2: "If a subscript is specified, the data description entry describing qualified-data-name-1 ...
      *> shall contain an OCCURS clause or shall be subordinate to a data description entry that contains an
      *> OCCURS clause." PLAIN has neither, so LENGTH OF PLAIN (1) is not a legal operand. 13.10.3 SR3 ("All
      *> subscripts of data-name-1 and data-name-2 shall be literals") is satisfied - it constrains the FORM of
      *> a subscript, not whether one may be written. Before PB1016 this compiled and KL was 7.
      *> Rejected by the ONE procedure-division screen, COBOLNET2096. (85 has no constant entry at all.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1016NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PLAIN PIC X(7).
       01 KL CONSTANT AS LENGTH OF PLAIN (1).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
