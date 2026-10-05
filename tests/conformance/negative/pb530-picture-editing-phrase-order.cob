      *> reject-at: 2023
      *> kb/Work PB530 - ISO 1989:2023 13.18.40.3 SR25, SECOND sentence: "When extended editing sign control
      *> symbols are used and two are specified, the first occurrence of the EDITING phrase shall be for the
      *> leftmost symbol in character-string-1 and the second occurrence shall be for the rightmost symbol in
      *> character-string-1." The compiler paired each phrase to its character-1 BY NAME and never consulted
      *> the order, so both entries below used to bind - and the order is not cosmetic: each extended symbol
      *> renders its own literal at its own position, so PO01 rendered MOVE -1.5 as ")001.50(" where the
      *> conforming spelling (positive golden 2023/pb530_picture_editing_order_2023) renders "(001.50)".
      *> Each entry is COBOLNET1984. The EDITING phrase is a COBOL-2023 introduction (Annex E.3.3 item 19),
      *> so 2023 is the only edition at which the rule can bite; the below-2023 gate is the separate golden
      *> negative/pb490-picture-editing-at-85.
      *>
      *> THE READING (kb/Work PB855, replacing PB530's): the sentence is read AS WRITTEN - the first phrase's
      *> character-1 IS character-string-1's leftmost symbol and the second's IS its rightmost - because
      *> 13.18.40.6's "has the same precedence as the 'cs' symbol" sentence is applied to every symbol but another
      *> currency-like one (PictureComposition.Precedence), so Annex D.24's PIC IS L9999.99F is no longer at odds
      *> with Table 10's blank leading-currency-before-trailing-currency cell. PB530 had read "the leftmost
      *> symbol" as the leftmost of the TWO, charging only the phrase order.
      *>
      *> PO01 F999.99L with the 'L' phrase written first - 'F' is the leftmost symbol and its phrase is
      *>     written second.
      *> PO02 L99F with the 'F' phrase written first - the same rule on the other pair of ends.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530POR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PO01 PIC F999.99L EDITING L FOR NEGATIVE IS "("
                            EDITING F FOR NEGATIVE IS ")".
       01 PO02 PIC L99F EDITING F FOR NEGATIVE IS "("
                        EDITING L FOR NEGATIVE IS ")".
       PROCEDURE DIVISION.
           STOP RUN.
