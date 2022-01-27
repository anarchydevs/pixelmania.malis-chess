using System;
using System.Linq;
using AOSharp.Core;
using AOSharp.Common.GameData;
using System.Collections.Generic;

namespace MaliChess
{
    public class ChessPiece
    {
        public Vector3 PlacedPos;
        public Vector3 RestPosition;
        public Vector3 Position;
        public Vector3 LocalPosition;
        public Vector3 Move = Vector3.Zero;
        public bool Static = false;
        public bool Selected = false;
        public bool FirstMove = true;
        public bool MoveMode = false;
        public Type Type;
        public Vector3 Color;
        public int EnemyInstance;
        public int HostInstance;
        public bool Eaten = false;
        private Vector3 _playerPos;
        public List<Edge> _mesh = new List<Edge>();
        public static void DefinePiece(Type type, Side side, Vector3 position, bool flipped)
        {
            ChessPiece chessPiece = new ChessPiece();
            chessPiece.Type = type;
            chessPiece.Color = side == Side.White ? new Vector3(1f, 1f, 1f) : new Vector3(0f, 1f, 1f);
            chessPiece.LocalPosition = position;
            GenerateModels.chessPieces.TryGetValue(type, out List<Edge> dictMesh);

            foreach (Edge edge in dictMesh)
            {
                Edge _edge = new Edge();
                _edge.firstPos = edge.firstPos;
                _edge.secondPos = edge.secondPos;
                chessPiece._mesh.Add(_edge);
            }

            if (flipped)
            {
                foreach (Edge edge in chessPiece._mesh)
                {
                    edge.firstPos.X = -edge.firstPos.X;
                    edge.firstPos.Z = -edge.firstPos.Z;
                    edge.secondPos.X = -edge.secondPos.X;
                    edge.secondPos.Z = -edge.secondPos.Z;
                }
            }

            if (side == Side.White)
                Gameplay.ChessPiecesWhite.Add(chessPiece);
            else
                Gameplay.ChessPiecesBlack.Add(chessPiece);

        }

        public void RenderModel()
        {
            if (HostInstance == 0)
                return;

            Vector3 _Color;

            if (Selected)
                _Color = new Vector3(0f, 1f, 0f);
            else
                _Color = Color;

            if (Type == Type.BoardBlack)
                _Color = new Vector3(0f, 1f, 1f);
            else if (Type == Type.BoardWhite)
                _Color = new Vector3(1f, 1f, 1f);


            if (!Static)
            {
                _playerPos = DynelManager.Players.First(x => x.Identity.Instance == HostInstance).Position;
            }
            else
            {
                if (EnemyInstance != 0)
                    _playerPos = DynelManager.Players.First(x => x.Identity.Instance == EnemyInstance).Position - LocalPosition;
                else
                    _playerPos = PlacedPos;
            }

            Position = _playerPos + LocalPosition;

            if (!MoveMode)
                RestPosition = Position;
            else
                Position = DynelManager.LocalPlayer.Position;

            if (Move != Vector3.Zero)
            {
                if (Move - _playerPos != LocalPosition) // pawn first move logic
                    FirstMove = false;

                LocalPosition = Move - _playerPos;
                Position = Move;
                Move = Vector3.Zero;
            }


            foreach (Edge edge in _mesh)
                Debug.DrawLine(Position + edge.firstPos, Position + edge.secondPos, _Color);

        }
    }
    [Serializable]
    public class Edge
    {
        public Vector3 firstPos;
        public Vector3 secondPos;
    }
    public enum Type
    {
        Pawn = 0,
        Knight = 1,
        Rook = 2,
        Bishop = 3,
        Queen = 4,
        King = 5,
        BoardBlack = 6,
        BoardWhite = 7

    }
    public enum Side
    {
        White = 0,
        Black = 1
    }
}
