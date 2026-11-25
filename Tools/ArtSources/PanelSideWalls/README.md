# Panel side walls

Matching left (west) and right (east) walls derived from the supplied north-wall sprite. These are 8px-thick overhead boundary caps with a silver rim facing the room interior, following SOLITUDE orthogonal room perspective.

- `Tiles32`: six transparent 32×32 tiles; west/east top, repeat, bottom.
- `side_wall_atlas_32.png`: 64×96 atlas, two columns (west/east), three rows (top/repeat/bottom); slice 32×32, zero margins/gutters.
- `Tiles32x64`: two 32×64 repeat modules, each equivalent to two stacked repeat tiles.
- `Strips`: tight 8×32 versions for custom assembly.

Import at **32 PPU**, Point filter, no compression or mipmaps, center pivot. West tiles occupy x24–31 of their cells (room interior to the right); east occupy x0–7 (room interior to the left). Place west tiles in the cell immediately left of the room floor and east tiles immediately right. Paint top, as many repeats as needed, then bottom. Use the 32×32 atlas for a Tile Palette or a later RuleTile setup. Scene layouts have not been generated or changed.

Artwork originates from built-in image editing. The exact prompt, untouched generated source, reference, and processor metadata are retained. Generated source strips were aligned without rescaling, with a plain middle row repeated to fill the processing cells, then reduced exactly 12:1 nearest-neighbor. A source cross-section forms the repeating straight run; source terminal rows close the top/bottom ends. The right side mirrors the left for exact construction consistency. No smoothing, fractional resizing, procedural illustration, or painted-over devices.

Checks: exact dimensions, binary alpha, no magenta residue, matching vertical join pixels, exact mirrored pairs, 3×3 repetitions. Review previews are outside Assets under work/Reviews/PanelSideWalls. The room context uses the supplied north sprite unchanged at 4:1 nearest-neighbor reduction with a neutral background for orientation. These are art-source exports; Unity import, RuleTiles, and in-game validation are not included.

Rebuild final exports using `python3 assemble.py`; the Processed folder retains the sprite processor outputs.
