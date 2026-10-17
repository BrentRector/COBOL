*>--- conformance-case
*> id: pb391_corresponding_rule2_sr8_binary
*> rules: [SR-14.9.25.3-13, GR-14.9.25.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB391 (second half). ISO 1989:2023 14.7.6 rule 2 - "In a MOVE statement, at least one of the
      *> data items is an elementary data item and the resulting move is valid according to the rules for the
      *> MOVE statement" - sends the CORRESPONDING pairing decision to the WHOLE of the MOVE statement's
      *> validity, not to 14.9.25.3 table 16 alone. 14.9.25.3 SR10, the rule that routes to the table, governs
      *> only "all other cases not described in Syntax rules 8 and 9", so SR8 is asked FIRST:
      *>
      *>   SR8 - "If identifier-1 references a data item described with usage binary-char, binary-short,
      *>          binary-long, or binary-double, identifier-2 shall reference a numeric or numeric-edited item."
      *>
      *> COBOL-2002 is SR8's introducing edition: the BINARY-CHAR / -SHORT / -LONG / -DOUBLE usages are a 2002
      *> addition to 13.18.60, so no COBOL-85 program can state this rule at all.
      *>
      *> The four namesake pairs read the rule directly. KX and KN have receivers that are neither numeric nor
      *> numeric-edited, so the pairs do NOT correspond and the receivers keep their prior content - a refused
      *> pair is a SILENT non-selection under rule 2, never a diagnostic. KD and KE have a numeric and a
      *> numeric-edited receiver, the two the rule names, so those pairs DO correspond and move. MM is the
      *> control proving the statement ran at all.
      *>
      *> Before PB391 the filter asked only table 16, where a BINARY-LONG item is an ordinary Numeric/Integer
      *> row member: KX paired and read 00000 over ZZZZZ, and KN paired and read the digits over its national
      *> receiver - while the SAME compiler refused the written MOVE of those very two items with COBOLNET0819
      *> (negative/pb391-move-binary-long-to-alphanumeric-written). One rule, two answers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB391CORRSR8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 KX USAGE BINARY-LONG VALUE 42.
          05 KN USAGE BINARY-LONG VALUE 42.
          05 KD USAGE BINARY-LONG VALUE 42.
          05 KE USAGE BINARY-LONG VALUE 42.
          05 MM PIC X(3) VALUE "AAA".
       01 G2.
          05 KX PIC X(5) VALUE "ZZZZZ".
          05 KN PIC N(5) VALUE N"QQQQQ".
          05 KD PIC 9(5) VALUE 11111.
          05 KE PIC ZZZZ9 VALUE "22222".
          05 MM PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       MAIN.
           MOVE CORRESPONDING G1 TO G2.
           DISPLAY "KX2=[" KX OF G2 "]".
           DISPLAY "KN2=[" KN OF G2 "]".
           DISPLAY "KD2=[" KD OF G2 "]".
           DISPLAY "KE2=[" KE OF G2 "]".
           DISPLAY "MM2=[" MM OF G2 "]".
           STOP RUN.
