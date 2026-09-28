      *> reject-at: 2002 2014 2023
      *> kb/Work PB1427 - the predefined NULL is written in operand
      *> positions 8.4.3.10.3 SR1 does not list: a STRING sender, an
      *> INSPECT operand, an intrinsic-function argument and a relation
      *> with no data-item operand for NULL to be associated with.
      *> RULE (8.4.3.10.3 SR1): "This item may be used only in the
      *> following cases, depending upon the associated data item's
      *> class" - a) pointer: "only as a sending operand in an
      *> INITIALIZE or a SET statement; as an argument in a program-
      *> prototype format CALL statement, a function-prototype format
      *> function activation, or a method invocation; or in a pointer-
      *> or-object-reference relation condition". An intrinsic function
      *> is not a function-prototype (8.4.3.2.2 names them apart).
      *> cite.py --check 8.4.3.10.3 "This item may be used only in the
      *>   following cases" -> OK 1)
      *> Before the fix each statement compiled and ran NULL as
      *> LOW-VALUE: STRING stored a NUL character, INSPECT tallied
      *> against it, FUNCTION LENGTH(NULL) answered 1, and NULL = NULL
      *> crashed the code generator. Each is COBOLNET2576 now.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1427N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WX PIC X(4) VALUE "ABCD".
       01 WT PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           STRING NULL DELIMITED BY SIZE INTO WX
           INSPECT WX TALLYING WT FOR ALL NULL
           COMPUTE WT = FUNCTION LENGTH(NULL)
           IF NULL = NULL DISPLAY "EQ".
           STOP RUN.
