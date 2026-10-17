*>--- conformance-case
*> id: pb452_set_content_float
*> rules: [SR-14.9.39.3-32, GR-14.9.39.4-33, GR-14.9.39.4-34, GR-14.9.39.4-35, DOC-A.1-176]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.39.2 Format 15 (numeric-content), general rules 33, 34 and 35 - the three non-finite
      *> values SET CONTENT stores into a STANDARD floating-point item. kb/Work PB452.
      *>
      *> GR33 "If FLOAT-INFINITY is specified, the content of identifier-14 is set to a canonical representation
      *> of infinity as described in ISO/IEC 60559:2020, Clause 3, for the basic interchange format
      *> corresponding to the usage of identifier-14. If the SIGN phrase is specified, the sign of the content is
      *> set according to the SIGN specification, otherwise the sign is positive." GR34 and GR35 say the same of
      *> a quiet and of a signaling NaN, each "with the payload set to an implementor-defined value" - the
      *> Annex A.1 item 176 obligation, discharged in docs/CONFORMANCE.md 7 under DOC-A.1-176.
      *>
      *> 14.9.39.3 SR32 confines identifier-14 to a STANDARD floating-point usage (3.166 float-binary-32/-64/-128,
      *> 3.167 float-decimal-16/-34); FLOAT-LONG and COMP-2 are floating-point but NOT standard floating-point and
      *> are refused (negative/pb452-set-content-not-standard-float). FLOAT-BINARY-32 and FLOAT-BINARY-64 are the
      *> two this implementation provides - binary32 and binary64 exactly.
      *>
      *> WHAT IS OBSERVED, AND WHY THESE OBSERVATIONS. A conforming program cannot DISPLAY a non-finite value
      *> (14.6 EC-DATA-NOT-FINITE), so each line asserts a RELATION, which 21.x/8.8.4.2 evaluate for a standard
      *> floating-point operand consistently with ISO/IEC 60559:
      *>   1  two receivers of ONE statement both hold +infinity, and +infinity equals +infinity.
      *>   2  GR33's sign: -infinity is less than +infinity, so SIGN NEGATIVE reached the sign bit.
      *>   3  GR34: a quiet NaN is unequal to ITSELF - the defining observable property of a NaN, and proof the
      *>      receiver did not merely take some finite value.
      *>   4  GR35: a signaling NaN is likewise unequal to itself.
      *>   5  a NaN is not an infinity: the binary32 NaN and the binary32 +infinity are unequal.
      *> The QUIET-versus-SIGNALING distinction has no COBOL surface here - the class condition that would expose
      *> it (8.8.4.4's FLOAT-NOT-A-NUMBER-QUIET / -SIGNALING, kb/Work PB225) is not implemented - so it is pinned
      *> bit-for-bit by unit:IeeeSpecialsTests instead, against the same table this emitter writes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB452SETCONTENTFLOAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P64 USAGE FLOAT-BINARY-64.
       01 WS-Q64 USAGE FLOAT-BINARY-64.
       01 WS-N64 USAGE FLOAT-BINARY-64.
       01 WS-M64 USAGE FLOAT-BINARY-64.
       01 WS-S32 USAGE FLOAT-BINARY-32.
       01 WS-I32 USAGE FLOAT-BINARY-32.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CONTENT OF WS-P64 WS-Q64 TO FLOAT-INFINITY
           IF WS-P64 = WS-Q64
               DISPLAY "1:EQ"
           ELSE
               DISPLAY "1:NE"
           END-IF
           SET CONTENT OF WS-N64 TO FLOAT-INFINITY SIGN NEGATIVE
           IF WS-N64 < WS-P64
               DISPLAY "2:LT"
           ELSE
               DISPLAY "2:GE"
           END-IF
           SET CONTENT OF WS-M64 TO FLOAT-NOT-A-NUMBER
           IF WS-M64 = WS-M64
               DISPLAY "3:EQ"
           ELSE
               DISPLAY "3:NE"
           END-IF
           SET CONTENT WS-S32 TO FLOAT-NOT-A-NUMBER-SIGNALING SIGN POSITIVE
           IF WS-S32 = WS-S32
               DISPLAY "4:EQ"
           ELSE
               DISPLAY "4:NE"
           END-IF
           SET CONTENT OF WS-I32 TO FLOAT-INFINITY
           IF WS-S32 = WS-I32
               DISPLAY "5:EQ"
           ELSE
               DISPLAY "5:NE"
           END-IF
           STOP RUN.
