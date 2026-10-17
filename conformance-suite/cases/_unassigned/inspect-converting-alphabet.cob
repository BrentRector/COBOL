*>--- conformance-case
*> id: inspect-converting-alphabet
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R38 - 14.9.22.2 Format 4: CONVERTING's operands are {identifier-6|literal-4} TO
      *> {identifier-7|literal-5}. An ALPHABET-name is neither (a SPECIAL-NAMES name is not an
      *> identifier), so GnuCOBOL's CONVERTING-alphabet extension is not ISO in any edition. The
      *> differential's run_misc:1759 exercises exactly this; the flip to WE_REJECT is the
      *> adjudicated divergence.
      *>
      *> The two alphabets name ISO general-format keywords, not the code-names ASCII / EBCDIC this case used
      *> to declare. Those two ARE supported code-name-1 spellings now (12.3.7.3 SR15 leaves the set to the
      *> implementor; owner decision kb/Work PB793 put both in it), but they are kept out of this golden on
      *> purpose: the rule being pinned is that an alphabet-name is not a CONVERTING operand AT ALL, and it
      *> has to hold for every alphabet, so the case names the two the standard itself spells.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R38NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPHA IS STANDARD-1
           ALPHABET BETA IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(10) VALUE "ABC".
       PROCEDURE DIVISION.
           INSPECT X CONVERTING BETA TO ALPHA.
           STOP RUN.
