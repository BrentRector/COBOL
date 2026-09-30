      *> PB79 - GROUP-USAGE BIT (ISO 13.18.29): the group is a bit group AND a bit data item, class and category
      *> boolean, "treated as though it were an elementary data item of usage bit ... described with PICTURE 1(m),
      *> where m is the bit length of the group" (GR1 b): its operand value is its bit string (the subordinates'
      *> boolean positions in order - 8.5.1.6.3 rule 1 puts same-level bit items, elementary or bit group, at
      *> successive bit positions), a MOVE to / from it pads and truncates in boolean positions, FUNCTION LENGTH
      *> is its bit length (15.50.4 r1) with NO trailing filler (the 8.5.1.6.3 NOTE excludes "a record that is
      *> entirely a bit group"), BYTE-LENGTH its bytes. Data-model design D19 + D20. Also pinned: a group-level
      *> USAGE BIT clause applies to its PICTURE-1 subordinates (13.18.60.4 GR1 - the same rule GROUP-USAGE BIT
      *> implies; before D20 the leaves stayed display-form and G occupied 8 bytes), and a bit group NESTED in an
      *> alphanumeric group shares a byte with a preceding same-level bit item (rule 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB79BIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT.
          05 B1 PIC 1(3) VALUE B"101".
          05 B2 PIC 1(5) VALUE B"11001".
          05 SUB.
             10 B3 PIC 1(4) VALUE B"0110".
       01 BR PIC 1(12) USAGE BIT.
       01 BS PIC 1(6) USAGE BIT.
       01 BD PIC 1(12).
       01 G USAGE BIT.
          05 GA PIC 1(5) VALUE B"10101".
          05 GB PIC 1(3) VALUE B"111".
       01 R2.
          05 P PIC 1(3) USAGE BIT VALUE B"110".
          05 Q GROUP-USAGE BIT.
             10 Q1 PIC 1(2) VALUE B"00".
             10 Q2 PIC 1(3) VALUE B"010".
          05 T PIC X VALUE "t".
      *> kb/Work PB173 - the REFERENCE-MODIFICATION legs. Data-model design D20 recorded this very gap: its
      *> "As built" paragraph lists "ref-mod read + write" for pb79_group_usage_national and OMITS it here.
       01 XM GROUP-USAGE BIT.
          05 XM1 PIC 1(4) VALUE B"1100".
          05 XM2 PIC 1(4) VALUE B"1010".
       01 XW GROUP-USAGE BIT.
          05 XW1 PIC 1(5) VALUE B"11111".
          05 XW2 PIC 1(7) VALUE B"0000000".
       01 XN GROUP-USAGE BIT.
          05 XN1 PIC 1(3) VALUE B"101".
          05 XNI GROUP-USAGE BIT.
             10 XN2 PIC 1(3) VALUE B"011".
      *> kb/Work PB173 - the OCCURS DEPENDING legs. A bit group may hold an occurs-depending table (13.18.29.3
      *> SR1 bars only a strongly-typed or VARIABLE-LENGTH group, and 8.5.1.12 makes neither an ODO table), so
      *> 13.18.38.4 GR8's current-extent rule and 13.18.29.4 GR1b's as-if PICTURE 1(m) meet on one operand.
       01 NN PIC 9 VALUE 4.
       01 XO GROUP-USAGE BIT.
          05 XO1 PIC 1(2) VALUE B"11".
          05 XOT PIC 1(3) OCCURS 1 TO 4 DEPENDING ON NN.
      *> kb/Work PB173 - the CONVERT ANY raw-storage legs. CVB is the ELEMENTARY twin of the bit group; CVD is
      *> the DISPLAY-form boolean control, whose carrier IS its storage (13.18.60.4 GR7).
       01 CVB PIC 1(8) USAGE BIT VALUE B"11001010".
       01 CVD PIC 1(8) VALUE B"11001010".
       01 CVR PIC X(20).
       PROCEDURE DIVISION.
           DISPLAY "LEN=" FUNCTION LENGTH(BG) " BYTES=" FUNCTION BYTE-LENGTH(BG).
           MOVE BG TO BR.
           DISPLAY "BR=[" BR "]".
           MOVE BG TO BS.
           DISPLAY "BS=[" BS "]".
           MOVE BG TO BD.
           DISPLAY "BD=[" BD "]".
           MOVE B"111100001111" TO BG.
           DISPLAY "B1=" B1 " B2=" B2 " B3=" B3.
           DISPLAY "BG=[" BG "]".
           IF BG = B"111100001111" DISPLAY "EQ" ELSE DISPLAY "NE" END-IF.
           MOVE B"1" TO BG.
           DISPLAY "BG=[" BG "]".
           IF (BG B-OR B"010000000000") = B"110000000000"
               DISPLAY "OR-EQ" ELSE DISPLAY "OR-NE" END-IF.
           DISPLAY "SUB-LEN=" FUNCTION LENGTH(SUB) " G-LEN=" FUNCTION LENGTH(G)
                   " G-BYTES=" FUNCTION BYTE-LENGTH(G).
           DISPLAY "R2-BYTES=" FUNCTION BYTE-LENGTH(R2) " Q-LEN=" FUNCTION LENGTH(Q).
           MOVE B"11" TO Q1.
           DISPLAY "Q=[" Q "] R2-T=[" T "]".
      *> ===== kb/Work PB173 - a bit group reference-modifies in BIT POSITIONS =====
      *> 8.4.3.3.3 SR1 last sentence: "For reference modification, bit group items and national group items are
      *> treated as elementary data items" - admitted AS AN ELEMENTARY ITEM, not through the group bullet.
      *> 13.18.29.4 GR1b makes that item PICTURE 1(m) of usage bit; 8.4.3.3.4 GR5a then: "If the usage of
      *> identifier-1 is bit, positions used in evaluation are bit positions". 8.4.3.3.4 GR6: the unique item
      *> keeps class, category and usage - boolean, bit.
      *> Before the fix the substrate was AsImage()'s PACKED bytes (ceil(m/8) characters) at BYTE offsets, so
      *> every leg below was a SILENT WRONG ANSWER: the read compared the wrong characters and the writes
      *> spliced characters into the packed image, which FromImage then redistributed.
      *> XM = 11001010 (m = 8).
           IF XM(1:3) = B"110" DISPLAY "RMR=OK"
              ELSE DISPLAY "RMR=WRONG [" XM(1:3) "]" END-IF.
      *> exact-width store: positions 1-2 <- 01, the tail untouched.
           MOVE B"01" TO XM(1:2).
           DISPLAY "RM1=[" XM "]".
      *> 14.6.8.6 - a SHORT source is "transferred ... into the corresponding boolean positions of the receiving
      *> data item, with zero fill or truncation to the right": 1 bit into a 3-bit slice gives 100, NOT a space
      *> fill. This is the leg that catches PlaceRenderer.Write's ref-mod pad reading raw Pic (null for a group).
           MOVE B"1" TO XM(3:3).
           DISPLAY "RM2=[" XM "]".
      *> 8.3.3.6.4 GR2 - a FIGURATIVE source fills EVERY position of the slice. The OTHER arm of the same pad
      *> defect: MoveEmitter's RefModSlice figurative fill made the identical raw-Pic read, nine files away.
           MOVE ALL ZERO TO XM(3:4).
           DISPLAY "RM3=[" XM "]".
      *> BYTE-BOUNDARY CROSSING - 12 bits pack into 2 characters, so a slice over positions 4..9 spans the
      *> packed-byte boundary, the case an 8-bit group cannot distinguish. XW = 111110000000.
           MOVE B"101010" TO XW(4:6).
           DISPLAY "RM4=[" XW "]".
      *> NESTED bit group - AsBits composes recursively (BitCarrierOf over the members), so the new place
      *> carries through a bit group inside a bit group. XN = 101011.
           IF XN(2:3) = B"010" DISPLAY "RM5=OK"
              ELSE DISPLAY "RM5=WRONG [" XN(2:3) "]" END-IF.
      *> COMPUTE RECEIVER - PB157 staged this loud (DiagnosticCatalog.RefModBitGroupSlice) precisely because the
      *> boolean channel counted BIT positions over a BYTE substrate. The model makes the units agree, so the
      *> containment and its descriptor are deleted and the receiver is an ordinary boolean one.
           COMPUTE XM(1:3) = B"101".
           DISPLAY "RM6=[" XM "]".
      *> ===== kb/Work PB173 - the OCCURS DEPENDING current extent, IN BIT POSITIONS =====
      *> 13.18.38.4 GR8 a): "If the data item referenced by data-name-1 is outside the group, only that part of
      *> the table area that is specified by the value of the data item referenced by data-name-1 at the start of
      *> the operation will be used." The bit group's part is counted in BIT positions (13.18.29.4 GR1b + 8.4.3.3.4
      *> GR5a), so at NN=2 the operand is 2 + 2*3 = 8 positions of the 14-position maximum - NOT the two PACKED
      *> characters the image channel counts. Before the fix the character-unit arithmetic gave a NEGATIVE fixed
      *> prefix (PhysicalWidth 2 - elem 1 * max 4 = -2) and the whole operand rendered as the EMPTY string, while
      *> the ref-mod slice read the full 14-position maximum: two alphabets and two extents on one operand.
           MOVE B"101" TO XOT(1).
           MOVE B"010" TO XOT(2).
           MOVE B"111" TO XOT(3).
           MOVE B"111" TO XOT(4).
           MOVE 2 TO NN.
           DISPLAY "ODO1=[" XO "]".
           DISPLAY "ODO2=[" XO(1:8) "]".
      *> a position past the current extent is outside the operand (8.4.3.3.4 item 5 b/c - EC-BOUND-REF-MOD). The
      *> ODO3/ODO4 legs that used to be here read and wrote XO(9:3) with checking off and pinned the space-padded
      *> read and the clamped write; an UNCHECKED violation now ends the run unit (kb/Work PB1707, owner decision
      *> R60 - the loud abort), so a golden cannot carry it and RefModUncheckedTerminationTests owns the witness
      *> over an occurs-depending group's current extent. The numbering is kept.
      *> the boolean-expression channel reads the SAME alphabet at the SAME extent (the comparison used to put
      *> the packed byte image against a boolean literal).
           IF XO = B"11101010" DISPLAY "ODO5=OK"
              ELSE DISPLAY "ODO5=WRONG [" XO "]" END-IF.
      *> GR8 a) receiving, WITHIN the extent: the whole-group bit receiver stores at the current extent and the
      *> unused table area is untouched (NN back to 4 shows the tail survived).
           MOVE B"00000000" TO XO.
           MOVE 4 TO NN.
           DISPLAY "ODO6=[" XO "]".
      *> ===== kb/Work PB173 - CONVERT's ANY raw-storage channel over a BIT SLICE =====
      *> 15.19.3 r7 asks for argument-1's STORAGE ("It is not necessary for the contents to be valid according
      *> to the usage"), and 8.4.3.3.4 GR5a makes a usage-bit operand's positions BIT positions while GR6 keeps
      *> the slice's class, category and usage - so a bit slice's storage is its PACKED bits, padded per 15.19.4
      *> r2 ("If the number of bits in argument-1 is not a multiple of those needed for a single alphanumeric
      *> character, the trailing portion needed to make up a complete multiple is padded with zero bits").
      *> Before the fix the sliced legs returned the '0'/'1' CHARACTERS (313130) while the unsliced legs
      *> correctly returned the packed byte - one operand, two encodings. XM is 10100010 here (after RM6).
           MOVE FUNCTION CONVERT(XM ANY ANUM HEX) TO CVR.
           DISPLAY "CV1=" CVR.
           MOVE FUNCTION CONVERT(XM(1:3) ANY ANUM HEX) TO CVR.
           DISPLAY "CV2=" CVR.
      *> the ELEMENTARY twin took the same arm and was wrong for the same reason (it predates the bit-group
      *> place entirely) - one arm fixes both.
           MOVE FUNCTION CONVERT(CVB ANY ANUM HEX) TO CVR.
           DISPLAY "CV3=" CVR.
           MOVE FUNCTION CONVERT(CVB(1:3) ANY ANUM HEX) TO CVR.
           DISPLAY "CV4=" CVR.
      *> ⛔ THE CONTROL THAT KEEPS THE FIX HONEST: a DISPLAY-form boolean is NOT packed - 13.18.60.4 GR7 makes
      *> usage DISPLAY "an alphanumeric coded character set", one character per boolean position, so its carrier
      *> IS its storage. This leg must keep returning the characters.
           MOVE FUNCTION CONVERT(CVD(1:3) ANY ANUM HEX) TO CVR.
           DISPLAY "CV5=" CVR.
           STOP RUN.
