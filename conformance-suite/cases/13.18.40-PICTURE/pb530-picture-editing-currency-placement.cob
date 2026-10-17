*>--- conformance-case
*> id: pb530-picture-editing-currency-placement
*> rules: [SR-13.18.40.3-26]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB530 - ISO 1989:2023 13.18.40.3 SR26, SECOND sentence: "For extended editing sign control,
      *> the currency symbol when used shall be either the leftmost symbol in character-string-1, optionally
      *> preceded by character-1, or the rightmost symbol in character-string-1 optionally followed by
      *> character-1." Like its first sentence, this one needs no rule of its own - Table 10 carries it, and
      *> a declared EDITING character-1 is TRANSPARENT to the walk (kb/Work PB528), which is exactly what
      *> makes the two LEGAL spellings legal (positive golden 2023/pb530_picture_editing_order_2023 entries
      *> E05 'L$999' and E06 '999$L'). This entry is the other half of that determination: transparency does
      *> not RESCUE a currency symbol that is neither leftmost nor rightmost. COBOLNET1935, 2023 only (the
      *> EDITING phrase is a COBOL-2023 introduction, Annex E.3.3 item 19).
      *>
      *> PC01 99$9L - the currency symbol is the third of five symbols. Assigned the leading-currency role
      *>     nothing may precede it but a leading sign; assigned the trailing-currency role nothing but a
      *>     trailing sign or CR/DB may follow it, and a '9' does.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530PCP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PC01 PIC 99$9L EDITING L FOR NEGATIVE IS "(".
       PROCEDURE DIVISION.
           STOP RUN.
