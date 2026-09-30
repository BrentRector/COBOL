      *> PB230 - ISO 14.6.13.2 rule 1, the BOOLEAN sibling of rule 2: "When the content of a boolean sending item
      *> is referenced during the execution of a statement and the content of that sending operand would evaluate
      *> to false in a boolean class condition, the result of the reference is undefined and an
      *> EC-DATA-INCOMPATIBLE exception condition is set to exist, except in the following circumstances: - a
      *> sending item is referenced in a class condition, or - a sending item is processed in a VALIDATE
      *> statement."  The boolean class condition is 8.8.4.4.4 GR3 e: "the condition is true if the content of the
      *> data item referenced by identifier-1 consists entirely of the boolean values '0' and '1'".
      *> A category-boolean item's storage is one character per boolean position (13.18.40.4 GR14's
      *> representation license), so a REDEFINES window over it can deposit a character that is no boolean value
      *> at all - and both channels that READ such an item must report it: the value channel (a boolean
      *> expression operand, L2) and the character channel (DISPLAY, L1).
      *> (The zero-length leg, L4 - a reference that does not touch the incompatible content raises nothing - needs
      *> a zero-length reference modification, which is legal only under the COBOL-2023 directive
      *> REF-MOD-ZERO-LENGTH, so it lives in conformance:2023/pb230_zero_length_boolean_operand.)
      *> EC-DATA-INCOMPATIBLE is fatal (Table 13), so RESUME AT NEXT STATEMENT abandons the raising statement:
      *> L1 prints nothing after its marker and L2 leaves RB holding what L0 stored.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB230BOOLSEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GB.
          05 BB PIC 1(4).
       01 XB REDEFINES GB PIC X(4).
       01 RB PIC 1(8).
       01 ZL PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE B"1010" TO BB.
           DISPLAY "L0 valid content".
           COMPUTE RB = BB.
           DISPLAY "   BB=[" BB "] RB=[" RB "]".
           MOVE "1Q01" TO XB.
           DISPLAY "L1 DISPLAY (character channel)".
           DISPLAY "   BB=[" BB "]".
           DISPLAY "L2 COMPUTE (value channel)".
           COMPUTE RB = BB.
           DISPLAY "   RB=[" RB "]".
           DISPLAY "L3 B-NOT of the same operand".
           COMPUTE RB = B-NOT BB.
           DISPLAY "   RB=[" RB "]".
           STOP RUN.
