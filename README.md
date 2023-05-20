
# Custom Tetris Trainer
**Visual Demo**  
In the following video I briefly go through each implemented mode and some features of the game.
https://youtu.be/90ZTzu2Rxnw

**Description and Goals**  

This is a prototype of the classic Tetris game in the **Unity Engine**, with custom art, SFX, and music. Tetris has always been a longtime favorite of mine. However, I've found that many Tetris games don't feel responsive to the player, and in a game where precision of controls is king, bad controls can be very frustrating and unfun to deal with. This is not just relevant to Tetris; good controls are important in almost every single game.  

In this project, I wanted to focus on designing a game that feels fast, accurate, and responsive, allowing a very high threshold of speed and precision for the player. There are a few important mechanics that I implemented to achieve this goal, which I will cover starting with the most crucial system. In addition, I implemented three different modes (zen, sprint, and cheese) that Tetris players typically use to train their skills, as well as an extra fun one (campaign), as a way to prototype some simple ideas I had for new mechanics in the game. A description of the concepts used to achieve these goals are described below for your reference.

**Handling System**  

When you tap left/right, the piece moves one step left/right. When you hold down a key, the pieces begin to slide in that direction. Pretty simple; however, there is an intrinsic delay between when a keypress is considered a 'tap' versus a 'hold'. This encompasses the idea of **delayed auto-shift** (DAS). It's like when you want to type "LOOOOOOOOOL"—the delay between pressing 'O' on your keyboard and getting the initial 'O' and having it start repeating 'OOOOO' is DAS. We can measure this delay in milliseconds.

How quickly does the 'OOOOO' start appearing on your screen? There is a given speed at which each new 'O' appears, which we can call **automatic repeat rate** (ARR). This is how *quickly* the piece slides across the board. We also measure this delay in ms.

The mechanic by which a Tetris piece slowly falls is called gravity. This speed is increased with a soft drop (e.g., holding the down arrow) to make it fall faster. We can call the rate at which this happens **soft drop rate** (SDR), and you can consider it a vertical version of ARR.

There are other handling input factors, but these are the most important ones.  

Depending on what a player sets the DAS, ARR, and SDR to, the pieces can be extremely responsive or very slow. The  key factor is allowing the player to choose what settings they are most comfortable with. For example, I prefer an ARR delay of 0 ms, and an SDR that is either 0 ms or very close. This means that holding left/right will snap my piece to the side (after the DAS delay) and a SDR of 0 means the piece hits the bottom of the board instantly as well. For the purpose of the video demonstration, I had a non-zero SDR so that the piece could be tracked more easily.

**Rotation System**  

Rotation in Tetris is actually quite complicated, and there actually exist many different systems for this. If you used the most naïve implementation of rotation, it is possible for the player to be unable to make certain rotations in situations where they should, most notably when a piece is pressed up against either side of the board (see wall kicks: https://tetris.fandom.com/wiki/Wall_kick)

There is a modern standard that I've implemented, so there is no need to reinvent the wheel here. Details are at this link:
https://tetris.fandom.com/wiki/SRS  

**Bag System**  

The bag system in Tetris involves how the pieces are randomly given to you. In classic Nestris, pieces were actually completely random, and if you were unlucky, it was possible to not get a specific piece for a very long time, known as a 'drought'.  
In modern Tetris, you receive each one of the seven different pieces (T, O, J, L, I, Z, S) in some random order, which is called a 'bag' of pieces. This guarantees a consistent amount of each piece, and at most 12 pieces between two of the same piece. This is super important for consistency, which in turn is also important for a game that feels fast. I've found that it is much easier to make decisions quickly and accurately when you are able to rely on consistency from the randomness present in the piece queue.

There are other systems such as lock delay, piece previews, ghost piece, hold piece, and all-spins, but these are secondary in importance and I won't discuss them here. The Tetris wiki (https://tetris.fandom.com/wiki/Tetris_Wiki) is an excellent place to find more information if needed.

**Visuals and Audio**  

Because this project is more of a study on how to make gameplay mechanics more precise, visuals and audio were secondary in implementation. However, I still put together a basic board and graphics, and accompanied the game with relevant custom made SFX and music. Unsurprisingly, good audio feedback for player actions is very important for a responsive feel, giving the player another way to receive an indication of what is happening when they are pressing keys. In addition, when a player is playing very quickly, it's sometimes much easier to feel the rhythm of piece placements through auditory cues than with visuals. A great example is the lock delay, where there is a short delay between having a piece land on a surface and having it actually lock into place. However, if there's no visual cue that the piece has not been placed yet, players may want to rely on the SFX of a piece locking to know that it has actually been placed.


