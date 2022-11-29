
using Unity.Netcode;
using UnityEngine;

namespace pdox.UnityNetcode
{
    public class DevMenuManager : MonoBehaviour
    {
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();

                SubmitNewPosition();
            }

            GUILayout.EndArea();
        }

        static void StartButtons()
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        }

        static void StatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ?
                "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

            GUILayout.Label("Transport: " +
                NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
            GUILayout.Label("Mode: " + mode);

                        if (NetworkManager.Singleton.IsHost)
            {
                if (GUILayout.Button("Close Host")) NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Close Server")) NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                if (GUILayout.Button("Close Client")) NetworkManager.Singleton.Shutdown();
            }
        }

        static void SubmitNewPosition()
        {

        }
    }
}