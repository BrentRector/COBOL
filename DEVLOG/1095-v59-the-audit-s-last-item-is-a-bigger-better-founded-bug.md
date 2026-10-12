## Entry 1095 — 2026-07-28 23:00 PDT — V59: the audit's last item is a bigger, better-founded bug than the audit said

V59 was adjudicated as implementor latitude: the byte-level representation of BINARY/PACKED is explicitly
implementor-defined (§13.18.60.4 GR4/GR11, all three citations re-verified with `cite.py` — unlike CA14's, these
were sound), values round-trip faithfully through the zoned digit image, and the divergence is observable only by
punning raw bytes. The recommendation was option B — a Tier-C `byte[]` canonical via a new `RedefCodec` — with
option A, keep the zoned image and document it, acceptable.

Three things came out of scoping it, and each one moved the answer.

**1. Fixing only Tier C would have built the anti-pattern.** The whole-group image (§14.4) and the file-record
path use the SAME zoned representation. Implementing option B where V59 aimed it would give one COMP leaf
radix-2 bytes when punned through REDEFINES and zoned bytes when written to a file — two mechanisms for one job.
I put the fork to the owner rather than pick it, and the answer was ONE representation at every byte boundary.
Measured first: a `PIC 9(4) COMP` and a `PIC 9(4) COMP-3` written to a sequential file both come out as ASCII
`31 32 33 34`, byte-identical to the DISPLAY field beside them. Our data files are interchangeable with nothing.

**2. There was no representation to choose — we had already chosen it, and the image ignores it.** I was about to
survey GnuCOBOL/IBM/MF for a width table (their corpus exercises `binary-size` = `1--8`, `2-4-8` and `1-2-4-8`,
byte order big-endian and native, so the latitude is real). Then I found `PicInfo.StorageWidth`: BINARY already
pinned at 1-2-4-8, PACKED at `Digits/2+1` BCD, documented on `DataItem.ByteWidth` and reported by `FUNCTION
BYTE-LENGTH`. Inventing a second table would have been the same defect one layer down. The fix is to make the
image agree with the widths the compiler already publishes.

**3. And that makes V59 a conformance defect, not latitude.** The original adjudication weighed only the byte-pun
view, where GR4/GR11 do give cover. But:

    05 G-COMP PIC 9(4) COMP.  05 G-PACK PIC 9(4) COMP-3.
    FUNCTION BYTE-LENGTH(G) = 5      FUNCTION LENGTH(G) = 8      REDEFINES G PIC X(8) accepted

§15.14.4 r1 returns the length in BYTES; §15.50.4 r3 returns it in ALPHANUMERIC CHARACTER POSITIONS. In a
single-byte-character model those cannot disagree — and a conforming program observes the disagreement with no
file, no REDEFINES and no byte pun at all. The latitude GR4 grants is over the REPRESENTATION; it does not
license the compiler to give two different answers for the size of one group. That argument is independent of
the interchange rationale, and it is what turns V59 from an architecture preference into a bug.

**What the fix is, and is not.** Not `RedefCodec`, and not Tier C — which stays unrealized, its float/COMP-5/INDEX
reject list untouched. The whole-group image is a Latin-1 `string`, and files, SORT and the Tier-B REDEFINES
backing all consume that one image; giving a BINARY/PACKED leaf its TRUE bytes there fixes every boundary through
the mechanism that already exists. The cost is that the leaf's image WIDTH moves from `Pic.Digits` to
`StorageWidth`, and `ImageWidth` has 129 references across 30 files. §14.4 had already flagged this axis when it
rejected leading-separate-sign images as something that "would change `ImageWidth` and every offset computation".

**The corpus blast radius is ZERO, which is the uncomfortable part.** No conformance golden and no NIST program
has a COMP or COMP-3 field inside an FD record. Nothing would have gone red under either representation. The
file-record byte form for the two most common non-display usages in real COBOL is completely untested, and that
is the actual reason this survived — not the ambiguity in GR4.

I stopped at the design boundary deliberately. The analysis is verified and recorded (queue V59, plan §0 NEXT 1,
and a superseding note on `COBOLNET_DESIGN.md` §14.4, whose "named loser (c)" had rejected raw bytes on the
premise that "cross-engine file compatibility is not required" — the premise the owner has now reversed). Starting
a 129-reference storage-model refactor at the end of a session and leaving it half-applied would have been the
worse outcome. Gates unchanged and green: Conformance 4113/4113.

