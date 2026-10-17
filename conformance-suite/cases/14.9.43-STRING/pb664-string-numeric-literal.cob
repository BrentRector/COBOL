*>--- conformance-case
*> id: pb664-string-numeric-literal
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR1, FIRST HALF: "All literals shall be described as alphanumeric, boolean, or national
      *> literals".  A numeric literal is none of the three, in literal-1 or in literal-2 - the DELIMITED BY
      *> position this program uses, so the two halves of the sentence are witnessed apart.
      *> Before kb/Work PB664 the literal half was unenforced and `STRING SRC DELIMITED BY 5 INTO A` simply
      *> never matched a delimiter.  The rule is COBOL-85 and unchanged since, so every edition rejects.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB664NEGL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(8) VALUE "AB CD EF".
       01 A66 PIC X(40).
       PROCEDURE DIVISION.
           MOVE SPACES TO A66
           STRING SRC DELIMITED BY 5 INTO A66
           DISPLAY "A66=[" A66 "]"
           STOP RUN.
