      *> ISO §8.1.3.2 GR2 (Annex A.1 item 26) — more than one encoding in
      *> one compilation group: the documented method is PER FILE, by
      *> byte-order mark, with no control functions.
      *>   cite.py --check 8.1.3.2 "If more than one encoding is permitted
      *>     in a single compilation group, the implementor shall specify
      *>     the control functions or other methods used for
      *>     distinguishing between encodings." -> OK 8.1.3.2 2)
      *> THE DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-26): every
      *> file of a compilation group - the source file and each library
      *> text a COPY brings in - is decoded independently; a file that
      *> begins with a byte-order mark is read in the encoding the mark
      *> names (UTF-8 EF BB BF, UTF-16 LE FF FE / BE FE FF, UTF-32); a
      *> file with no mark is read as UTF-8; the mark is not source text.
      *> THIS FILE is UTF-8 with NO byte-order mark. Its five sibling
      *> library texts each carry a mark and one 01 entry:
      *>   L1ENC16L.cpy  UTF-16LE + BOM   01 V16L PIC X(3) VALUE "O€K".
      *>   L1ENC16B.cpy  UTF-16BE + BOM   01 V16B PIC X(3) VALUE "AéB".
      *>   L1ENC32L.cpy  UTF-32LE + BOM   01 V32L PIC X(3) VALUE "Ω€Z".
      *>   L1ENC32B.cpy  UTF-32BE + BOM   01 V32B PIC X(3) VALUE "ÆØÅ".
      *>   L1ENC8B.cpy   UTF-8    + BOM   01 V8B  PIC X(3) VALUE "äöü".
      *> DERIVATION of the one .out line: each file decoded in its own
      *> mark's encoding yields exactly the characters written above, and
      *> every one of them is a single character position of the UTF-16
      *> repertoire (DOC-A.1-25), so each PIC X(3) holds its literal
      *> exactly; V8N is this file's own no-BOM UTF-8 literal. A file
      *> decoded in the WRONG encoding cannot produce these values (a
      *> UTF-16/32 file read as UTF-8 is NUL-riddled and does not even
      *> parse), and a BOM read as text would be a stray character ahead
      *> of the level number. DISPLAY separates the six values with "|".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ENC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY L1ENC16L.
       COPY L1ENC16B.
       COPY L1ENC32L.
       COPY L1ENC32B.
       COPY L1ENC8B.
       01 V8N PIC X(3) VALUE "é€!".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY V16L "|" V16B "|" V32L "|" V32B "|" V8B "|" V8N.
           STOP RUN.
