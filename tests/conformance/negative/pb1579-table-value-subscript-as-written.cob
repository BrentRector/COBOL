      *> reject-at: 2002 2014 2023
      *> A FORMAT 2 VALUE SUBSCRIPT BEYOND THE HOST RANGE IS QUOTED AS WRITTEN
      *> (kb/Work PB1579). ISO/IEC 1989:2023 §8.4.2.3.4 GR2: "The value of a
      *> subscript shall be a positive integer", and §13.18.63.3 SR20 bounds each
      *> subscript-1 by its OCCURS maximum, so FROM (-99999999999999999999) is out
      *> of range 1..3. The diagnostic must name the number the program contains:
      *> before PB1579 the subscript was read by long.TryParse, saturated to
      *> int.MaxValue and quoted back as the POSITIVE 2147483647; with the one
      *> integer-literal reader it saturated to int.MinValue and was quoted as
      *> -2147483648. The .err pins the written text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1579TV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-T PIC X(2) OCCURS 3 VALUE "AB" FROM (-99999999999999999999) TO (+3).
       PROCEDURE DIVISION.
           DISPLAY S-T(1).
           STOP RUN.
