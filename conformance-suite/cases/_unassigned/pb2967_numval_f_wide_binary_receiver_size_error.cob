*>--- conformance-case
*> id: pb2967_numval_f_wide_binary_receiver_size_error
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2967 - the NUMVAL-F family's checked landing: a value past every container is a SIZE ERROR in a
      *> 16-byte COMP-5 receiver (the sibling of the intrinsic-quantizer and COMP-2 legs in the 2002 golden of the
      *> same note).
      *>
      *> NUMVAL-F (15.69) returns the value of its argument, here 1E+39, -1E+39 and 1E+18; 15.69.4 r2 grants the native
      *> value only an approximation of it, and 1E+18 is far inside the container of a PIC S9(19) COMP-5 item (128 bits,
      *> about 1.7E+38), so it STORES.  1E+39 and -1E+39 exceed that container: 14.7.5 case 3 makes the size error
      *> condition exist "if, after radix point alignment and any applicable rounding specifications, the result of
      *> an arithmetic statement is further from zero than permitted for the associated resultant data item"
      *> (cite.py --check 14.7.5 "further from zero than permitted" -> OK 14.7.5 3)), and 14.7.5 rule 1 leaves the
      *> resultant unchanged (cite.py --check 14.7.5 "unchanged" -> OK 14.7.5 1)).
      *> The old checked landing saturated 1E+39 to Int128.MaxValue = 170141183460469231731687303715884105727, which
      *> a 16-byte receiver accepts as a legal value, so the statement took NOT ON SIZE ERROR and stored it.
      *> The PIC S9(18) DISPLAY control always raised and must keep doing so.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2967N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RS PIC S9(19) COMP-5 VALUE 7.
       01 RU PIC 9(19) COMP-5 VALUE 7.
       01 R18 PIC S9(18) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE RS = FUNCTION NUMVAL-F("1E+39")
               ON SIZE ERROR DISPLAY "POSITIVE SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "POSITIVE STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           COMPUTE RS = FUNCTION NUMVAL-F("-1E+39")
               ON SIZE ERROR DISPLAY "NEGATIVE SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "NEGATIVE STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           COMPUTE RU = FUNCTION NUMVAL-F("1E+39")
               ON SIZE ERROR DISPLAY "UNSIGNED SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "UNSIGNED STORED"
           END-COMPUTE
           DISPLAY "RU=" RU
           COMPUTE R18 = FUNCTION NUMVAL-F("1E+39")
               ON SIZE ERROR DISPLAY "DISPLAY SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "DISPLAY STORED"
           END-COMPUTE
           DISPLAY "R18=" R18
           COMPUTE RS = FUNCTION NUMVAL-F("1E+18")
               ON SIZE ERROR DISPLAY "INRANGE SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "INRANGE STORED"
           END-COMPUTE
           DISPLAY "RS=" RS
           STOP RUN.
