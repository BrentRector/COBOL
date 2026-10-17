*>--- conformance-case
*> id: pb165_dynamic_length_boundary
*> rules: [GR-14.9.4.4-3, GR-14.9.4.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB165 - a DYNAMIC LENGTH formal parameter across the activation boundary, the THIRD length
      *> regime (13.18.19: "the length of the data item defined by the entry can vary. The minimum length of
      *> the data item is zero"; 13.18.19.3 SR1 pins its PICTURE at exactly ONE 'X' or 'N' symbol).
      *>   REF  14.2.3 GR8 - "the activated runtime element operates as if the formal parameter occupies the
      *>        same storage area as the argument": the callee sees the argument's CURRENT 7 characters, and
      *>        its own MOVE of a 2-character value leaves the caller's item at length 2 (no padding -
      *>        8.5.1.10.4 / 13.18.19.4 GR1).
      *>   CON  14.2.3 GR9's SECOND regime - for a NESTED call the allocated record is "a dynamic-length
      *>        elementary item of the same category and described with the same dynamic-length-structure-name
      *>        as the formal parameter", with the argument as the sending operand of a MOVE: the 7-character
      *>        argument arrives whole, and the callee's store does NOT reach the caller.
      *> Measured before PB165: BOTH printed length 1 and a single character, because the formal-carrier width
      *> dispatch had an ANY LENGTH arm and a fixed arm and no DYNAMIC LENGTH arm - so it took the fixed arm at
      *> the item's PICTURE length, which SR1 guarantees is 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCDEFG" TO D
           CALL "PB165DR" AS NESTED USING D
           DISPLAY "REF-AFTER=[" D "] LEN=" FUNCTION LENGTH(D)
           MOVE "ABCDEFG" TO D
           CALL "PB165DC" AS NESTED USING BY CONTENT D
           DISPLAY "CON-AFTER=[" D "] LEN=" FUNCTION LENGTH(D)
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165DR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION USING LD.
       M1.
           DISPLAY "REF-IN=[" LD "] LEN=" FUNCTION LENGTH(LD)
           MOVE "ZY" TO LD
           GOBACK.
       END PROGRAM PB165DR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB165DC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION USING LE.
       M2.
           DISPLAY "CON-IN=[" LE "] LEN=" FUNCTION LENGTH(LE)
           MOVE "QP" TO LE
           GOBACK.
       END PROGRAM PB165DC.
       END PROGRAM PB165D.
