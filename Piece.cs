using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Tetris;

using static Utils;
using static Board;

internal class Piece : ICloneable
{
    public object Clone()
    {
        Piece newPiece = new Piece(PieceType.o, Point.Zero);
        newPiece.squaresPos = (Point[])squaresPos.Clone();
        newPiece.rects = (Rectangle[])rects.Clone();
        newPiece.origin = origin;
        newPiece.color = color;
        return newPiece;
    }

    //Static
    public const int squareSize = 32;
    public const int squareCount = 4;
    
    //Tetraminos
    static readonly int[,] s_tetramino = new int[3, 3] {
        {0,1,1},
        {1,1,0},
        {0,0,0}
    };
    static readonly int[,] z_tetramino = new int[3, 3] {
        {1,1,0},
        {0,1,1},
        {0,0,0}
    };
    static readonly int[,] t_tetramino = new int[3, 3] {
        {0,1,0},
        {1,1,1},
        {0,0,0}
    };
    static readonly int[,] i_tetramino = new int[4, 4] {
        {0,0,0,0},
        {1,1,1,1},
        {0,0,0,0},
        {0,0,0,0}
    };
    static readonly int[,] o_tetramino = new int[2, 2] {
        {1,1},
        {1,1}
    };
    static readonly int[,] j_tetramino = new int[3, 3] {
        {1,0,0},
        {1,1,1},
        {0,0,0}
    };
    static readonly int[,] l_tetramino = new int[3, 3] {
        {0,0,1},
        {1,1,1},
        {0,0,0}
    };

    static readonly Color[] colors = new Color[(int)PieceType.max]
    {
        new Color(102,255,255), //light blue
        new Color(255,0,255),   //pink
        new Color(255,0,0),     //red
        new Color(0,255,0),     //green
        new Color(255,255,0),   //yellow
        new Color(255,153,0),   //orange
        new Color(0,102,255)    //blue
    };

    static int[][,] squareTypes = new int[(int)PieceType.max][,] { i_tetramino, t_tetramino, z_tetramino, s_tetramino, o_tetramino, l_tetramino, j_tetramino };

    public Color Color { get => color; set => color = value; }
    private Color color = Color.Purple;

    public Point[] SquarePos => squaresPos;
    private Point[] squaresPos = new Point[squareCount];

    public Rectangle[] Rects { get => rects; set => rects = value; }
    private Rectangle[] rects = new Rectangle[squareCount];
    private Point origin;
    int[,] plan;

    public Piece(PieceType type, Point position)
    {
        //type = PieceType.l;
        rects = Enumerable.Repeat(new Rectangle(0, 0, squareSize, squareSize), squareCount).ToArray();
        color = colors[Convert.ToInt32(type)];
        origin = position;
        BuildPiece(squareTypes[Convert.ToInt32(type)]);

        if(position.X < boardX)
            while (PieceCollision())
                Move(0, -1);
    }

    public enum PieceType { l, t, z, z2, o, r, r2, max }

    //Moving
    /*public void HorizontalMove(int direciton)
    {
        Move(direciton * squareSize, 0);

        //Left and right wall collision
        foreach (Rectangle square in rectSquares)
        {
            //Rectangle squareRect = new Rectangle(square.X, square.Y, squareSize, squareSize);
            if (square.X < 0) Move(-direciton * squareSize, 0);
            if (square.X >= MyGame.screenSize.X) Move(-direciton * squareSize, 0);
        }
            
        //Other pieces collision
        if(PieceCollision())
            Move(-direciton * squareSize, 0);
    }*/
    /*private void Move(int x, int y)
{
    for (int i = 0; i < squareCount; ++i)
        rectSquares[i].Location += new Point(x, y);

    origin += new Point(x, y);
}*/
    /*public int MoveDown()
{
    Move(0, squareSize);

    if (PieceCollision())
    {
        Move(0, -squareSize);

        //If touching ceiling, game over
        if (WallsCollision() == 2) return 2;
        return 1;
    }

    return WallsCollision(); 
}
*/

