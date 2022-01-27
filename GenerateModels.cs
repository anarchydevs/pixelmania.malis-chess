using System;
using System.Linq;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using System.Collections.Generic;
using AOSharp.Common.GameData.UI;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Newtonsoft.Json;
using System.IO;

namespace MaliChess
{
    public class GenerateModels : AOPluginEntry
    {
        public static Dictionary<Type, List<Edge>> chessPieces = new Dictionary<Type, List<Edge>>();

        public override void Run(string pluginDir)
        {
            try
            {
                Serialize();
                GenerateBoard(Side.White);
                GeneratePlayer(Side.White);
                GenerateEnemy(Side.Black);
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }

        private void Serialize()
        {
            chessPieces = JsonConvert.DeserializeObject<Dictionary<Type, List<Edge>>>(File.ReadAllText($"{UI.PluginDir}\\MeshData\\MeshData.json"));
        }

        private void GenerateBoard(Side side)
        {
            ChessPiece.DefinePiece(Type.BoardBlack, side, new Vector3(0, 0, 0), false);
            ChessPiece.DefinePiece(Type.BoardWhite, side, new Vector3(0, 0, 0), false);
        }

        private void GeneratePlayer(Side side)
        {
            ChessPiece.DefinePiece(Type.Rook, side, new Vector3(0, 0, 0), false);
            ChessPiece.DefinePiece(Type.Knight, side, new Vector3(1, 0, 0), false);
            ChessPiece.DefinePiece(Type.Bishop, side, new Vector3(2, 0, 0), false);
            ChessPiece.DefinePiece(Type.King, side, new Vector3(3, 0, 0), false);
            ChessPiece.DefinePiece(Type.Queen, side, new Vector3(4, 0, 0), false);
            ChessPiece.DefinePiece(Type.Bishop, side, new Vector3(5, 0, 0), false);
            ChessPiece.DefinePiece(Type.Knight, side, new Vector3(6, 0, 0), false);
            ChessPiece.DefinePiece(Type.Rook, side, new Vector3(7, 0, 0), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(0, 0, -1), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(1, 0, -1), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(2, 0, -1), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(3, 0, -1), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(4, 0, -1), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(5, 0, -1), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(6, 0, -1), false);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(7, 0, -1), false);
        }

        private void GenerateEnemy(Side side)
        {
            ChessPiece.DefinePiece(Type.Rook, side, new Vector3(7, 0, -7), true);
            ChessPiece.DefinePiece(Type.Knight, side, new Vector3(6, 0, -7), true);
            ChessPiece.DefinePiece(Type.Bishop, side, new Vector3(5, 0, -7), true);
            ChessPiece.DefinePiece(Type.Queen, side, new Vector3(4, 0, -7), true);
            ChessPiece.DefinePiece(Type.King, side, new Vector3(3, 0, -7), true);
            ChessPiece.DefinePiece(Type.Bishop, side, new Vector3(2, 0, -7), true);
            ChessPiece.DefinePiece(Type.Knight, side, new Vector3(1, 0, -7), true);
            ChessPiece.DefinePiece(Type.Rook, side, new Vector3(0, 0, -7), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(7, 0, -6), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(6, 0, -6), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(5, 0, -6), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(4, 0, -6), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(3, 0, -6), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(2, 0, -6), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(1, 0, -6), true);
            ChessPiece.DefinePiece(Type.Pawn, side, new Vector3(0, 0, -6), true);
        }
    }
}
