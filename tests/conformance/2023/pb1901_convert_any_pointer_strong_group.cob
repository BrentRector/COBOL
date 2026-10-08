      *> kb/Work PB1901 sibling sweep -- CONVERT's ANY source-format over a strongly-typed group with a USAGE POINTER leaf.
      *> 15.19.3 r7 (cite.py --check 15.19.3 OK): an ANY source takes the operand's raw STORAGE; 15.3 item 2 treats a
      *> strongly-typed group as an alphanumeric argument (cite.py --check 15.3 "Strongly-typed group items are treated
      *> as though they were of class and category alphanumeric, unless they are prohibited as arguments of a function"
      *> -> OK). The group's storage image is "ab" followed by the pointer leaf's 8 positions, which hold its storage
      *> image - for the predefined address NULL the zero address, eight X"00" positions (kb/Work PB1071; they were
      *> eight SPACES before), never the reference (CONFORMANCE.md A.1 items 56, 214 and 216), the one image DISPLAY,
      *> a MOVE and every intrinsic string argument read. 15.19.4 r2: ANUM HEX returns the value as hexadecimal
      *> digits - 'a' is 61, 'b' is 62, a zero position is 00 in the alphanumeric coded character set:
      *>   61 62 00 00 00 00 00 00 00 00   =   "61620000000000000000"
      *> It used to compile clean and abort at run time (the whole-group image refusal), exactly as UPPER-CASE did.
      *> CONVERT is a COBOL 2023 function; strongly-typed groups are 2002 (conformance:2002/pb1901_strong_group_pointer_leaf_string_argument).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1901CNV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 A PIC X(2) VALUE "ab".
          05 P USAGE POINTER.
       01 G TYPE REC-T.
       01 R PIC X(20).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CONVERT(G ANY ANUM HEX) TO R
           DISPLAY "[" R "]"
           STOP RUN.
       END PROGRAM PB1901CNV.
