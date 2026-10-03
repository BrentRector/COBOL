      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1095 - ISO 12.3.7.4 GR12: "the contiguous characters in the native character set beginning with the
      *> character specified by the value of literal-5, and ending with the character specified by the value of
      *> literal-6" (cite.py --check 12.3.7.4 "the contiguous characters in the native character set beginning with the
      *> character specified by the value of literal-5, and ending with the character specified by the value of
      *> literal-6"). Under IN, ordinal 1 of AL = "B" ALSO "A" ALSO "Z" names THREE characters (GR7 k6), so there is no
      *> ONE character to begin the run: the specification defines no such run and the bound is refused, naming GR12,
      *> rather than silently taking literal-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1095TH.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "B" ALSO "A" ALSO "Z" "C" "D"
           CLASS C1 IS 1 THRU 3 IN AL.
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
