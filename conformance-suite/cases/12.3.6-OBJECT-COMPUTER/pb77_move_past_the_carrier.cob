*>--- conformance-case
*> id: pb77_move_past_the_carrier
*> rules: [GR-12.3.6.4-4, GR-14.6.8.2-1, GR-14.6.8.2-2, GR-14.6.8.2-4, GR-14.6.13.1.3-8, DOC-A.1-70]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB77 - a value past the Int128 carrier lands its LOW-ORDER digits in a truncating store, never the
      *> saturation sentinel's. ISO 14.6.8.2 r1/r2/r4: a MOVE aligns the sending value by decimal point and
      *> transfers it "with zero fill or truncation on either end" - there is no size error in a MOVE. Before this
      *> landing MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9) stored 884105727 and TO PIC 9(5) stored 03715: the
      *> low digits of Int128.MaxValue (170141183460469231731687303715884105727), because the sender's rescale
      *> to the receiver's working scale SATURATED (the PB13 discipline for a CHECKED store, whose capacity check
      *> then raises) and the MOVE store kept v mod 10^digits of the sentinel. Every carrier now has two landing
      *> forms, chosen by the LANDING: checked (ON SIZE ERROR / EC-SIZE) saturates so the size error fires;
      *> unchecked (MOVE, the no-phrase store, INVOKE BY CONTENT) keeps the low-order digits - the SDIDI carrier's
      *> ToUnscaledChecked / ToUnscaled pair (PB74) applied to the native exact family and the float family.
      *>
      *> The float family's value IS a binary64 (CONFORMANCE.md item 92): 1.0E+40 as a double is exactly
      *> 10000000000000000303786028427003666890752, so its low-order digits are those - the same digits an
      *> in-carrier value has always landed (1.0E+25's 69664), continued past the carrier. Every expected value
      *> below is derived by exact arithmetic on the sending value, never observed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB77MOVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V9   PIC V9(9).
       01 X5   PIC 9(5).
       01 S5   PIC S9(5) SIGN LEADING SEPARATE.
       01 W31  PIC 9(31).
       01 ED   PIC ZZZZ9.
       01 D40  USAGE COMP-2 VALUE 1.0E+40.
       01 DN   USAGE COMP-2 VALUE -2.5E+40.
       PROCEDURE DIVISION.
      *> --- the exact family (NUMVAL-F) as a MOVE sender: 5E+30 has all-zero low digits ---
           MOVE FUNCTION NUMVAL-F("5E+30") TO V9
           DISPLAY "NVF-V9=" V9
           MOVE FUNCTION NUMVAL-F("5E+30") TO X5
           DISPLAY "NVF-X5=" X5
      *> a 31-digit significand times 10^3: -1234567890123456789012345678901000, low five digits 01000
           MOVE FUNCTION NUMVAL-F("-1234567890123456789012345678901E+3") TO S5
           DISPLAY "NVF-S5=" S5
      *> NUMVAL's 31-digit significand at the receiver's working scale is 40 digits: low five are 78901
           MOVE FUNCTION NUMVAL("1234567890123456789012345678901") TO X5
           DISPLAY "NV-X5=" X5
      *> --- the float family / a COMP-2 item as a MOVE sender, past the carrier ---
           MOVE D40 TO X5
           DISPLAY "D40-X5=" X5
           MOVE D40 TO V9
           DISPLAY "D40-V9=" V9
           MOVE D40 TO W31
           DISPLAY "D40-W31=" W31
           MOVE D40 TO ED
           DISPLAY "D40-ED=" ED
           MOVE DN TO S5
           DISPLAY "DN-S5=" S5
      *> --- the no-phrase arithmetic store is the same truncating landing (14.6.13.1.3 item 8) ---
           COMPUTE X5 = FUNCTION NUMVAL-F("5E+30")
           DISPLAY "CMP-NVF-X5=" X5
           COMPUTE X5 = D40
           DISPLAY "CMP-D40-X5=" X5
           COMPUTE X5 = FUNCTION ABS(DN)
           DISPLAY "CMP-ABS-X5=" X5
           COMPUTE ED = D40
           DISPLAY "CMP-D40-ED=" ED
      *> --- the CHECKED landing is unchanged: the size error fires and the receiver is left alone ---
           MOVE 1 TO X5
           COMPUTE X5 = FUNCTION NUMVAL-F("5E+30")
               ON SIZE ERROR DISPLAY "SIZE-NVF=YES X5=" X5
               NOT ON SIZE ERROR DISPLAY "SIZE-NVF=NO X5=" X5
           END-COMPUTE
           COMPUTE X5 = D40
               ON SIZE ERROR DISPLAY "SIZE-D40=YES X5=" X5
               NOT ON SIZE ERROR DISPLAY "SIZE-D40=NO X5=" X5
           END-COMPUTE
           COMPUTE X5 = FUNCTION ABS(DN)
               ON SIZE ERROR DISPLAY "SIZE-ABS=YES X5=" X5
               NOT ON SIZE ERROR DISPLAY "SIZE-ABS=NO X5=" X5
           END-COMPUTE
           STOP RUN.
