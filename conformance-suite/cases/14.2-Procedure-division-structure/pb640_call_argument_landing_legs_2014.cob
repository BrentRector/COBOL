*>--- conformance-case
*> id: pb640_call_argument_landing_legs_2014
*> rules: [GR-14.2.3-9]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB640 - the SCOPE of the activating-side landing: ISO 14.2.3 GR9 splits the
      *> argument crossing three ways, and only ONE of them is the COMPUTE the landing performs.
      *>
      *>   "The argument is used as the sending operand and the allocated record as the receiving
      *>    operand in the following:
      *>      - if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase
      *>      - if the formal parameter is of class index, object, or pointer, a SET statement
      *>      - otherwise, a MOVE statement."
      *>
      *> and that whole list applies only to GR9's SECOND branch (a prototyped program, an AS
      *> NESTED CALL, a method, a function). GR9's FIRST branch - "a program for which there is no
      *> program-specifier in the REPOSITORY paragraph of the activating runtime element and there
      *> is no NESTED phrase specified on the CALL statement" - allocates a record "of the same
      *> length as the argument" and "that argument is moved to this allocated record without
      *> conversion". 14.8.2.3.3 draws the same partition for conformance (rule 1 vs rule 2).
      *>
      *> EXPECTED VALUES, DERIVED:
      *>  NOCONV - GR9 FIRST branch: PIC 9(4) = 1234 to a PIC 9(4) formal, no conversion => 1234.
      *>  IDXLEG - GR9's SET leg. An index data item is class INDEX (8.5.2.1 Table 2), NOT numeric,
      *>    so the crossing is a SET and "SET copies the occurrence number unchanged". IX is 3, so
      *>    the callee's TE (TI) is the third 2-character element of "ABCDEFGHIJ" => EF. This row
      *>    is the trap the landing's guard exists for: an index item's storage description carries
      *>    category Numeric with ZERO digit positions, so a numeric landing would store
      *>    value % 10**0 = 0 and this row would read the FIRST element's storage, not the third.
      *>  ANYLEN - GR9's ANY LENGTH bullet: the allocated record is "a data item of the same
      *>    category, usage, and length as the ARGUMENT", so n follows the argument => 7 then 3.
      *>  DYNLEN - (2014 and later only: the DYNAMIC LENGTH clause is a COBOL-2014 introduction,
      *>    so the 2002 copy of this golden carries every other leg and not this one.)
      *>    GR9's DYNAMIC LENGTH bullet: "a dynamic-length elementary item ... described
      *>    with the same dynamic-length-structure-name as the formal parameter" with the argument
      *>    moved into it => length 5, HELLO.
      *>  MOVELEG - GR9's "otherwise, a MOVE statement": X(7) "ABCDEFG" into an X(4) formal is
      *>    14.9.25.4 GR6 b)'s alphanumeric move, left-justified with truncation on the right
      *>    => ABCD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640LA14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N4 PIC 9(4) VALUE 1234.
       01 A7 PIC X(7) VALUE "ABCDEFG".
       01 A3 PIC XXX VALUE "XYZ".
       01 IX USAGE INDEX.
       01 MT.
          05 ME OCCURS 5 INDEXED BY MX PIC X.
       01 DL PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB640LF14" USING BY CONTENT N4
      *> The index data item is set FROM AN INDEX-NAME: ISO 14.9.39.3 SR3 forbids
      *> arithmetic-expression-1 when identifier-1 is of class index (kb/Work PB212).
           SET MX TO 3
           SET IX TO MX
           CALL "PB640LI14" AS NESTED USING BY CONTENT IX
           CALL "PB640LN14" AS NESTED USING BY CONTENT A7
           CALL "PB640LN14" AS NESTED USING BY CONTENT A3
           MOVE "HELLO" TO DL
           CALL "PB640LD14" AS NESTED USING BY CONTENT DL
           CALL "PB640LM14" AS NESTED USING BY CONTENT A7
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640LI14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 TE PIC X(2) OCCURS 5 INDEXED BY TI.
       01 WI PIC X(2).
       LINKAGE SECTION.
       01 LI USAGE INDEX.
       PROCEDURE DIVISION USING LI.
       M1.
           MOVE "ABCDEFGHIJ" TO T
           SET TI TO LI
           MOVE TE (TI) TO WI
           DISPLAY "IDXLEG=[" WI "]"
           GOBACK.
       END PROGRAM PB640LI14.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640LN14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC X ANY LENGTH.
       PROCEDURE DIVISION USING LN.
       M2.
           DISPLAY "ANYLEN=" FUNCTION LENGTH(LN) " V=" LN
           GOBACK.
       END PROGRAM PB640LN14.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640LD14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LD PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION USING LD.
       M3.
           DISPLAY "DYNLEN=" FUNCTION LENGTH(LD) " V=" LD
           GOBACK.
       END PROGRAM PB640LD14.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640LM14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LM PIC X(4).
       PROCEDURE DIVISION USING LM.
       M4.
           DISPLAY "MOVELEG=[" LM "]"
           GOBACK.
       END PROGRAM PB640LM14.
       END PROGRAM PB640LA14.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB640LF14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF PIC 9(4).
       PROCEDURE DIVISION USING LF.
       M5.
           DISPLAY "NOCONV=" LF
           GOBACK.
       END PROGRAM PB640LF14.
