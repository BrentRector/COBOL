      *> ISO 13.10.3 SR2 and 14.9.48.3 SR8 (kb/Work PB1182): "Except in a compiler directive, constant-name-1 may be
      *> used anywhere that a format specifies a literal of the class and category of constant-name-1." STRING's
      *> literal-1 and literal-2 and UNSTRING's literal-1 and literal-2 are such positions, so a constant-name stands
      *> there exactly as the literal it names. UNSTRING's identifier-1 is NOT: 14.9.48.3 SR8 makes "the data item
      *> referenced by identifier-1" the sending operand, so a constant-name is refused there (the negative is
      *> StringUnstringOperandScreenTests). This golden is the positive control - the literal positions keep working.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES - NOT FROM A RUN:
      *>   1) STRING K DELIMITED BY KD with K = "XY,ZW" and KD = ",": the content up to the delimiter -> "XY".
      *>   2) UNSTRING "AB,CD" DELIMITED BY KD INTO R1 R2: the constant is literal-1 -> AB / CD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1182K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS "XY,ZW".
       01 KD CONSTANT AS ",".
       01 OUT1 PIC X(8) VALUE SPACES.
       01 S PIC X(8) VALUE "AB,CD".
       01 R1 PIC X(4).
       01 R2 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STRING K DELIMITED BY KD INTO OUT1.
           DISPLAY "1=[" OUT1 "]".
           UNSTRING S DELIMITED BY KD INTO R1 R2.
           DISPLAY "2=[" R1 "][" R2 "]".
           STOP RUN.
