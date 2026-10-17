*>--- conformance-case
*> id: pb681-table-element-not-subscripted
*> rules: [SR-8.4.2.3.3-5]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §8.4.2.3.3 SR5: "Each table element reference shall be subscripted except when such reference
*> appears" - as a SEARCH subject, in a REDEFINES clause, in an OCCURS KEY IS phrase, in a SORT key or table
*> subject, in a screen FROM/TO/USING clause, or as a SUM addend. A MOVE sending operand is none of them. The
*> reference resolver returned an UNREPORTED null for the omitted list, so `MOVE E TO B` over a two-dimensional
*> table element compiled clean and aborted the run unit at the MOVE; the condition-name arm (a level 88 under
*> a table element, SET/IF with no subscript) was the same null through the second resolver entry. The rule is
*> written the same way at every edition. COBOLNET2270 (kb/Work PB681).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB681NTES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B PIC X(8).
       01  T.
           05 R OCCURS 3.
              10 E PIC X OCCURS 3.
                 88 E-Y VALUE "Y".
       PROCEDURE DIVISION.
           MOVE E TO B.
           SET E-Y TO TRUE.
           DISPLAY B.
           STOP RUN.
