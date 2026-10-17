      *>--- conformance-case
      *> id: pb873_signed_argument_image_carried_formal
      *> rules: [GR-14.2.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB873 - a SIGNED numeric argument crossing into a
      * separately-compiled program whose formal sees it as characters.
      * ISO 14.2.3 GR8: "If the argument is passed by reference, the
      * activated runtime element operates as if the formal parameter
      * occupies the same storage area as the argument."  GR9 (first
      * branch - no program-specifier, no NESTED phrase): the BY CONTENT
      * record is "of the same length as the argument" and the argument
      * is moved into it "without conversion".  So the formal sees the
      * argument's STORAGE - its operational sign included.
      * GR11: "references to data-name-1 ... are resolved in accordance
      * with their description in the linkage section" - LR is PIC
      * S9(4)V99, so LR is NEGATIVE and SUBTRACT 1 gives -13.34.
      * Sign characters: trailing over-punch, the documented IBM
      * convention (CONFORMANCE.md DOC-A.1-177): -4 is "M", -3 "L".
      * Derived:
      *   -12.34 in S9(4)V99 storage = 00123M (both arms)
      *   BY REFERENCE: LR - 1 = -13.34 -> the caller's SM = 00133M
      *   BY CONTENT: the callee's store never reaches SM - 00133M
      *   SIGN LEADING SEPARATE -123 is the 4 characters "-123"; a
      *   PIC X(4) formal sees them, and its MOVE "+124" is the
      *   caller's storage (GR8) - SL then holds +124.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB873MAIN85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SM  PIC S9(4)V99 VALUE -12.34.
       01 SL  PIC S9(3) SIGN LEADING SEPARATE VALUE -123.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB873REF85" USING BY REFERENCE SM.
           DISPLAY "AFTER-REF " SM.
           CALL "PB873REF85" USING BY CONTENT SM.
           DISPLAY "AFTER-CONTENT " SM.
           CALL "PB873CHR85" USING BY REFERENCE SL.
           DISPLAY "AFTER-CHAR " SL.
           STOP RUN.
       END PROGRAM PB873MAIN85.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB873REF85.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR  PIC S9(4)V99.
       01 LRX REDEFINES LR PIC X(6).
       PROCEDURE DIVISION USING LR.
       SUB-PARA.
           DISPLAY "FORMAL-IMAGE " LRX.
           IF LR < 0
               DISPLAY "FORMAL-NEGATIVE"
           ELSE
               DISPLAY "FORMAL-NOT-NEGATIVE".
           SUBTRACT 1 FROM LR.
           EXIT PROGRAM.
       END PROGRAM PB873REF85.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB873CHR85.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC  PIC X(4).
       PROCEDURE DIVISION USING LC.
       CHR-PARA.
           DISPLAY "CHAR-FORMAL " LC.
           MOVE "+124" TO LC.
           EXIT PROGRAM.
       END PROGRAM PB873CHR85.
