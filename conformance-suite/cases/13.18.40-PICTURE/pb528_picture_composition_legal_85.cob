      *>--- conformance-case
      *> id: pb528_picture_composition_legal_85
      *> rules: [SR-13.18.40.3-2, SR-13.18.40.3-16, SR-13.18.40.3-18,
      *>    SR-13.18.40.3-19, SR-13.18.40.3-21, SR-13.18.40.3-24,
      *>    SR-13.18.40.3-26]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB528 - THE DRIFT TEST OVER THE LEGAL SHAPES. Closing the 13.18.40.3 composition gap means
      *> writing down prohibitions, and a prohibition written one symbol too wide REJECTS LEGAL SOURCE, which
      *> is worse than the under-rejection it replaces. Every entry here is a shape the standard permits and
      *> the validator must keep permitting; each is MOVEd through and DISPLAYed, so a wrong ACCEPT/REJECT and
      *> a wrong RENDER are both visible. The composition rules are syntax rules of every edition from 1985,
      *> so 85 is the sharpest edition to pin them at.
      *>
      *> L01 ZZ9 with 12 - 13.18.40.5 rule 7 a: the replacement character (space, for 'Z') goes into every
      *>     position preceding the first nonzero numeric character. The value in three digit positions is
      *>     012, so position 1 is a leading zero and position 2 is the first nonzero => " 12".
      *> L02 99PP with 1200 - 13.18.40.4 GR14 'P': the assumed point is "to the right of the string of 'P's
      *>     if they indicate the rightmost digit positions", and 'P' "is not counted in the size of the
      *>     item". Two stored digits, scale -2, algebraic value 12 x 10^2 => "12".
      *> L03 PP99 with 0.0012 - the same rule's other leg: the point is to the LEFT of a leftmost 'P' string,
      *>     so the scale is 4 and the two stored digits are 12 => "12".
      *> L04 S999 SIGN IS LEADING SEPARATE with -12 - SR18's legal shape; 13.18.52 puts the sign in its own
      *>     leading position => "-012".
      *> L05 99PPV with 1200 - SR19's "immediately follow the last symbol 'P'" leg; the V restates the point
      *>     GR14 already implies, so the image is L02's => "12".
      *> L06 VPP99 with 0.0012 - SR19's "immediately precede the first symbol 'P'" leg => "12".
      *> L07 ***9 with 7 - rule 7's asterisk replacement over the leading zeros of 0007 => "***7".
      *> L08 $999+ with -12 - SR26's "leftmost symbol" currency leg beside a trailing sign; Table 8 renders a
      *>     '+' over a negative value as '-' => "$012-".
      *> L09 $$$9.99 with 12.34 - rule 6 a floating insertion: a single '$' immediately precedes the first
      *>     nonzero numeric character, spaces before it. Three '$' give two digit positions, plus the '9'
      *>     and the two fraction digits; 012.34 suppresses the leading 0 => " $12.34".
      *> L10 +$$99 with 12 - SR26's "optionally preceded by one of the symbols '+' or '-'". The two '$' are a
      *>     floating string (one digit position); 012 suppresses the leading 0 and floats the '$' => "+ $12".
      *> L11 999$+ with -12 - SR26's rightmost-currency leg, "optionally followed by one of '+', '-', 'CR' or
      *>     'DB'" => "012$-".
      *> L12 ++ with 1 - SR12 a's second bullet made concrete: two occurrences of '+' ARE a picture, and rule
      *>     6 makes them a floating string of one digit position => "+1".
      *> L13 999.99CR with -12.34 - Table 8: 'CR' over a negative value renders CR => "012.34CR".
      *> L14 ZZZ,ZZ9.99 with 1234.5 - rule 7's "Any of the simple insertion editing symbols embedded in this
      *>     string ... are part of the string": the comma is inside the suppression string => "  1,234.50".
      *> L15 XXBXX with "ABCD" - rule 3 simple insertion of the space => "AB CD".
      *> L16 9,999 with 1234 => "1,234".
      *> L17 -999 with -12 - Table 8's '-' over a negative value => "-012".
      *> L18 999- with -12 => "012-".
      *> L19 9V9 with 1.2 - category numeric; 'V' is not counted in the size => "12".
      *> L20 $$$$PP - DECLARED ONLY, because it is the shape SR16 is measured against: its digit positions are
      *>     the FLOATING CURRENCY occurrences plus the 'P's (13.18.40.4 GR14), so the 'P' string really is at
      *>     the rightmost digit positions even though no '9', 'Z' or '*' appears (kb/Work PB155).
      *> L21 S9(5) - SR18 with a repeat factor, the shape PB528's SR22 leg pairs with.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB528LGL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L01 PIC ZZ9.
       01 L02 PIC 99PP.
       01 L03 PIC PP99.
       01 L04 PIC S999 SIGN IS LEADING SEPARATE.
       01 L05 PIC 99PPV.
       01 L06 PIC VPP99.
       01 L07 PIC ***9.
       01 L08 PIC $999+.
       01 L09 PIC $$$9.99.
       01 L10 PIC +$$99.
       01 L11 PIC 999$+.
       01 L12 PIC ++.
       01 L13 PIC 999.99CR.
       01 L14 PIC ZZZ,ZZ9.99.
       01 L15 PIC XXBXX.
       01 L16 PIC 9,999.
       01 L17 PIC -999.
       01 L18 PIC 999-.
       01 L19 PIC 9V9.
       01 L20 PIC $$$$PP.
       01 L21 PIC S9(5).
       PROCEDURE DIVISION.
           MOVE 12 TO L01
           MOVE 1200 TO L02
           MOVE 0.0012 TO L03
           MOVE -12 TO L04
           MOVE 1200 TO L05
           MOVE 0.0012 TO L06
           MOVE 7 TO L07
           MOVE -12 TO L08
           MOVE 12.34 TO L09
           MOVE 12 TO L10
           MOVE -12 TO L11
           MOVE 1 TO L12
           MOVE -12.34 TO L13
           MOVE 1234.5 TO L14
           MOVE "ABCD" TO L15
           MOVE 1234 TO L16
           MOVE -12 TO L17
           MOVE -12 TO L18
           MOVE 1.2 TO L19
           DISPLAY "L01=[" L01 "]"
           DISPLAY "L02=[" L02 "]"
           DISPLAY "L03=[" L03 "]"
           DISPLAY "L04=[" L04 "]"
           DISPLAY "L05=[" L05 "]"
           DISPLAY "L06=[" L06 "]"
           DISPLAY "L07=[" L07 "]"
           DISPLAY "L08=[" L08 "]"
           DISPLAY "L09=[" L09 "]"
           DISPLAY "L10=[" L10 "]"
           DISPLAY "L11=[" L11 "]"
           DISPLAY "L12=[" L12 "]"
           DISPLAY "L13=[" L13 "]"
           DISPLAY "L14=[" L14 "]"
           DISPLAY "L15=[" L15 "]"
           DISPLAY "L16=[" L16 "]"
           DISPLAY "L17=[" L17 "]"
           DISPLAY "L18=[" L18 "]"
           DISPLAY "L19=[" L19 "]"
           STOP RUN.
