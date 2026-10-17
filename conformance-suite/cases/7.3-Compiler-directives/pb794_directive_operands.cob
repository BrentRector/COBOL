*>--- conformance-case
*> id: pb794_directive_operands
*> rules: [SR-7.3.3-4, SR-7.3.3-5, SR-7.3.19.3-2, GR-7.3.19.4-1,
*>    FMT-7.3.9.2, FMT-7.3.18.2, FMT-7.3.19.2, FMT-7.3.20.2,
*>    FMT-7.3.22.2, FMT-7.3.24.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB794P23.
      *> kb/Work PB794 - the CONFORMING half of the compiler-directive operand rule, so the
      *> fix that made seven malformed spellings loud cannot have made the legal ones loud
      *> too. The negative twins are negative/pb794-source-format-unknown and
      *> negative/pb794-source-format-literal.
      *>
      *> WHAT IS EXERCISED, AND WHY EACH LINE IS CONFORMING:
      *>   7.3.3 SR3/SR4 - a compiler directive "may be followed only by space characters
      *>     and an optional inline comment", so every directive here carries one. Six
      *>     stages sliced their own operand and none of them knew that: >>PROPAGATE ON *> c
      *>     was REJECTED, and >>SOURCE FORMAT FIXED *> c was not recognized AT ALL, which
      *>     left the following segment to be read in the wrong reference format.
      *>   7.3.3 SR5 - the space after the >> indicator is optional; both spellings appear.
      *>   7.3.24.2 - FORMAT and IS are optional words (not underlined), FIXED and FREE are
      *>     required; both switches below are written with an inline comment on them.
      *>   7.3.18.2 - LISTING's ON is NOT underlined, so per 5.2.3 the ON alternative may be
      *>     selected with the word omitted: a bare >>LISTING is conforming (7.3.18.3 GR4/GR5
      *>     call this "specifying or implying the ON phrase"). 7.3.18.3 GR1 then makes the
      *>     directive a no-op here, because this compiler produces no source listing.
      *>   7.3.23.2 - likewise for REF-MOD-ZERO-LENGTH's ON. No reference modification is
      *>     written, so the implied ON changes nothing observable.
      *>   7.3.22.2 / 7.3.20.2 - PUSH and POP take { directive-name | ALL }; LISTING is a
      *>     compiler-directive name 7.3.22.3 SR1 admits (it excludes only EVALUATE, IF,
      *>     PAGE, POP and PUSH). 7.3.22.4 GR3 keeps the pushed directive's effect active
      *>     and 7.3.20.4 GR1 restores it, neither of which touches run-unit data.
      *>   7.3.9.2 - CALL-CONVENTION { COBOL | call-convention-name-1 }; COBOL is 7.3.9.3
      *>     GR1's default, so writing it changes nothing.
      *>   7.3.19.2 - PAGE takes optional comment-text-1, which 7.3.19.3 SR2 says is "not
      *>     checked syntactically"; 7.3.19.4 GR3 makes it a no-op with no listing.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC: every directive above is defined to have no
      *> effect on run-unit data or on the display device, so the three DISPLAYs report the
      *> unchanged W-N, one from each reference-format segment:
      *>   A=5   fixed-form segment (the file's initial format)
      *>   B=5   free-form segment, entered by the first >>SOURCE FORMAT switch
      *>   C=5   fixed-form again, after the second switch
      *> Directives sit at COLUMN 8 in the fixed segments (column 7 is the indicator area)
      *> and at column 1 in the free segment.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
       >>LISTING *> the ON phrase implied - 7.3.18.2 leaves ON un-underlined
       >>CALL-CONVENTION COBOL *> 7.3.9.3 GR1's own default
       >>PAGE PB794 - comment-text-1 is not checked syntactically (7.3.19.3 SR2)
           DISPLAY "A=" W-N
       >>PUSH LISTING *> a directive-name operand, 7.3.22.3 SR1
       >>POP LISTING *> and its restore, 7.3.20.4 GR1
       >>SOURCE FORMAT FREE *> switch the following segment to free form
DISPLAY "B=" W-N
>> SOURCE FORMAT IS FIXED *> 7.3.3 SR5: the space after >> is optional
       >>REF-MOD-ZERO-LENGTH *> the ON phrase implied - 7.3.23.2 leaves ON un-underlined
           DISPLAY "C=" W-N
           STOP RUN.
