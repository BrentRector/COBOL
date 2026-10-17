*>--- conformance-case
*> id: pb8_refmod_function_result
*> rules: [SR-8.4.3.3.3-2, SR-8.4.3.3.3-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB8 - REFERENCE-MODIFYING A FUNCTION RESULT (ISO 8.4.3.3.3 SR2).
      *> 8.4.3.1.2 Format 3 composes an identifier from `identifier-1 reference-modifier-1`, and SR2 admits a
      *> function-identifier as identifier-1: "If identifier-1 is a function-identifier, it shall reference an
      *> alphanumeric, boolean, or national function." 8.4.3.1.4 GR1 fixes the order - (f) the argument list
      *> binds to the name, THEN (g) "a reference modifier applies to the identifier on the left".
      *>
      *> Every one of these was a COBOL0001 parse error or a silent wrong answer before this fix:
      *>   FUNCTION name (r)          - zero-argument function, ref-modified          - parse error
      *>   FUNCTION name(args) (r)    - argument list, then ref-modified               - parse error
      *>   name (r)                   - keyword-omitted zero-argument, ref-modified    - wrong diagnostic (1543)
      *>   name(args) (r)             - keyword-omitted, then ref-modified             - COMPILED CLEAN, threw
      *>                                                                                 at RUN TIME
      *> The standard writes the third shape itself at D.14.3.6:
      *>   MOVE FUNCTION LOCALE-DATE (CURRENT-DATE (1:8)) TO a-date-field.
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE SPEC, and where the value is a clock they are STRUCTURAL - the
      *> PB7 convention. 15.21.3 fixes CURRENT-DATE at 21 character positions, so 8.4.3.3.4 item 5c makes
      *> (1:8) exactly 8 positions and the omitted-length (5:) exactly 21-5+1 = 17. The alphanumeric cases
      *> use UPPER-CASE/LOWER-CASE (15.97/15.57), whose results are fixed by their arguments.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB8REFMODFN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T   PIC X(3).
       01 W   PIC X(8).
       01 L   PIC 9(3).
       01 IX  PIC 9(2) VALUE 2.
       PROCEDURE DIVISION.
      *> 1 - FUNCTION keyword, argument list, then a ref-mod. 15.97.3 r1: UPPER-CASE("abcdefgh") is
      *> "ABCDEFGH"; 8.4.3.3.4 item 5b/5c: (2:3) is the 3 positions from position 2 = "BCD".
           MOVE FUNCTION UPPER-CASE("abcdefgh") (2:3) TO T
           DISPLAY "1=" T

      *> 2 - the same reference with FUNCTION omitted (8.4.3.2.3 SR2 + REPOSITORY ALL INTRINSIC). The two
      *> reference forms are the SAME reference and must give the same value.
           MOVE UPPER-CASE("abcdefgh") (2:3) TO T
           DISPLAY "2=" T

      *> 3 - a ZERO-ARGUMENT function, ref-modified. 8.4.3.2.3 SR6 gives the '(' to the argument list only
      *> when the function's definition PERMITS arguments; CURRENT-DATE (15.21.2) permits none, so this '('
      *> is the reference modifier. Length is structural: 15.21.3 = 21 positions, so (1:8) is 8.
           COMPUTE L = FUNCTION LENGTH(FUNCTION CURRENT-DATE (1:8))
           DISPLAY "3=" L

      *> 4 - the D.14.3.6 shape: the same zero-argument ref-mod with FUNCTION omitted, nested as an argument.
           COMPUTE L = FUNCTION LENGTH(CURRENT-DATE (1:8))
           DISPLAY "4=" L

      *> 5 - the OMITTED length, "to the end" (8.4.3.3.4 item 5c): 21 - 5 + 1 = 17.
           COMPUTE L = FUNCTION LENGTH(FUNCTION CURRENT-DATE (5:))
           DISPLAY "5=" L

      *> 6 - a ref-modified result used as an ARGUMENT to another function (8.4.3.2.3 SR8 admits an
      *> identifier, and Format 3 makes a ref-modified function-identifier one).
           MOVE FUNCTION LOWER-CASE(FUNCTION UPPER-CASE("abcdefgh") (2:3)) TO T
           DISPLAY "6=" T

      *> 7 - the positions are ARITHMETIC EXPRESSIONS, not just literals (8.4.3.3.3 SR4: "leftmost-position
      *> and length shall be arithmetic expressions"). IX = 2, so (IX + 1 : 3) is (3:3) = "CDE".
           MOVE FUNCTION UPPER-CASE("abcdefgh") (IX + 1:3) TO T
           DISPLAY "7=" T

      *> 8 - the whole result, ref-modified from position 1 with the length omitted: the identity slice.
           MOVE FUNCTION UPPER-CASE("abcdefgh") (1:) TO W
           DISPLAY "8=" W

      *> 9 - a NATIONAL function is admitted by SR2 alongside alphanumeric. 15.66 NATIONAL-OF returns a
      *> national result; 8.4.3.3.4 GR1/GR6 count its positions in national character positions.
           MOVE FUNCTION DISPLAY-OF(FUNCTION NATIONAL-OF("abcdefgh") (2:3)) TO T
           DISPLAY "9=" T

      *> 10-12 - THE OPERAND CHANNELS. 8.4.3.3.3 SR5: "reference modification is allowed anywhere an identifier
      *> referencing a data item of class alphanumeric, boolean, or national is permitted", and 8.4.3.1.2
      *> Format 1 makes a function-identifier an identifier - so every general-operand position admits one.
      *> Each of these binds through a DIFFERENT entry point (AcceptDisplayBinder, StringUnstringBinder, the
      *> condition binder), which is exactly what a wrapper node around the call would have broken silently.
           DISPLAY "10=" FUNCTION UPPER-CASE("abcdefgh") (2:3)
           MOVE SPACES TO W
           STRING FUNCTION UPPER-CASE("abcdefgh") (2:3) DELIMITED BY SIZE
                  "-" DELIMITED BY SIZE INTO W
           DISPLAY "11=" W
           IF FUNCTION UPPER-CASE("abcdefgh") (2:3) = "BCD"
              DISPLAY "12=OK"
           ELSE
              DISPLAY "12=BAD"
           END-IF
           STOP RUN.
