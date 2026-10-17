      *>--- conformance-case
      *> id: pb158_arith_precedence_spine
      *> rules: [GR-8.8.1.2-2, GR-8.8.1.2-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB158APS.
      *> kb/Work PB158 - THE 8.8.1.2 PRECEDENCE SPINE, PINNED IN ONE
      *> LABELLED GOLDEN. EVERY EXPECTED VALUE IS DERIVED FROM GR2's
      *> HIERARCHY (1st UNARY +/-, 2nd EXPONENTIATION, 3rd MULTIPLY
      *> AND DIVIDE, 4th ADD AND SUBTRACT) AND GR3 (CONSECUTIVE
      *> OPERATIONS OF THE SAME LEVEL EXECUTE LEFT TO RIGHT), NOT
      *> FROM A RUN.
      *>
      *> R1  - 2 ** 2      = 4    RANK 1 OVER RANK 2. UNARY MINUS
      *>                          OUTRANKS EXPONENTIATION, SO THIS IS
      *>                          (-2)**2, NOT -(2**2). THIS IS COBOL'S
      *>                          ONE INVERSION OF THE MAINSTREAM
      *>                          CONVENTION AND NOTHING PINNED IT
      *>                          ANYWHERE BEFORE THIS GOLDEN.
      *> R2  2 ** 3 * 4    = 32   RANK 2 OVER RANK 3: (2**3)*4.
      *> R3  2 + 3 * 4     = 14   RANK 3 OVER RANK 4: 2+(3*4).
      *> R4  2 ** 3 ** 2   = 64   GR3 LEFT FOLD ON **: (2**3)**2.
      *>                          A RIGHT FOLD WOULD GIVE 512.
      *> R5  (2 + 3) * 4   = 20   GR1 PARENTHESES BEAT PRECEDENCE -
      *> R6  2 + 3 * 4     = 14   ITS DISCRIMINATOR, SAME OPERANDS.
      *> R7  8 / 4 / 2     = 1    GR3 LEFT FOLD ON / : (8/4)/2.
      *>                          A RIGHT FOLD WOULD GIVE 4.
      *>
      *> R8/R9 ARE GR7's CONTRAST: "ARITHMETIC EXPRESSIONS ALLOW THE
      *> USER TO COMBINE ARITHMETIC OPERATIONS WITHOUT THE
      *> RESTRICTIONS ON COMPOSITE OF OPERANDS AND RECEIVING DATA
      *> ITEMS." THE WIDE COMPUTE IS LEGAL BECAUSE OF THAT RULE; THE
      *> SAME OPERANDS THROUGH ADD ARE SUBJECT TO 14.9.2's COMPOSITE
      *> RULE INSTEAD. BOTH MUST PRODUCE THE SAME ARITHMETIC ANSWER.
      *>
      *> R10-R12 ARE TABLE 3 'P' CELLS THAT MUST KEEP COMPILING - THE
      *> OVER-REJECTION GUARD FOR THE COBOLNET1719 SCREEN. ROW
      *> '+ - * / **' x COLUMN 'UNARY' = P, SO A BINARY OPERATOR MAY
      *> BE FOLLOWED BY A UNARY SIGN; ROW '(' x COLUMN 'UNARY' = P;
      *> AND ROW 'UNARY' x COLUMN 'IDENTIFIER OR LITERAL' = P, WHICH
      *> IS WHAT '- -2' IS (8.3.3.3.2 RULE 2 PUTS THE ADJACENT SIGN
      *> INSIDE THE LITERAL).
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 5.
       01 B PIC S9(4) VALUE 3.
       01 W1 PIC S9(9) VALUE 123456789.
       01 W2 PIC S9(9) VALUE 987654321.
       01 R PIC S9(9) VALUE 0.
       01 WD PIC S9(18) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = - 2 ** 2
           DISPLAY "R1=" R
           COMPUTE R = 2 ** 3 * 4
           DISPLAY "R2=" R
           COMPUTE R = 2 + 3 * 4
           DISPLAY "R3=" R
           COMPUTE R = 2 ** 3 ** 2
           DISPLAY "R4=" R
           COMPUTE R = (2 + 3) * 4
           DISPLAY "R5=" R
           COMPUTE R = 2 + 3 * 4
           DISPLAY "R6=" R
           COMPUTE R = 8 / 4 / 2
           DISPLAY "R7=" R
           COMPUTE WD = W1 + W2 - W1
           DISPLAY "R8=" WD
           MOVE 0 TO WD
           ADD W1 W2 GIVING WD
           SUBTRACT W1 FROM WD
           DISPLAY "R9=" WD
           COMPUTE R = A - - B
           DISPLAY "R10=" R
           COMPUTE R = - (A + B)
           DISPLAY "R11=" R
           COMPUTE R = - -2
           DISPLAY "R12=" R
           STOP RUN.
