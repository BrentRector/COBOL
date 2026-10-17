*>--- conformance-case
*> id: l1_display_transfer_size
*> rules: [GR-14.9.11.4-2, GR-14.9.11.4-5, DOC-A.1-58]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR2 DISPLAY statement — the implementor-defined SIZE OF A DATA TRANSFER (Annex A.1
      *> item 58, docs/CONFORMANCE.md#DOC-A.1-58).
      *>   python scripts/spec/cite.py --check 14.9.11.4 "The implementor shall define, for each device, the size
      *>   of a data transfer."  ->  OK  §14.9.11.4 2)
      *>
      *> THE DETERMINATION UNDER TEST (row 58): **Unbounded.** The standard display device is a byte stream and
      *> imposes no transfer size, so GR4's arm is ALWAYS the one taken and GR5's splitting arm is unreachable:
      *>   python scripts/spec/cite.py --check 14.9.11.4 "If the device is capable of receiving data of the same
      *>   size as the data item being transferred, then the data item is transferred."  ->  OK  §14.9.11.4 4)
      *>   python scripts/spec/cite.py --check 14.9.11.4 "the data beginning with the leftmost character is
      *>   stored aligned to the left in the receiving device"  ->  OK  §14.9.11.4 5) a)
      *> GR5 a) is what a BOUNDED transfer size would do: an operand larger than one transfer is stored in
      *> pieces, "and the remaining data is then transferred according to General rules 4 and 5" — a second
      *> transfer, and on a line device a second LINE, because GR10 only resets the positioning after the LAST
      *> operand. So the observable of "unbounded" is: however large the sending item, ONE line comes out.
      *>
      *>   line 1  ⛔ THE MEASUREMENT. 2,000 character positions in ONE DISPLAY, emitted as ONE line. Every
      *>           device transfer size a terminal or print device has ever had (80, 120, 132, 256, 512, 1024)
      *>           is smaller than this operand, so any of them would fold it into 2 or more lines under GR5 a)
      *>           and the byte comparison would fail. The content is `ALL "0123456789"`, which fills the
      *>           2,000-position receiver with exactly 200 repetitions — §8.3.3.6.4 GR2, the fixed-length
      *>           association arm ("the string of characters is repeated character by character until the
      *>           size of the resultant string is greater than or equal to the number of character positions
      *>           in the associated data item ... then truncated from the right"); GR3 governs only when the
      *>           length is NOT specified by the context, which a PIC X(2000) receiver specifies. The
      *>           repeated literal makes a fold at ANY column visible, not merely one at a multiple of ten.
      *>   line 2  GR6, the multi-operand sending item: "the size of the sending item is the sum of the sizes
      *>           associated with the operands, and the values of the operands are transferred in the sequence
      *>           in which the operands are encountered WITHOUT MODIFYING THE POSITIONING OF THE DEVICE between
      *>           the successive operands".
      *>           python scripts/spec/cite.py --check 14.9.11.4 "the size of the sending item is the sum of the
      *>           sizes associated with the operands"  ->  OK  §14.9.11.4 6)
      *>           FIVE operands totalling 1 + 5 + 2 + 5 + 1 = 14 positions come out as fourteen contiguous
      *>           characters on one line, with no separator and no repositioning: [ABCDE][ABCDE].
      *>   line 3  the run continued; nothing about the long transfer was diagnosed or truncated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DSPXFR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC X(2000).
       01 MID PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       MAIN.
           MOVE ALL "0123456789" TO BIG
           DISPLAY BIG
           DISPLAY "[" MID "][" MID "]"
           DISPLAY "TAIL=SEEN"
           STOP RUN.
