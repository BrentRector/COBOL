*>--- conformance-case
*> id: pb3018_native_dec_lane_wide_binary_operand_signed_and_unsigned_agree
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3018 - on the NATIVE arithmetic lane that a decimal
      *>   operand selects (a floating-point literal), a 16-byte binary
      *>   operand is lifted into the standard-decimal intermediate by ONE
      *>   rule whether its container is signed or unsigned.
      *>
      *>   8.8.1.5.2 1): "If the operand value cannot be expressed exactly
      *>   in an SDIDI, the value is rounded according to the rules in
      *>   11.9.11" (cite.py --check 8.8.1.5.2 "If the operand value cannot
      *>   be expressed exactly in an SDIDI, the value is rounded" -> OK
      *>   8.8.1.5.2 1)).  A 16-byte COMP-5 item owns its whole container
      *>   (13.18.60.4 12), "The implementor may allow a wider range",
      *>   cite.py OK; DOC-A.1-123): signed [-2**127, 2**127) and unsigned
      *>   [0, 2**128) both reach 39 digits, so the lift cannot prove the
      *>   value fits 38 digits and ROUNDS it to 34 (DOC-A.1-82) - for the
      *>   unsigned item exactly as for the signed one.  A DISPLAY item of 31
      *>   digits is provably within the lane's 38 exact digits and lifts
      *>   exactly.
      *>
      *>   V = 123456789012345678901234567890123456 (36 digits, held in
      *>   16 bytes as X"0017C6E3BFD70FDEEAEC417172DCBAC0").  Adding the
      *>   floating-point literal 0.0E+0 puts the operation on the decimal
      *>   lane; the operand rounds to 34 digits, 1234567890123456789012345678901235
      *>   with exponent 2, i.e. 123456789012345678901234567890123500.
      *>   RU and RS are PIC 9(31) / S9(31) COMP-5 (16 bytes); each
      *>   shows its LOW 31 digits, 6789012345678901234567890123500, and
      *>   RHI is the value / 100000 truncated, its HIGH 31 digits
      *>   1234567890123456789012345678901 - identical for both.
      *>   D is the exact control: PIC 9(31) DISPLAY 1234567890123456789012345678901
      *>   plus 0.0E+0 keeps all 31 digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3018A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VU.
          05 WU PIC 9(31) COMP-5.
       01 VUX REDEFINES VU PIC X(16).
       01 VS.
          05 WS PIC S9(31) COMP-5.
       01 VSX REDEFINES VS PIC X(16).
       01 D PIC 9(31) VALUE 1234567890123456789012345678901.
       01 RU PIC 9(31) COMP-5.
       01 RS PIC S9(31) COMP-5.
       01 RDX PIC 9(31).
       01 RHI PIC 9(31).
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"0017C6E3BFD70FDEEAEC417172DCBAC0" TO VUX VSX.
           COMPUTE RU = WU + 0.0E+0.
           COMPUTE RHI = RU / 100000.
           DISPLAY "U  " RU " high " RHI.
           COMPUTE RS = WS + 0.0E+0.
           COMPUTE RHI = RS / 100000.
           DISPLAY "S  " RS " high " RHI.
           IF RU = RS DISPLAY "U = S" ELSE DISPLAY "U not = S".
           COMPUTE RDX = D + 0.0E+0.
           DISPLAY "D  " RDX.
           STOP RUN.
