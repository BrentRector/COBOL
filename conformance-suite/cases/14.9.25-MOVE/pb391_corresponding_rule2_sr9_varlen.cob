*>--- conformance-case
*> id: pb391_corresponding_rule2_sr9_varlen
*> rules: [SR-14.9.25.3-13, GR-14.9.25.4-11]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB391 (second half). ISO 1989:2023 14.7.6 rule 2 sends the CORRESPONDING pairing decision to
      *> the WHOLE of the MOVE statement's validity. 14.9.25.3 SR10, the rule that routes to table 16, governs
      *> only "all other cases not described in Syntax rules 8 and 9", so SR9 is asked before the table:
      *>
      *>   SR9 - "If identifier-1 or identifier-2 references a variable-length group then these groups shall be
      *>          compatible groups as specified in 8.5.1.12, Variable-length groups."
      *>
      *> 8.5.1.12.1 states the prohibition in terms of the OTHER operand - a variable-length group "may not
      *> undergo a comparison or a move operation, in either direction, explicitly or otherwise, unless the
      *> other operand is a compatible group" - so an ELEMENTARY namesake opposite a variable-length group is a
      *> violation, not a fall-through, in both directions.
      *>
      *> COBOL-2014 is SR9's introducing edition: a variable-length group needs a DYNAMIC LENGTH elementary
      *> item (13.18.19) or an OCCURS DYNAMIC table (13.18.38 format 4), both 2014 additions.
      *>
      *> VA (variable-length group -> elementary) and VB (elementary -> variable-length group) are the two
      *> directions 8.5.1.12.1 names, and they are the ONLY shapes in which SR9 can reach rule 2's filter at
      *> all: a group x group namesake pair DESCENDS instead (14.9.25.4 GR11 NOTE 5), so one side of any pair
      *> the filter sees is elementary and an engaged SR9 is therefore always a violation. Neither pair
      *> corresponds, so both receivers keep their prior content - a refused pair is a SILENT non-selection
      *> under rule 2, never a diagnostic. VC is the CONTROL for the descent: two compatible variable-length
      *> groups of the same name, whose elementary members DO pair and move, proving the fix refuses the pair
      *> SR9 names and not every namesake under a variable-length group. MM proves the statement ran.
      *>
      *> Before PB391 the filter asked only table 16, which has no row for a variable-length group and exempts
      *> every GROUP operand under 14.9.25.4 GR4: VA and VB both paired, and the emitted implied MOVE asked for
      *> a whole-group character image the pair can never have, so the program reached the RUN TIME and threw
      *> NotImplementedCobolFeatureException - while the SAME compiler refused the written MOVE of those two
      *> items with COBOLNET1931.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB391CORRSR9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 VA.
             10 VAD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 VB PIC X(5) VALUE "SSSSS".
          05 VC.
             10 VCD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 MM PIC X(3) VALUE "AAA".
       01 G2.
          05 VA PIC X(5) VALUE "ZZZZZ".
          05 VB.
             10 VBD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 VC.
             10 VCD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 MM PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "PP" TO VAD OF VA OF G1.
           MOVE "QQ" TO VCD OF VC OF G1.
           MOVE "RR" TO VBD OF VB OF G2.
           MOVE "TT" TO VCD OF VC OF G2.
           MOVE CORRESPONDING G1 TO G2.
           DISPLAY "VA2=[" VA OF G2 "]".
           DISPLAY "VB2=[" VBD OF VB OF G2 "]".
           DISPLAY "VC2=[" VCD OF VC OF G2 "]".
           DISPLAY "MM2=[" MM OF G2 "]".
           STOP RUN.
