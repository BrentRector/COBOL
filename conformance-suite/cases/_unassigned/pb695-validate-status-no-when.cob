*>--- conformance-case
*> id: pb695-validate-status-no-when
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OMITTED OPTIONAL WORD MUST REACH THE DOCUMENTED REFUSAL, NOT A PARSE ERROR (PB695).
      *> ISO 13.18.62.2 prints `{ VALIDATE-STATUS | VAL-STATUS } IS { identifier-1 | literal-1 } WHEN
      *> { ERROR | NO ERROR } [ ON { ... } ] FOR { identifier-2 } ...`. Measured on printed page 543 /
      *> folio 513: ERROR carries an underline rectangle (93.3% cover), NO one at 83.5% and ON one at
      *> 83.5%, while WHEN's box 312.75-343.36 and IS's box 214.50-223.27 have NO rule in their bands.
      *> 8.3.2.4.3 therefore makes `VALIDATE-STATUS IS "E" ERROR FOR W` a conforming spelling.
      *> WHY THIS IS A NEGATIVE CASE. The VALIDATE-STATUS clause is a DECLINED obsolete feature of the
      *> VALIDATE facility, and 4.2.7 makes non-support conformant only when it is DIAGNOSED -
      *> COBOLNET1708 is that diagnosis. The WHEN-less spelling must draw the SAME refusal, and the
      *> code is what proves the clause was recognized rather than mis-parsed (COBOL0001).
      *> 8.9 does not reserve VALIDATE-STATUS at COBOL-85, where the clause does not exist at all, so
      *> reject-at names 2002 and later - the editions where the refusal is a DIAGNOSED non-support.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695VSNOWHEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       01 VS1 PIC X(4)
          VALIDATE-STATUS IS "E" ERROR FOR W.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNREACHABLE"
           STOP RUN.
