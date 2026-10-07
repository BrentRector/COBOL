      *> ISO 14.9.43.3 SR8 and 14.9.48.3 SR1 / SR4 (kb/Work PB1181, PB1182): the POSITIVE CONTROLS of the STRING /
      *> UNSTRING operand screens - every legal neighbour of a form the compiler now refuses must keep working, or the
      *> screens are over-rejecting. The refusals themselves are StringUnstringOperandScreenTests (every form, every
      *> edition) and the negative corpus.
      *>
      *> SR8: "Where identifier-1 or identifier-2 is an elementary numeric data item, it shall be described as an
      *> integer without the symbol 'P' in its picture character-string." N3 (PIC 9(3)) and D2 (PIC 9) are integers.
      *> SR4: "Numeric items shall not be specified with the symbol 'P'" - a V alone is not a P, so a PIC 9V9
      *> receiver stays legal.
      *> SR1: "Literal-1 and literal-2 shall be literals of the category alphanumeric or national and shall be
      *> neither a figurative constant that begins with the word ALL nor a zero-length literal." ALL "," is the ALL
      *> PHRASE followed by an ordinary literal-1, and SPACE is a figurative constant that does NOT begin with ALL.
      *> (The hexadecimal-literal neighbour, X"2C", is a COBOL-2002 literal format - kb/Work PB1545 - and lives
      *> in 2002/pb1545_hex_alphanumeric_literal.)
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES (14.9.43.4 GR1/GR3, 14.9.48.4 GR3-GR11) - NOT FROM A RUN:
      *>   1) STRING N3 "-" "AB5CD" DELIMITED BY D2: each sending item is moved up to the first "5" it holds; N3 is
      *>      "123" (no 5), "-" has none, "AB5CD" stops before the 5 -> "123-AB".
      *>   2) UNSTRING "AB,,CD,EF" DELIMITED BY ALL ",": the run of commas is ONE delimiter -> AB / CD / EF.
      *>   3) UNSTRING "12,34" INTO NV (PIC 9V9) M (PIC 9(4)): "12" is moved as the integer 12 to 9V9, keeping the low
      *>      order digits -> 2.0, shown "20"; "34" -> 0034.
      *>   4) UNSTRING "AB,CD" DELIMITED BY "," into alphanumeric receivers: AB / CD.
      *>   5) UNSTRING "AB,CD5EF" DELIMITED BY "," OR "5": AB / CD / EF.
      *>   6) UNSTRING FUNCTION UPPER-CASE("a,b") DELIMITED BY ",": a function-identifier is an identifier-1 -> A / B.
      *>   7) UNSTRING "A   B" DELIMITED BY ALL SPACE: A / B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1181CTL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N3 PIC 9(3) VALUE 123.
       01 D2 PIC 9 VALUE 5.
       01 OUT1 PIC X(12) VALUE SPACES.
       01 S PIC X(12).
       01 A PIC X(4).
       01 B PIC X(4).
       01 C PIC X(4).
       01 NV PIC 9V9.
       01 M PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           STRING N3 "-" "AB5CD" DELIMITED BY D2 INTO OUT1.
           DISPLAY "1=[" OUT1 "]".
           MOVE "AB,,CD,EF" TO S.
           UNSTRING S DELIMITED BY ALL "," INTO A B C.
           DISPLAY "2=[" A "][" B "][" C "]".
           MOVE "12,34" TO S.
           UNSTRING S DELIMITED BY "," INTO NV M.
           DISPLAY "3=[" NV "][" M "]".
           MOVE "AB,CD" TO S.
           UNSTRING S DELIMITED BY "," INTO A B.
           DISPLAY "4=[" A "][" B "]".
           MOVE "AB,CD5EF" TO S.
           UNSTRING S DELIMITED BY "," OR "5" INTO A B C.
           DISPLAY "5=[" A "][" B "][" C "]".
           UNSTRING FUNCTION UPPER-CASE("a,b") DELIMITED BY ","
               INTO A B.
           DISPLAY "6=[" A "][" B "]".
           MOVE "A   B" TO S.
           UNSTRING S DELIMITED BY ALL SPACE INTO A B.
           DISPLAY "7=[" A "][" B "]".
           STOP RUN.
