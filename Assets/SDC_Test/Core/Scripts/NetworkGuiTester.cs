using Mirror;
using SDC_Test.NM.Contracts;
using UnityEngine;
using VContainer;

namespace SDC_Test.Core
{
    public sealed class NetworkGuiTester : MonoBehaviour
    {
        private INMServer mServer;

        private string _messageText = "Hello Client!";

        [Inject]
        public void Construct(INMServer nmServer)
        {
            mServer = nmServer;
        }

        private void OnGUI()
        {
            if (!NetworkServer.active) 
                return;

            int screenWidth = Screen.width;
            GUILayout.BeginArea(new Rect(screenWidth - 320, 10, 300, 150), "Server Message Sender", GUI.skin.window);

            GUILayout.Space(10);
            GUILayout.Label("Enter Message Text:");

            _messageText = GUILayout.TextField(_messageText, GUILayout.Height(25));

            GUILayout.Space(10);

            if (GUILayout.Button("Send HelloMessage", GUILayout.Height(35)))
            {
                HelloMessage msg = new() 
                { 
                    Message = _messageText 
                };
                mServer.Raise(msg);
            }

            GUILayout.EndArea();
        }
    }
}
