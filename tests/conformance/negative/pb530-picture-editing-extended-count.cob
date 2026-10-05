      *> reject-at: 2023
      *> kb/Work PB530 - ISO 1989:2023 13.18.40.3 SR24, SECOND sentence: "For extended editing sign control
      *> symbols, either one or two extended editing sign control symbols may be used in character-string-1."
      *> A THIRD FOR phrase exceeds that maximum, and nothing counted them: PIC 9L9F9G with three phrases
      *> bound clean and rendered MOVE -12 as "0(1)2]". The bound is what makes SR25's second sentence
      *> well-formed at all - that rule pairs the FIRST phrase with the leftmost symbol and the SECOND with
      *> the rightmost and says nothing about a third. COBOLNET1985; 2023 only (the EDITING phrase is a
      *> COBOL-2023 introduction, Annex E.3.3 item 19).
      *>
      *> An extended symbol is the FOR form ALONE (SR12: "If literal-1 is specified, character-1 is a fixed
      *> editing sign control symbol. If the FOR phrase is specified, character-1 is an extended editing sign
      *> control symbol"), so any number of IS-form simple-insertion phrases is untouched by this rule - the
      *> positive golden 2023/pb528_picture_editing_transparency_2023 keeps that leg honest.
      *>
      *> PX01 9L9F9G with three FOR phrases, in the conforming left-to-right order, so SR25 is satisfied and
      *>     SR24 is the only rule the string breaks.
      *> PX02 L9L9L with ONE FOR phrase (kb/Work PB855): the bound is over SYMBOLS - each fixed occurrence and
      *>     each floating string of one character-1 - not over phrases, so three fixed occurrences of one
      *>     letter are three extended symbols.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530PXC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PX01 PIC 9L9F9G EDITING L FOR NEGATIVE IS "("
                          EDITING F FOR NEGATIVE IS ")"
                          EDITING G FOR NEGATIVE IS "]".
       01 PX02 PIC L9L9L EDITING L FOR NEGATIVE IS "(".
       PROCEDURE DIVISION.
           STOP RUN.
