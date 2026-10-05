      *> reject-at: 2023
      *> kb/Work PB855 - ISO 1989:2023 13.18.40.6 (Precedence rules), the closing sentence: "If the EDITING phrase is
      *> specified, the precedence of 'es' as related to Table 10, Format 1 picture symbol order of precedence, has
      *> the same precedence as the 'cs' symbol in the column and row of non-floating insertion symbols." 'es' is the
      *> EXTENDED editing sign control symbol (13.18.40.3 SR12: character-1 written with the FOR phrase), and a
      *> non-floating currency symbol is the first or second symbol of the string (the leftmost column and row of
      *> Table 10) or its last or penultimate symbol (the rightmost) - never between two digit positions. A
      *> character-1 of the FOR form in the middle of the digits therefore has no precedence the table admits:
      *> PE01 99L99 - read as a leading 'cs', the 'L' may follow nothing but a leading sign; read as a trailing 'cs',
      *>     nothing but a trailing sign or CR/DB may follow it, and a '9' does. Before kb/Work PB855 the string
      *>     bound clean and MOVE -12.3 rendered "00(12", a spelling no general rule of the standard describes.
      *> The IS form (simple insertion, rule 3) has no Table-10 precedence and stays legal anywhere:
      *> positive golden 2023/pb528_picture_editing_transparency_2023 N01 (9L9 EDITING L IS ":"). COBOLNET1935, 2023
      *> only (the EDITING phrase is a COBOL-2023 introduction, Annex E.3.3 item 19).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB855MID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PE01 PIC 99L99 EDITING L FOR NEGATIVE IS "(".
       PROCEDURE DIVISION.
           STOP RUN.
