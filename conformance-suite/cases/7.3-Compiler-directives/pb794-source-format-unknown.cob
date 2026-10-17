*>--- conformance-case
*> id: pb794-source-format-unknown
*> rules: [SR-7.3.3-3, FMT-7.3.24.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB794 - a >>SOURCE FORMAT operand that is neither FIXED nor FREE.
      *>
      *> ISO 7.3.24.2 general format (verified against the PRINTED page, folio 84):
      *>     >> SOURCE FORMAT IS { FIXED }
      *>                        { FREE  }
      *> SOURCE, FIXED and FREE are UNDERLINED (required words); FORMAT and IS are not, so
      *> per 5.2.3 they are optional words. The braces are a plain required choice, so
      *> exactly one of FIXED or FREE shall be written - UNKNOWN is a user-defined word the
      *> directive's syntax does not specify, which 7.3.3 SR6 forbids ("compiler-instruction
      *> is composed of compiler-directive words, system-names, and user-defined words AS
      *> SPECIFIED IN THE SYNTAX OF EACH DIRECTIVE"), and it selects no reference format, so
      *> 7.3.24.3 GR1 has no answer for the text that follows.
      *>
      *> The diagnostic is COBOLNET1911 - ONE producer for every directive whose operand is
      *> a closed word set, read off the source-format-directive-2002 row's directiveOperand
      *> column, never a per-directive code.
      *>
      *> reject-at omits 85: there the line draws COBOLNET0900 instead (the whole 7.3
      *> compiler-directive facility is a COBOL-2002 introduction), and an edition that has
      *> no compiler directives at all has nothing to say about this one's operand.
      *> The positive twin is 2023/pb794_directive_operands.
       >>SOURCE FORMAT UNKNOWN
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB794NU.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X".
           STOP RUN.
