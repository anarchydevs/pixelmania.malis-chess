using System;
using System.Linq;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData.UI;
using AOSharp.Common.GameData;
using System.Collections.Generic;

namespace MaliChess
{
    public class UI : AOPluginEntry
    {
        public static string PluginDir;
        private Window _mainWindow;
        private static Window _promoteWindow;
        private static Window _requestWindow;
        private Window _helpWindow;
        public override void Run(string pluginDir)
        {
            try
            {
                PluginDir = pluginDir;
                CreateMainWindow();
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
        }
        public void CreateMainWindow()
        {
            _mainWindow = Window.CreateFromXml("Main", $"{PluginDir}\\XML\\MainWindow.xml",
                   windowStyle: WindowStyle.Default,
                     windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade | WindowFlags.NoExit);

            if (_mainWindow.FindView("challenge", out Button challenge))
            {
                challenge.Clicked = Challenge;
            }

            if (_mainWindow.FindView("forfeit", out Button forfeit))
            {
                forfeit.Clicked = Forfeit;
            }

            if (_mainWindow.FindView("help", out Button help))
            {
                help.Clicked = HelpWindow;
            }

            _mainWindow.Show(true);
        }
        private void HelpWindow(object sender, ButtonBase e)
        {
            _helpWindow = Window.CreateFromXml("Help", $"{PluginDir}\\XML\\HelpWindow.xml",
               windowStyle: WindowStyle.Default,
                 windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade);
            _helpWindow.Show(true);
        }
        private void Challenge(object sender, ButtonBase e)
        {
            if (Gameplay.MatchId != 0)
            {
                Chat.WriteLine("Already in match (Forfeit?)");
                return;
            }

            if (Targeting.Target.Identity == null)
                Chat.WriteLine("Invalid target!");

            Chat.WriteLine($"Challenging: {DynelManager.Players.First(x => x.Identity.Instance == Targeting.Target.Identity.Instance).Name}");
            Chat.SendVicinityMessage($"MCC {Targeting.Target.Identity.Instance}", VicinityMessageType.Shout);
        }
        private void Forfeit(object sender, ButtonBase e)
        {
            Chat.WriteLine($"Forfeiting match");
            GenerateModels.ResetBoard();
            Chat.SendVicinityMessage("MCF", VicinityMessageType.Shout);
        }
        public static void CreateRequestWindow(string name)
        {
            _requestWindow = Window.CreateFromXml("Request", $"{PluginDir}\\XML\\RequestWindow.xml",
                   windowStyle: WindowStyle.Default,
                     windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade | WindowFlags.NoExit);

            if (_requestWindow.FindView("yes", out Button yes))
            {
                yes.Tag = name;
                yes.Clicked = AcceptRequest;
            }

            if (_requestWindow.FindView("no", out Button no))
            {
                no.Clicked = DenyRequest;
            }

            if (_requestWindow.FindView("text", out TextView text))
            {
                text.Text = DynelManager.Players.First(x=>x.Identity.Instance == Convert.ToInt32(name)).Name;
            }

            _requestWindow.Show(true);
        }
        public static void CreatePromoteWindow(Vector3 pos)
        {
            _promoteWindow = Window.CreateFromXml("Promote", $"{PluginDir}\\XML\\PromoteWindow.xml",
                   windowStyle: WindowStyle.Default,
                     windowFlags: WindowFlags.AutoScale | WindowFlags.NoFade | WindowFlags.NoExit);

            if (_promoteWindow.FindView("rook", out Button rook))
            {
                rook.Tag = new PromoteTag
                {
                    LocalPos = pos,
                    Type = Type.Rook
                };
                rook.Clicked = PromotePawn;
            }

            if (_promoteWindow.FindView("bishop", out Button bishop))
            {
                bishop.Tag = new PromoteTag
                {
                    LocalPos = pos,
                    Type = Type.Bishop
                };
                bishop.Clicked = PromotePawn;
            }

            if (_promoteWindow.FindView("knight", out Button knight))
            {
                knight.Tag = new PromoteTag
                {
                    LocalPos = pos,
                    Type = Type.Knight
                };
                knight.Clicked = PromotePawn;
            }

            if (_promoteWindow.FindView("queen", out Button queen))
            {
                queen.Tag = new PromoteTag
                {
                    LocalPos = pos,
                    Type = Type.Queen
                };
                queen.Clicked = PromotePawn;
            }

            _promoteWindow.Show(true);
        }
        public static void PromotePawn(object sender, ButtonBase e)
        {
            PromoteTag promoteTag = (PromoteTag)e.Tag;
            Vector3 locPos = promoteTag.LocalPos;
            Type type = promoteTag.Type;

            ChessPiece selectedPiece = Gameplay.Player.Find(x => x.LocalPosition == locPos);
            selectedPiece._mesh = Gameplay.Player.Find(x => x.Type == type)._mesh;
            selectedPiece.Type = type;

            Chat.SendVicinityMessage($"MCR3 {(int)type} {locPos.X} {locPos.Y} {locPos.Z}", VicinityMessageType.Shout);

            _promoteWindow.Close();

        }
        public static void AcceptRequest(object sender, ButtonBase e)
        {
            string name = (string)e.Tag;

            _requestWindow.Close();
            Gameplay.MatchId = Convert.ToInt32(name);
            Chat.SendVicinityMessage($"MCA {Gameplay.MatchId}", VicinityMessageType.Shout);
            Chat.WriteLine("Request Accepted");
        }
        public static  void DenyRequest(object sender, ButtonBase e)
        {
            _requestWindow.Close();
            Gameplay.InMatch = false;
            Chat.SendVicinityMessage("MCD", VicinityMessageType.Shout);
            Chat.WriteLine("Request Denied");
        }
        public class PromoteTag
        {
            public Vector3 LocalPos;
            public Type Type;
        }
    }
}