    public bool HorizontalMove(int direciton)
    {
        bool moved = true;

        if(direciton == 0)
            return false;

        Move(direciton, 0);

        //Left and right wall collision
        foreach (Point square in squaresPos)
        {
            //Rectangle squareRect = new Rectangle(square.X, square.Y, squareSize, squareSize);
            if (square.X < 0)
            {
                Move(-direciton, 0);
                moved = false;
            }
            if (square.X >= boardX)
            {
                Move(-direciton, 0);
                moved = false;
            }
        }

        //Other pieces collision
        if (PieceCollision())
        {
            Move(-direciton, 0);
            moved = false;
        }

        return moved;
    }
    public int MoveDown()
    {
        Move(0, 1);
        int wallCollision = WallsCollision();

        if (wallCollision == 1 || PieceCollision())
        {
            Move(0, -1);

            //If touching ceiling, game over
            if (WallsCollision() == 2) return 2;

            return 1;
        }

        return wallCollision;
    }
    private void Move(int x, int y)
    {
        for (int i = 0; i < squareCount; ++i)
        {
            squaresPos[i] += new Point(x, y);
            rects[i].Location += new Point(x * squareSize, y * squareSize);
        }
        origin += new Point(x, y);
    }

    //Rotating
    public bool Rotate(int dir)
    {
        Piece rotated = new Piece(PieceType.o, Point.Zero);
        rotated.plan = (int[,])plan.Clone();
        rotated.origin = origin;

        //If copy can rotate, building piece as copy
        if (rotated.TryRotate(dir))
        {
            //print("Can rotate");
            origin = rotated.origin;
            BuildPiece(rotated.plan);
            return true;
        }
        return false;
    }
    private bool TryRotate(int dir)
    {
        //Array rotation
        int w = plan.GetLength(1);
        int h = plan.GetLength(0);

        int[,] rotatedPlan = new int[h, w];

        for (int y = 0; y < h; ++y)
        {
            for (int x = 0; x < w; ++x)
            {
                bool outsideLeft = (x + origin.X < 0);
                bool outsideRight = (x + origin.X >= boardX);
                bool outsideDown = (y + origin.Y >= boardY);

                if (outsideLeft) origin.X = 0;
                if (outsideRight) origin.X = boardX - w;
                if (outsideDown) origin.Y = boardY - h;

                if (dir == 1) rotatedPlan[y, x] = plan[x, h - y - 1];
                if (dir == -1) rotatedPlan[y, x] = plan[w - x - 1, y];
            }
        }
        BuildPiece(rotatedPlan);

        //Raising the piece two times, if still touches anything - cancel rotation
        int upLimit = 0;
        while (PieceCollision())
        {
            Move(0, -1);
            upLimit++;
            if (upLimit > 2) return false;
        }
        return true;
    }

    //Collision
    /*    
    *private int WallsCollision()
    {
        foreach (Rectangle square in rectSquares)
        {
            //Rectangle squareRect = new Rectangle(square.X, square.Y, squareSize, squareSize);

            if (square.Y + square.Height >= MyGame.screenSize.Y)
                return 1; //Collision with ground

            if (square.Y <= 0)
                return 2; //Collision with ceiling
        }

        return 0; //No collision
    }

    private bool PieceCollision()
    {
        foreach (Piece testPiece in pieces)
        {
            if (testPiece == this) continue;

            foreach (Rectangle square in rectSquares)
            {
                //Rectangle squareRect = new Rectangle(square.X, square.Y, squareSize, squareSize);

                foreach (Rectangle testSquare in testPiece.rectSquares)
                {
                    //Rectangle testRect = new Rectangle(testSquare.X, testSquare.Y, squareSize, squareSize);
                    if (testSquare.Intersects(square)) 
                        return true;
                }
            }
        }

        return false;

        //Any squares touching stuff
        foreach (Point square in squares)
        {
            //Our square
            Rectangle squareRect = new Rectangle(square.X, square.Y, squareSize, squareSize);

            //Touching ceiling
            if (squareRect.Y <= 0) return 2;

            //Touching other pieces
            foreach (Piece touchingPiece in pieces)
            {
                if (touchingPiece == this) continue;

                foreach (Point touchingSquare in touchingPiece.squares)
                {
                    Rectangle touchingSquareRect = new Rectangle(touchingSquare.X, touchingSquare.Y, squareSize, squareSize);
                    if (squareRect.Intersects(touchingSquareRect))
                    {
                        //if touching any pieces
                        Move(0, -squareSize);
                        return 1;
                    }
                }
            }

            //Touching ground
            if (squareRect.Y + squareRect.Height >= MyGame.screenSize.Y)
            {
                return 1;
            }
        }

        return 0;
    }
    */

