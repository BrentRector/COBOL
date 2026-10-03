*> reject-at: 85
*> ISO 8.7.5.2 SR11 (cite.py --check 8.7.5.2 "<> is an abbreviation for NOT EQUAL"; the SR11 line was read directly) and
*> 8.3.2.4.2 2) ("<>" - "Relational operator - not equal"). COBOL-85's relational operators are the six GREATER / LESS /
*> EQUAL forms with their symbols and NOT; <> is a COBOL-2002 introduction (kb/Work PB1459; the edge is derived,
*> VCR row 7.26), so it is refused below 2002 exactly as its siblings & and >> are. The word form NOT EQUAL TO is
*> COBOL-85 and stays legal (the positive twin is 2002/l1c26_relational_operator_not_equal_symbol).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1459N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           IF A <> 3 DISPLAY "NE".
           STOP RUN.
