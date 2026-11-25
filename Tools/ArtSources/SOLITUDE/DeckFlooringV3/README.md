# SOLITUDE deck flooring V3

Research-inspired layout: larger replaceable deck plates, open circulation surfaces, and service detail concentrated at maintenance edges. Cool desaturated gray steel supports ivory equipment and walls.

Four 64x64 plate modules (plate, worn, service, drain), plus 32x32 plain infill. Each large plate spans 2x2 native world cells. Tiles32 contains the 17 native cells; the manifest identifies each cell quadrant. Assemble quadrants in order. They are not all randomly interchangeable. Randomize complete plate modules, favoring plain/worn; use service and drain only near corresponding equipment. The preview staggers alternate plate rows by one 32px cell to reduce the square-grid effect. Plain infill can extend plate interiors, but do not place it on a required seam.

Single-pixel seams are owned by the left and top of each plate, avoiding double-width joins. Seam highlights are restrained and uniform. No perspective, noisy texture, hazard striping, or per-tile bolts. Intended material joints remain visible; there are no transparent gaps. All five assets checked in 3x3 repetition. The 17 sliced tiles reassemble their source modules pixel-exactly. Nearest-neighbor exports use six colors and opaque alpha. Source renders are 256x256 per module; native art is 64x64 per plate and 32x32 infill.

PNG contains original renders, native modules, 8x enlargements, atlas, repetition checks, and a static composite with previous wall/vent assets. The context scene is an art review, not a Unity scene or runtime validation. No Unity import.

Model/deck_flooring_v3.blend opens on the deck scene, with earlier scenes preserved. Geometry uses pixel-coordinate shallow relief and flat emission colors under a directly-above orthographic camera. Previous floor studies remain untouched.
