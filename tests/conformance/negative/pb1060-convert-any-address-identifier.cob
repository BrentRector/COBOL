      *> reject-at: 2023
      *> Train 1021 review finding C-2 (kb/Work PB1060) - ISO/IEC 1989:2023 15.19.3 r7: "When the
      *> source-format is ANY, argument-1 shall be of any usage, except index, message-tag, object reference,
      *> pointer, function-pointer or program-pointer." ADDRESS OF X "creates a unique data item of class
      *> pointer and category data-pointer" (8.4.3.11.4 GR1), the item a USAGE POINTER describes, so it is
      *> refused exactly as a USAGE POINTER argument-1 is. It used to compile and abort at run time with
      *> NotImplemented "address-identifier as a character argument".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1060CV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 R PIC X(40).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONVERT(ADDRESS OF X ANY ANUM HEX) TO R
           DISPLAY R
           STOP RUN.
