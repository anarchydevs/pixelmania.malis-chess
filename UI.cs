using System;
using System.Linq;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData.UI;
using AOSharp.Common.GameData;

namespace MaliChess
{
    public class UI : AOPluginEntry
    {
        public static string PluginDir;
        private static Window _requestWindow;
        private static Window _promoteWindow;

        public override void Run(string pluginDir)
        {
            try
            {
                PluginDir = pluginDir;
            }
            catch (Exception e)
            {
                Chat.WriteLine(e.Message);
            }
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

            Chat.SendVicinityMessage($"MCR3 {(int)type} {locPos.X} {locPos.Y} {locPos.Z}");

            _promoteWindow.Close();

        }
        public static void AcceptRequest(object sender, ButtonBase e)
        {
            string name = (string)e.Tag;

            _requestWindow.Close();
            Gameplay.MatchId = Convert.ToInt32(name);
            Chat.SendVicinityMessage($"MCA {Gameplay.MatchId}");
            Chat.WriteLine("Request Accepted");
        }
        public static  void DenyRequest(object sender, ButtonBase e)
        {
            _requestWindow.Close();
            Gameplay.InMatch = false;
            Chat.WriteLine("Request Denied");
        }
        public class PromoteTag
        {
            public Vector3 LocalPos;
            public Type Type;
        }
    }
}
