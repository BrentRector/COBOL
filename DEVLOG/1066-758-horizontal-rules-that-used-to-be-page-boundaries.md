## Entry 1066 — 2026-07-28 01:55 PDT — 758 horizontal rules that used to be page boundaries

A printed page boundary was transcribed as a block of three things: a `<a id="page-N">` anchor, a running
header, and a pair of `---` separators around them. De-paging removed the first two. It did not remove the
third, so the document carried **769 horizontal rules that used to mark a page boundary and now marked
nothing** — visible lines cutting through the prose, 287 of them sitting in pairs with nothing whatever
between them.

I nearly talked myself out of removing them, on the theory that some might be deliberate section dividers.
The document settles it: of 2,691 headings, **2,380 have no rule before them**. An 11% minority is residue,
not a house style.

The front matter is a different case and is left alone — its 11 rules separate the title-page blocks and set
off the preface's `About this transcription` note, and a rule dividing two blocks printed on ONE page was
never page furniture. 758 removed from the body; the file is 1,000 lines lighter.

Three invariants gate it: no word changes (409,022 → 408,264, and the difference is exactly the 758 `---`
tokens), no heading is created or destroyed (2,691 → 2,691), and no rule is removed that sits directly under a
paragraph — `---` under a line of text is a SETEXT heading marker, and removing it would silently demote an H2
to a paragraph. Four rules looked like that case and none of them were: each sits under `</pre>` or a
blockquote, which close before it.

Found on the way, and not yet fixed: **five of the fifteen Annex D illustrations are not right.** D.9, D.11
and D.13 were never drawn — what stands in for each is either the flowchart's labels in reading order or a
rough arrow sketch. D.7 has 22 box walls in columns no rule reaches, and D.12 has 2, with its text overflowing
its own border. Their siblings D.10, D.12 and D.14 ARE drawn, which is the odd part: whoever drew these did
alternate figures and stopped.

The house style for them is settled and visible in D.8 — boxes with centred text, connectors meeting the
border at `┬`/`┴`/`├` junctions, branch labels beside the connector, no separate arrowheads. A first draft of
D.11 in that style is measured rather than counted by eye: a box is sized from its content and `put` refuses to
overwrite a non-blank cell. That guard immediately caught the draft drawing its True arrow straight through the
decision box's right wall and its loop line through the bottom box's border — both of which would otherwise
have rendered as a plausible-looking picture.

Gates: lint clean · figures 484/484 · publishable at 47,043 lines · acknowledgment verbatim · 3,811 links,
zero dangling.

