using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;    

namespace Tetris;
using static Piece;

static class Board
{
    public const int boardY = 20;
    public const int boardX = 10;
    public static int[,] board = new int[boardY, boardX];

    static int[,] filledBoard = new int[boardY, boardX]
    {
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {1,1,1,1,1,1,1,1,1,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0,0,0},
    };

    private static Color[,] colors = new Color[boardY, boardX];

    static Board() => ResetBoard();

    static public bool UpdateBoard(Point[] squaresPos, Color color)
    {
        foreach (Point pos in squaresPos)
        {
            if (pos.X >= boardX || pos.Y >= boardY)
                throw new Exception("out of range pos in UpdateBoard: x=" + pos.X + " y=" + pos.Y);

            board[pos.Y, pos.X] = 1;
            colors[pos.Y, pos.X] = color;
        }

        return BuildRows();

        //print
        /*Console.WriteLine("Board:");
        for (int y = 0; y < board.GetLength(0); ++y)
        {
            for (int x = 0; x < board.GetLength(1); ++x)
                Console.Write(board[y, x] + " ");
            Console.WriteLine();
        }
        Console.WriteLine();*/
    }
    static private bool BuildRows()
    {
        bool boardChanged = false;

        int goToRow = -1;
        for (int y = boardY - 1; y >= 0; --y)
        {
            //How much squares are filled
            int filled = 0;
            for (int x = 0; x < boardX; ++x)
            {
                if (board[y, x] == 1)
                    ++filled;
            }
            //If row is complete and we haven't got a row to go, setting this one to goToRow
            if (filled == boardX)
            {
                MyGame.AddScore();
                if(goToRow == -1)
                {
                    boardChanged = true;
                    goToRow = y;
                }
            }

            //If row isn't complete, moving it to goToRow, and raising goToRow by 1
            if (goToRow >= 0 && filled != boardX)
            {
                for (int rowX = 0; rowX < boardX; ++rowX)
                {
                    board[goToRow, rowX] = board[y, rowX];
                    colors[goToRow, rowX] = colors[y, rowX];
                }

                goToRow -= 1;
            }
        }

        return boardChanged;
    }
    static public void DrawPieces(SpriteBatch spriteBatch, Texture2D squareTexture)
    {
        for (int y = 0; y < board.GetLength(0); ++y)
            for (int x = 0; x < board.GetLength(1); ++x)
                if (board[y, x] == 1)
                {
                    Rectangle rect = new Rectangle(x * squareSize, y * squareSize, squareSize, squareSize);
                    spriteBatch.Draw(squareTexture, rect, colors[y, x]);
                }
    }
    static public void ResetBoard()
    {
        for (int y = 0; y < board.GetLength(0); ++y)
            for (int x = 0; x < board.GetLength(1); ++x)
                board[y, x] = 0;

        for (int y = 0; y < board.GetLength(0); ++y)
            for (int x = 0; x < board.GetLength(1); ++x)
                colors[y, x] = Color.White;
    }
}
