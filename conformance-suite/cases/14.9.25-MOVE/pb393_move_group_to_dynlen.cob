*>--- conformance-case
*> id: pb393_move_group_to_dynlen
*> rules: [GR-14.9.25.4-8]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB393. ISO 1989:2023 14.9.25.4 GR8: "If the sending or receiving item is a dynamic-length
      *> elementary item, the current content of the dynamic-length elementary item is moved or changed as
      *> specified in 8.5.1.10.4, Operations on dynamic-length elementary items." And 8.5.1.10.4: "If a
      *> dynamic-length elementary item is a receiving operand and is not reference-modified, the new value
      *> becomes the content of the item. The new length of the dynamic-length elementary item is determined by
      *> the length of new content", with "If the maximum length is reached, the value is truncated on the
      *> right as necessary."
      *> Neither operand is a variable-length GROUP here, so 14.9.25.3 SR9 and GR9 do not apply at all: the
      *> sender is a plain fixed group (14.9.25.4 GR4's group move) and the receiver is a bare dynamic-length
      *> item. A= the whole 5-character group image with the new length 5; B= the same image truncated on the
      *> right to the LIMIT 3, length 3.
      *> Before this fixture the ONE dynamic-length store lived in the MOVE conversion path, which only
      *> MoveKind.Convert reaches; a GROUP sender routes to the group-to-elementary renderer, which asked for a
      *> whole-group character image the receiver does not have and ABORTED the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB393MVGDL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 F1 PIC X(3) VALUE "ABC".
          05 F2 PIC X(2) VALUE "DE".
       01 WS-D  PIC X DYNAMIC LENGTH.
       01 WS-D3 PIC X DYNAMIC LENGTH LIMIT IS 3.
       01 N2 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE WS-G TO WS-D.
           MOVE FUNCTION LENGTH (WS-D) TO N2.
           DISPLAY "A=[" WS-D "]" N2.
           MOVE WS-G TO WS-D3.
           MOVE FUNCTION LENGTH (WS-D3) TO N2.
           DISPLAY "B=[" WS-D3 "]" N2.
           STOP RUN.