    private int WallsCollision()
    {
        foreach (Point square in squaresPos)
        {
            //Rectangle squareRect = new Rectangle(square.X, square.Y, squareSize, squareSize);

            if (square.Y >= boardY)
                return 1; //Collision with ground

            if (square.Y < 0)
                return 2; //Collision with ceiling
        }

        return 0; //No collision
    }
    private bool PieceCollision()
    {
        foreach (Point pos in squaresPos)
        {
            if (pos.X < 0 || pos.X >= boardX) 
                throw new Exception("posX out of range in PieceCollision: " + pos.X);

            if (pos.Y < 0) continue;

            if (board[pos.Y, pos.X] == 1)
                return true;
        }


        return false;
    }

    //Other
    public void Draw(SpriteBatch spriteBatch, Texture2D squareTexture)
    {
        foreach (Rectangle square in rects)
            spriteBatch.Draw(squareTexture, square, color);
    }
    private void BuildPiece(int[,] plan)
    {
        this.plan = plan;

        int i = 0;
        for (int y = 0; y < plan.GetLength(0); ++y)
        {
            for (int x = 0; x < plan.GetLength(1); ++x)
            {
                if (plan[y, x] == 1)
                {
                    squaresPos[i] = new Point(x + origin.X, y + origin.Y);
                    rects[i].Location = new Point( x * squareSize + origin.X * squareSize,
                                                        y * squareSize + origin.Y * squareSize);

                    ++i;
                }
            }
        }
    }
}
/*
    private Vector2[] rotations = new Vector2[squareCount];
    private Point[][] squaresDirections = new Point[squareCount][]
    {
            *//*l*//* new Point[squareCount] { new Point(-2,2), new Point(-2,1),    new Point(-2,0),   new Point(-2,-1)},
            *//*t*//* new Point[squareCount] { new Point(1,1), new Point(0,1),   new Point(2,1),   new Point(1,0) },      
            *//*z*//* new Point[squareCount] { new Point(0,0), new Point(-1,1),   new Point(0,1),   new Point(1,0) },       
            *//*o*//* new Point[squareCount] { new Point(0,0), new Point(-1,0),    new Point(0,-1),   new Point(-1,-1) }
    };
    private Vector2[][] pieceRotation = new Vector2[squareCount][]
    {
            *//*l*//* new Vector2[squareCount] { new Vector2(0,3),new Vector2(0,2),new Vector2(0,1),new Vector2(0,0) },
            *//*t*//* new Vector2[squareCount] { new Vector2(0,0),new Vector2(0,-1),new Vector2(-1,0),new Vector2(1,0) },
            *//*z*//* new Vector2[squareCount] { new Vector2(0,0),new Vector2(-1,1),new Vector2(0,1),new Vector2(1,0) },
            *//*o*//* new Vector2[squareCount] { new Vector2(-0.5f,-0.5f), new Vector2(0.5f, -0.5f), new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f) }
    };

    Point origin = squares[0];
         
    for (int i =0; i < squares.Length; ++i)
    {
        float radian = -(float)(90 * (Math.PI / 180)) * dir;
        Vector2 rotatedVect = rotations[i].Rotate(radian);

        points[i] = rotatedVect.ToPoint() * new Point(32,32) + origin;

        rotations[i] = rotatedVect;
        rotatedVect.Floor();
        squares[i] = rotatedVect.ToPoint() * new Point(32,32) + origin;
    }
*/