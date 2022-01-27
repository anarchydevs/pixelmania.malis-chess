using System;
using System.Linq;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using System.Collections.Generic;
using System.Windows.Input;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.ChatMessages;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = AOSharp.Core.Debug;

namespace MaliChess
{
    public class Gameplay : AOPluginEntry
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        public static List<ChessPiece> ChessPiecesWhite = new List<ChessPiece>();
        public static List<ChessPiece> ChessPiecesBlack = new List<ChessPiece>();
        public static bool InMatch = false;
        public static int MatchId;
        public static List<ChessPiece> Player = new List<ChessPiece>();
        private List<ChessPiece> _enemy = new List<ChessPiece>();
        private List<Vector3> _legalMoves = new List<Vector3>();
        private bool _staticBoard = false;
        private bool _keyTrigger = true;
        private bool _keyTrigger2 = true;
        private Vector3 _eatenPieces = new Vector3(9, 0, -8);
        public unsafe override void Run(string pluginDir)
        {
            try
            {
                Chat.WriteLine("MaliChess Loaded");

                Game.OnUpdate += OnUpdate;
                Network.ChatMessageReceived += Network_ChatMessageReceived;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }


        private unsafe void OnUpdate(object s, float deltaTime)
        {
            if (MatchId == 0)
                return;

            foreach (ChessPiece piece in Player)
            {
                piece.RenderModel();

                if (piece.Type == Type.BoardWhite || piece.Type == Type.BoardBlack) // dont do any calculations to the board
                    continue;

                if (Player.Where(x => x.MoveMode == true).FirstOrDefault() == null)
                    piece.Selected = Vector3.Distance(DynelManager.LocalPlayer.Position, piece.Position) < 0.5f && piece.Eaten == false || piece.MoveMode == true ? true : false;

            }

            foreach (ChessPiece piece in _enemy)
            {
                piece.RenderModel();

                if (piece.Type == Type.BoardWhite || piece.Type == Type.BoardBlack) // dont do any calculations to the board
                    continue;
            }

            MoveBoard();

            SelectPiece();
        }
        private void Network_ChatMessageReceived(object s, ChatMessageBody chatMessage)
        {
            if (chatMessage.PacketType == ChatMessageType.VicinityMessage)
            {
                string[] data = ((VicinityMessage)chatMessage).Text.Split(' ');
                string action = data[0];

                string message = ((VicinityMessage)chatMessage).Text;
                uint sender = ((VicinityMessage)chatMessage).Sender;

                if (message == "MCR" && !InMatch && sender != DynelManager.LocalPlayer.Identity.Instance)
                {
                    UI.CreateRequestWindow(((VicinityMessage)chatMessage).Sender.ToString());
                    InMatch = true;
                }

                if (action == "MCA")
                {
                    MatchId = Convert.ToInt32(data[1]);

                    if (MatchId == DynelManager.LocalPlayer.Identity.Instance)
                    {
                        Player = ChessPiecesWhite;
                        _enemy = ChessPiecesBlack;
                    }
                    else
                    {
                        _enemy = ChessPiecesWhite;
                        Player = ChessPiecesBlack;
                    }

                    int hostMode = Convert.ToInt32(data[1]);

                    foreach (ChessPiece piece in Player)
                        piece.HostInstance = hostMode;

                    foreach (ChessPiece piece in _enemy)
                        piece.HostInstance = hostMode;
                }

                //so trolls cant fk up your match

                if (sender == DynelManager.LocalPlayer.Identity.Instance)
                    return;


                if (action == "MCU")
                {
                    Vector3 locPos = new Vector3(Convert.ToDouble(data[1]), Convert.ToDouble(data[2]), Convert.ToDouble(data[3]));
                    ChessPiece chessPiece = _enemy.Find(x => x.LocalPosition == locPos);
                    chessPiece.EnemyInstance = Convert.ToInt32(sender);
                }

                if (action == "MCR2")
                {
                    int index = Convert.ToInt32(data[1]);
                    Vector3 locPos = new Vector3(Convert.ToDouble(data[2]), Convert.ToDouble(data[3]), Convert.ToDouble(data[4]));
                    _enemy[index].LocalPosition = locPos;
                    _enemy[index].EnemyInstance = 0;

                    foreach (ChessPiece piece in Player)
                    {
                        if (piece.LocalPosition == locPos)
                        {
                            _eatenPieces = new Vector3(_eatenPieces.X, _eatenPieces.Y, _eatenPieces.Z + 1);
                            piece.LocalPosition = new Vector3(_eatenPieces.X, 0, _eatenPieces.Z);
                            piece.Eaten = true;

                            if (_eatenPieces.Z == 0)
                            {
                                _eatenPieces = new Vector3(_eatenPieces.X + 1, _eatenPieces.Y, -8);
                            }

                            break;
                        }
                    }

                }

                if (action == "MCR3")
                {
                    int type = Convert.ToInt32(data[1]);
                    Vector3 locPos = new Vector3(Convert.ToDouble(data[2]), Convert.ToDouble(data[3]), Convert.ToDouble(data[4]));

                    ChessPiece selectedPiece = _enemy.Find(x => x.LocalPosition == locPos);
                    selectedPiece._mesh = _enemy.Find(x => x.Type == (Type)type)._mesh;
                    selectedPiece.Type = (Type)type;
                }

                if (sender != MatchId)
                    return;

                if (action == "MCP")
                {
                    Vector3 placePos = new Vector3(Convert.ToDouble(data[1]), Convert.ToDouble(data[2]), Convert.ToDouble(data[3]));
                    PlaceBoard(placePos);
                }

                if (action == "MCM")
                {
                    PlaceBoard(Vector3.Zero);
                }
            }
        }


        private void SelectPiece()
        {
            ChessPiece selectedPiece = Player.Where(x => x.Selected == true).FirstOrDefault();
            ChessPiece movingPiece = Player.Where(x => x.MoveMode == true).FirstOrDefault();

            if (selectedPiece != null)
            {
                if (movingPiece != null)
                    ShowLegalMoves(movingPiece);

                if (Keyboard.IsKeyDown(Key.R) && ApplicationIsActivated() && _staticBoard)
                {
                    if (_keyTrigger2)
                    {
                        if (!selectedPiece.MoveMode) // picking up a piece
                        {
                            if (Player.Where(x => x.MoveMode == true).FirstOrDefault() == null)
                            {
                                Chat.SendVicinityMessage($"MCU {selectedPiece.LocalPosition.X} {selectedPiece.LocalPosition.Y} {selectedPiece.LocalPosition.Z}");
                                selectedPiece.MoveMode = true;
                                _legalMoves = new List<Vector3>();
                            }
                        }
                        else // placing a piece
                        {
                            float distance = 5;
                            Vector3 shortestMove = Vector3.Zero;
                            foreach (Vector3 move in _legalMoves)
                            {
                                if (Vector3.Distance(DynelManager.LocalPlayer.Position, move) < distance)
                                {
                                    distance = Vector3.Distance(DynelManager.LocalPlayer.Position, move);
                                    shortestMove = move;
                                }
                            }
                            int index = Player.TakeWhile(x => x.MoveMode != true).Count();
                            Vector3 newLocPos = selectedPiece.LocalPosition + shortestMove - selectedPiece.RestPosition;


                            foreach (ChessPiece piece in _enemy)
                            {
                                if (piece.LocalPosition == newLocPos)
                                {
                                    _eatenPieces = new Vector3(_eatenPieces.X, _eatenPieces.Y, _eatenPieces.Z + 1);
                                    piece.LocalPosition = new Vector3(_eatenPieces.X, 0, _eatenPieces.Z);
                                    piece.Eaten = true;

                                    if (_eatenPieces.Z == 0)
                                    {
                                        _eatenPieces = new Vector3(_eatenPieces.X + 1, _eatenPieces.Y, -8);
                                    }

                                    break;
                                }
                            }

                            Chat.SendVicinityMessage($"MCR2 {index} {newLocPos.X} {newLocPos.Y} {newLocPos.Z}");

                            if (selectedPiece.Type == Type.Pawn)
                            {
                                if (newLocPos.Z == 0 || newLocPos.Z == -7)
                                    UI.CreatePromoteWindow(newLocPos);
                            }
                            selectedPiece.Move = shortestMove;
                            selectedPiece.MoveMode = false;
                        }
                        _keyTrigger2 = false;
                    }
                }
                else
                {
                    _keyTrigger2 = true;
                }
            }

        }
        private void ShowLegalMoves(ChessPiece selectedPiece)
        {
            if (_legalMoves.Count() != 0)
            {
                foreach (Vector3 legalMove in _legalMoves)
                    Debug.DrawSphere(legalMove, 0.25f, DebuggingColor.Green);
                return;
            }

            _legalMoves.Add(selectedPiece.RestPosition);

            if (selectedPiece.Type == Type.Rook)
            {
                RookLogic(selectedPiece);
            }
            if (selectedPiece.Type == Type.Bishop)
            {
                BishopLogic(selectedPiece);
            }
            if (selectedPiece.Type == Type.Knight)
            {
                KnightLogic(selectedPiece);
            }
            if (selectedPiece.Type == Type.Queen)
            {
                RookLogic(selectedPiece);
                BishopLogic(selectedPiece);
            }
            if (selectedPiece.Type == Type.King)
            {
                KingLogic(selectedPiece);
            }
            if (selectedPiece.Type == Type.Pawn)
            {
                PawnLogic(selectedPiece);
            }
        }
        private void PawnLogic(ChessPiece selectedPiece)
        {
            if (MatchId == DynelManager.LocalPlayer.Identity.Instance)
            {
                if (selectedPiece.FirstMove)
                {                
                    //up
                    for (int i = 1; i < 3; i++)
                    {
                        Vector3 moveVector = Vector3.Forward * -1 * i;

                        if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                            break;

                        if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                            break;

                        _legalMoves.Add(selectedPiece.RestPosition + moveVector);

                    }
                }

                if (Math.Abs(selectedPiece.LocalPosition.Z) + 1 <= 7)
                {
                    Vector3 moveVector = Vector3.Forward * -1;
                    if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null &&
                        _enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                        _legalMoves.Add(selectedPiece.RestPosition + moveVector);
                }

                List<Vector3> attackMoves = new List<Vector3> { new Vector3(1, 0, -1), new Vector3(-1, 0, -1) };

                foreach (ChessPiece enemyPiece in _enemy)
                {
                    foreach (Vector3 attackMove in attackMoves)
                    {
                        if (selectedPiece.LocalPosition + attackMove == enemyPiece.LocalPosition)
                            _legalMoves.Add(selectedPiece.RestPosition + attackMove);
                    }
                }
            }
            else
            {
                if (selectedPiece.FirstMove)
                {
                    for (int i = 1; i < 3; i++)
                    {
                        Vector3 moveVector = Vector3.Forward * i;

                        if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                            break;

                        if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                            break;

                        _legalMoves.Add(selectedPiece.RestPosition + moveVector);

                    }
                }

                if (Math.Abs(selectedPiece.LocalPosition.Z) - 1 >= 0)
                {
                    Vector3 moveVector = Vector3.Forward ;
                    if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null &&
                        _enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                        _legalMoves.Add(selectedPiece.RestPosition + moveVector);
                }

                List<Vector3> attackMoves = new List<Vector3> { new Vector3(1, 0, 1), new Vector3(-1, 0, 1) };

                foreach (ChessPiece enemyPiece in _enemy)
                {
                    foreach (Vector3 attackMove in attackMoves)
                    {
                        if (selectedPiece.LocalPosition + attackMove == enemyPiece.LocalPosition)
                            _legalMoves.Add(selectedPiece.RestPosition + attackMove);
                    }
                }
            }
        }
        private void RookLogic(ChessPiece selectedPiece)
        {
            //up
            for (int i = 1; i < 7 - Math.Abs(selectedPiece.LocalPosition.Z) + 1; i++)
            {
                Vector3 moveVector = Vector3.Forward * -1 * i;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
                else
                    break;

                if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                    break;
            }
            //down
            for (int i = 1; i < 0 + Math.Abs(selectedPiece.LocalPosition.Z) + 1; i++)
            {
                Vector3 moveVector = Vector3.Forward * i;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
                else
                    break;

                if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                    break;
            }
            //left
            for (int i = 1; i < 7 - Math.Abs(selectedPiece.LocalPosition.X) + 1; i++)
            {
                Vector3 moveVector = Vector3.Right * i;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
                else
                    break;

                if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                    break;

            }
            //right
            for (int i = 1; i < Math.Abs(selectedPiece.LocalPosition.X) + 1; i++)
            {
                Vector3 moveVector = Vector3.Right * -1 * i;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
                else
                    break;

                if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                    break;
            }
        }
        private void BishopLogic(ChessPiece selectedPiece)
        {
            //northeast
            for (int i = 1; i < 7 + 1; i++)
            {
                float neX = Math.Abs(selectedPiece.LocalPosition.X) - i;
                float neZ = Math.Abs(selectedPiece.LocalPosition.Z) + i;

                if (neX >= 0 && neZ <= 7)
                {
                    Vector3 moveVector = new Vector3(-1, 0, -1) * i;

                    if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;

                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);

                    if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;
                }
                else
                    break;
            }
            //northwest
            for (int i = 1; i < 7 + 1; i++)
            {
                float neX = Math.Abs(selectedPiece.LocalPosition.X) + i;
                float neZ = Math.Abs(selectedPiece.LocalPosition.Z) + i;

                if (neX <= 7 && neZ <= 7)
                {
                    Vector3 moveVector = new Vector3(1, 0, -1) * i;
                    if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;

                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);

                    if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;
                }
                else
                    break;
            }
            //southwest
            for (int i = 1; i < 7 + 1; i++)
            {
                float neX = Math.Abs(selectedPiece.LocalPosition.X) + i;
                float neZ = Math.Abs(selectedPiece.LocalPosition.Z) - i;

                if (neX <= 7 && neZ >= 0)
                {
                    Vector3 moveVector = new Vector3(1, 0, 1) * i;

                    if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;

                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);

                    if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;
                }
                else
                    break;
            }
            //southeast
            for (int i = 1; i < 7 + 1; i++)
            {
                float neX = Math.Abs(selectedPiece.LocalPosition.X) - i;
                float neZ = Math.Abs(selectedPiece.LocalPosition.Z) - i;

                if (neX >= 0 && neZ >= 0)
                {
                    Vector3 moveVector = new Vector3(-1, 0, 1) * i;

                    if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;

                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);


                    if (_enemy.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) != null)
                        break;
                }
                else
                    break;
            }
        }
        private void KnightLogic (ChessPiece selectedPiece)
        {
            //12r
            if (Math.Abs(selectedPiece.LocalPosition.X) - 1 >= 0 && Math.Abs(selectedPiece.LocalPosition.Z) + 2 <= 7)
            {
                Vector3 moveVector = new Vector3(-1, 0, -2);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + new Vector3(-1, 0, -2));
            }
            //21r
            if (Math.Abs(selectedPiece.LocalPosition.X) - 2 >= 0 && Math.Abs(selectedPiece.LocalPosition.Z) + 1 <= 7)
            {
                Vector3 moveVector = new Vector3(-2, 0, -1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //1-2r
            if (Math.Abs(selectedPiece.LocalPosition.X) - 1 >= 0 && Math.Abs(selectedPiece.LocalPosition.Z) - 2 >= 0)
            {
                Vector3 moveVector = new Vector3(-1, 0, 2);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //2-1r
            if (Math.Abs(selectedPiece.LocalPosition.X) - 2 >= 0 && Math.Abs(selectedPiece.LocalPosition.Z) - 1 >= 0)
            {
                Vector3 moveVector = new Vector3(-2, 0, 1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }

            //mirrored
            //-12r
            if (Math.Abs(selectedPiece.LocalPosition.X) + 1 <= 7 && Math.Abs(selectedPiece.LocalPosition.Z) + 2 <= 7)
            {
                Vector3 moveVector = new Vector3(1, 0, -2);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //-21r
            if (Math.Abs(selectedPiece.LocalPosition.X) + 2 <= 7 && Math.Abs(selectedPiece.LocalPosition.Z) + 1 <= 7)
            {
                Vector3 moveVector = new Vector3(2, 0, -1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //-1-2r
            if (Math.Abs(selectedPiece.LocalPosition.X) + 1 <= 7 && Math.Abs(selectedPiece.LocalPosition.Z) - 2 >= 0)
            {
                Vector3 moveVector = new Vector3(1, 0, 2);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //-2-1r
            if (Math.Abs(selectedPiece.LocalPosition.X) + 2 <= 7 && Math.Abs(selectedPiece.LocalPosition.Z) - 1 >= 0)
            {
                Vector3 moveVector = new Vector3(2, 0, 1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
        }
        private void KingLogic(ChessPiece selectedPiece)
        {
            //up
            if (Math.Abs(selectedPiece.LocalPosition.Z) + 1 <= 7)
            {
                Vector3 moveVector = Vector3.Forward * -1;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //down
            if (Math.Abs(selectedPiece.LocalPosition.Z) - 1 >= 0)
            {
                Vector3 moveVector = Vector3.Forward;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //RIGHT
            if (Math.Abs(selectedPiece.LocalPosition.X) - 1 >= 0)
            {
                Vector3 moveVector = Vector3.Right * -1;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //LEFT
            if (Math.Abs(selectedPiece.LocalPosition.X) + 1 <= 7)
            {
                Vector3 moveVector = Vector3.Right;
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //northeast
            if (Math.Abs(selectedPiece.LocalPosition.X) - 1 >= 0 && Math.Abs(selectedPiece.LocalPosition.Z) + 1 <= 7)
            {
                Vector3 moveVector = new Vector3(-1, 0, -1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //northwest
            if (Math.Abs(selectedPiece.LocalPosition.X) + 1 <= 7 && Math.Abs(selectedPiece.LocalPosition.Z) + 1 <= 7)
            {
                Vector3 moveVector = new Vector3(1, 0, -1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }
            //southwest
            if (Math.Abs(selectedPiece.LocalPosition.X) + 1 <= 7 && Math.Abs(selectedPiece.LocalPosition.Z) - 1 >= 0)
            {
                Vector3 moveVector = new Vector3(1, 0, 1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }

            //southeast
            if (Math.Abs(selectedPiece.LocalPosition.X) - 1 >= 0 && Math.Abs(selectedPiece.LocalPosition.Z) - 1 >= 0)
            {
                Vector3 moveVector = new Vector3(-1, 0, 1);
                if (Player.Find(x => x.LocalPosition == selectedPiece.LocalPosition + moveVector) == null)
                    _legalMoves.Add(selectedPiece.RestPosition + moveVector);
            }

        }
        private void MoveBoard()
        {
            if (Keyboard.IsKeyDown(Key.E) && ApplicationIsActivated() && MatchId == DynelManager.LocalPlayer.Identity.Instance)
            {
                if (Player.Where(x => x.MoveMode == true).Count() == 0)
                {
                    if (_keyTrigger)
                    {
                        PlaceBoard(DynelManager.LocalPlayer.Position);
                    }
                    _keyTrigger = false;
                }
            }
            else
            {
                _keyTrigger = true;
            }
        }
        public void PlaceBoard(Vector3 pos)
        {
            _staticBoard = !_staticBoard;

            foreach (ChessPiece chessPiece in Player)
            {
                chessPiece.Static = _staticBoard;
                chessPiece.PlacedPos = pos;
            }

            foreach (ChessPiece chessPiece in _enemy)
            {
                chessPiece.Static = _staticBoard;
                chessPiece.PlacedPos = pos;
            }

            Vector3 playerPos = DynelManager.LocalPlayer.Position;

            if (DynelManager.LocalPlayer.Identity.Instance == MatchId)
            {
                if (_staticBoard == true)
                    Chat.SendVicinityMessage($"MCP {playerPos.X} {playerPos.Y} {playerPos.Z}");
                else
                    Chat.SendVicinityMessage($"MCM");
            }

        }
        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();

            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }
    }
}
