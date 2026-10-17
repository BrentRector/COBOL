*>--- conformance-case
*> id: pb303_as_phrase_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the AS externalized-name phrase is a COBOL-2002 introduction.
       *> The X3.23-1985 PROGRAM-ID paragraph is `PROGRAM-ID. program-name [IS [COMMON]
       *> [INITIAL] PROGRAM].` - no AS phrase - and AS is a user-definable word at 85
       *> (constructs.json user-word-as-2002).  Below 2002 the phrase is rejected with
       *> COBOLNET0900 naming the edition.
       *>
       *> This is the SECOND half of the PB303 inversion, and the half a positive golden
       *> cannot show: before the fix the compiler ACCEPTED this program at 85 - the one
       *> edition where the phrase does not exist - because AS fell through the PROGRAM-ID
       *> attribute list as a user-defined word, while every edition that HAS the phrase
       *> rejected it with COBOLNET0901.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303B85 AS "PB303B8X".
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
