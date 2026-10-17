      *>--- conformance-case
      *> id: pb3233_inspect_figurative_pattern_size
      *> rules: [SR-14.9.22.3-3, SR-14.9.22.3-6, SR-14.9.22.3-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3233 - ISO 14.9.22.3 SR3, SR6 and SR9. A figurative
      *> literal-1 or literal-4 "refers to an implicit one character
      *> data item" (SR3), so SR6 / SR9 measure it as ONE character:
      *> a one-character literal-3 / literal-5 is the same size and
      *> compiles. Only a figurative literal-3 / literal-5 is exempt
      *> from the size rule (14.9.22.4 GR14 / GR22 size it at run time).
      *> Each leg fails if the size rule refuses an equal-size pair
      *> or the replacement is not applied:
      *>   X  ALL SPACE BY "Q"           "A B C " -> "AQBQCQ"
      *>   Y  CONVERTING SPACE TO "R"    "A B C " -> "ARBRCR"
      *>   Z  ALL STAR BY "-"  (STAR is the symbolic character for
      *>      ordinal 43 of the native set, "*")   -> "A-B-C-"
      *>   V  CONVERTING "AB" TO SPACE   (GR22: SPACE is two spaces)
      *>      "ABCABC" -> "  C  C"
      *>   W  ALL "AB" BY ZERO (GR14: ZERO is two zeros)
      *>      "ABCABC" -> "00C00C"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3233FP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS STAR IS 43.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "A B C ".
       01 Y PIC X(6) VALUE "A B C ".
       01 Z PIC X(6) VALUE "A*B*C*".
       01 V PIC X(6) VALUE "ABCABC".
       01 W PIC X(6) VALUE "ABCABC".
       PROCEDURE DIVISION.
           INSPECT X REPLACING ALL SPACE BY "Q".
           DISPLAY "X=[" X "]".
           INSPECT Y CONVERTING SPACE TO "R".
           DISPLAY "Y=[" Y "]".
           INSPECT Z REPLACING ALL STAR BY "-".
           DISPLAY "Z=[" Z "]".
           INSPECT V CONVERTING "AB" TO SPACE.
           DISPLAY "V=[" V "]".
           INSPECT W REPLACING ALL "AB" BY ZERO.
           DISPLAY "W=[" W "]".
           STOP RUN.
