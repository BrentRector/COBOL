      *> reject-at: 2002 2014 2023
      *> ISO 15.3 item 2 treats a strongly-typed group as class and category ALPHANUMERIC for an argument of the
      *> alphanumeric TYPE only (cite.py --check 15.3 "Strongly-typed group items are treated as though they were of
      *> class and category alphanumeric, unless they are prohibited as arguments of a function" -> OK). ABS's
      *> argument is class numeric (15.7.3 r1, "Argument-1 shall be of class numeric"; cite.py --check 15.7.3 OK), and
      *> 8.5.2.1 makes the class of a strongly-typed group its type-name (cite.py --check 8.5.2.1 "Both the class and the
      *> category of a strongly-typed group item are the type-name specified in the TYPE clause" -> OK), so the group is
      *> not class numeric here however numeric its leaf is. This is the boundary kb/Work PB1901 pins: a strong group is
      *> legal for the string-argument functions (conformance:2002/pb1901_strong_group_pointer_leaf_string_argument)
      *> and refused for the numeric ones.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1901NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 N PIC 9(3) VALUE 7.
       01 G TYPE REC-T.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION ABS(G)
           STOP RUN.
