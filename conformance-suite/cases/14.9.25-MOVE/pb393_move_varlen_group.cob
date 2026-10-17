*>--- conformance-case
*> id: pb393_move_varlen_group
*> rules: [SR-14.9.25.3-9, GR-14.9.25.4-9]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB393. ISO 1989:2023 14.9.25.4 GR9 - "If both the sending operand and the receiving data item
      *> are group items and one or both is a variable-length group" - over three shapes. The operation is a
      *> STRUCTURAL walk over the two groups' 8.5.1.12 components, which the COMPARISON twin states outright
      *> (8.8.4.2.17: "when corresponding dynamic-length elementary items are encountered, the length is
      *> determined as described in 8.5.1.10.4 ... comparison continues with the next data item in each of the
      *> compatible groups"), so GR9a's "corresponding character position" is the position under 8.5.1.12.2's
      *> correspondence - the accounting in which a dynamic-length item is of zero length - and not the
      *> receiver's incidental current content length.
      *>
      *> E= GR9a, groups of EQUAL length (5 = 3+0+2 and 1+2+0+2, the dynamic item at relative byte 3 in both).
      *>    "The content of each character position that is not occupied by a corresponding table is moved to
      *>    the corresponding character position in the receiving group": the fixed run ABC|YZ lands as
      *>    RA=A, RC=BC, RB=YZ across DIFFERENT leaf boundaries, and the corresponding dynamic-length items
      *>    move per 8.5.1.10.4 ("the new value becomes the content"), R-D = PQRS.
      *> S= GR9b with the SENDING group SHORTER (fixed run 3 against 7): "each location that occupies the
      *>    excess part is space filled ... 3. All other character positions are filled with space characters"
      *>    - so TB, which lies wholly in the excess part, is four spaces while TA and T-D still take the
      *>    common part per GR9a.
      *> L= GR9b with the SENDING group LONGER (fixed run 7 against 3): "the character positions that occupy
      *>    the excess part are ignored by the operation" - UA takes MNO, U-D takes the dynamic content, and
      *>    the sender's trailing WXYZ is dropped.
      *> Z= 8.5.1.10.4's zero-length arm reached through GR9: "If the length of the sending operand is zero,
      *>    no data is moved and the new length of the dynamic-length elementary item is set to zero", so
      *>    FUNCTION LENGTH of the receiving dynamic item is 0 after a zero-length sender.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393MVVLG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N2 PIC 9(2).
       01 GS.
          05 SA  PIC X(3) VALUE "ABC".
          05 S-D PIC X DYNAMIC LENGTH.
          05 SB  PIC X(2) VALUE "YZ".
       01 GR.
          05 RA  PIC X(1).
          05 RC  PIC X(2).
          05 R-D PIC X DYNAMIC LENGTH.
          05 RB  PIC X(2).
       01 HS.
          05 HA  PIC X(3) VALUE "JKL".
          05 H-D PIC X DYNAMIC LENGTH.
       01 TG.
          05 TA  PIC X(3) VALUE "999".
          05 T-D PIC X DYNAMIC LENGTH.
          05 TB  PIC X(4) VALUE "8888".
       01 VS.
          05 VA  PIC X(3) VALUE "MNO".
          05 V-D PIC X DYNAMIC LENGTH.
          05 VB  PIC X(4) VALUE "WXYZ".
       01 UG.
          05 UA  PIC X(3) VALUE "777".
          05 U-D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "PQRS" TO S-D.
           MOVE GS TO GR.
           DISPLAY "E=[" RA "][" RC "][" R-D "][" RB "]".
           MOVE "DD" TO H-D.
           MOVE HS TO TG.
           DISPLAY "S=[" TA "][" T-D "][" TB "]".
           MOVE "EE" TO V-D.
           MOVE VS TO UG.
           DISPLAY "L=[" UA "][" U-D "]".
           MOVE "" TO H-D.
           MOVE HS TO TG.
           MOVE FUNCTION LENGTH (T-D) TO N2.
           DISPLAY "Z=[" TA "]" N2.
           STOP RUN.
