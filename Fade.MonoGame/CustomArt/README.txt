FISH FISH POP - CUSTOM ART
==========================

Pictures in this folder replace the game's own art. Put a file here with one
of the names below, start the game, and that fish uses your picture.
Delete the file (or rename it) to get the original back.

The game only reads this folder when it starts, so restart the game after
adding or changing a picture.


THE RULES FOR EVERY PICTURE
---------------------------

  * PNG files, with a TRANSPARENT background.
    The game does not cut out a white background. A picture saved without
    transparency shows up as a white square on the board.
    (Affinity: File > Export > PNG, and have no background layer, or hide it.)

  * Square. 32 x 32 pixels is the size the game draws a fish at on the board.
    - 32 x 32  is drawn pixel for pixel. This is the sharpest.
    - 16 x 16  also works: every pixel is drawn twice as big, the way the
               game's older art is.
    - Any other square size works, but shrinks less evenly.
    Some places draw fish smaller than that (the little fish in the
    background, quest icons), and those keep only some of the pixels.

  * Leave a little space round the edge. A fish fills its whole square on
    the board, and neighbours touch.

  * The file name must match exactly, including capital letters and ".png".


A BLACK LINE AROUND IT: "_b"
----------------------------

The game's own fish have a dark outline drawn into the picture. If yours
does not, add "_b" to the end of the name, before ".png":

    Pufferfish_b.png

and the game draws a black line around the fish for you: one pixel of a
16 x 16 picture wide (two pixels of a 32 x 32 one), just outside the
picture, on every side where the picture touches empty space. If your
picture has its own outline already, leave "_b" off, or it gets two. The
line turns and tilts along with the fish. It can go one pixel past the edge of the square, so a fish
that touches the edge still gets its line there.

If there is both a "_b" file and a plain one, the "_b" one is used.
Every file below can have "_b", except Ghost.png.


THE FILES
---------

  File name          What it is                       When it is on the board
  -----------------  -------------------------------  -----------------------------
  Pufferfish.png     pufferfish                       every game
  Catfish.png        catfish                          every game
  Goldfish.png       goldfish                         every game
  Angelfish.png      angelfish                        every game
  Crab.png           crab                             every game
  Bass.png           bass                             every game
  Worm.png           worm                             every game
  Jellyfish.png      jellyfish                        with the jellyfish card
  Stingray.png       stingray                         with the stingray card
  Bottle.png         message in a bottle              with the bottle card
  Krill.png          krill                            during a quest
  Seaweed.png        seaweed                          during a quest
  SandDollar.png     sand dollar                      during a quest
  Turtle.png         turtle                           during a quest
  GlassFish.png      glass fish                       during a quest
  Bones.png          skeleton (a pile of bones)       a fish stung twice by jellyfish
  Ghost.png          the ghost that rises from bones  when bones are matched

The seven every-game fish, Jellyfish and Stingray are 32 x 32 in the game
(Bass is a seahorse now, and Worm a starfish); the rest are still 16 x 16.

Watch for pure white pixels at the corners of an outline: the last batch's
"_b" files had a few, and they show as white specks on the board.

The game has no "happy" picture for a fish yet; only one picture per fish is
used.


CHECKING THAT IT WORKED
-----------------------

The game writes a line for every picture it took from this folder to its
log, for example:

  [fade] texture 1 loaded from file '...\CustomArt\Pufferfish.png' (16 x 16)

The log is log.txt in %APPDATA%\FishFishPop on Windows, and in
~/Library/Application Support/FishFishPop on a Mac.
A picture that is there but could not be read gets a line too.
