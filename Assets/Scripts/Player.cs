using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace pdox.UnityNetcode
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private Transform m_SpawnedObjectPrefab;
        [SerializeField] private Canvas m_PlayerInfoCanvas;

        private NetworkVariable<MyCustomData> l_RandomNumber = new NetworkVariable<MyCustomData>(new MyCustomData
        {
            _int = 0,
            _bool = true
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public struct MyCustomData : INetworkSerializable
        {
            public int _int;
            public bool _bool;
            public FixedString128Bytes _string;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _int);
                serializer.SerializeValue(ref _bool);
                serializer.SerializeValue(ref _string);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                l_RandomNumber.Value = new MyCustomData
                {
                    _int = Random.Range(0, 100),
                    _bool = Random.Range(0, 2) == 0
                };
            } else
            {
                m_PlayerInfoCanvas.transform.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = $"ClientID: {OwnerClientId}";
            }

            l_RandomNumber.OnValueChanged += (MyCustomData a_OldValue, MyCustomData a_NewValue) =>
            {
                Debug.Log($"Client ID: {OwnerClientId} Random number changed from {a_OldValue._int} to {a_NewValue._int}");
            };
        }

        private void Update()
        {

            if (!IsOwner)
            {
                m_PlayerInfoCanvas.gameObject.transform.rotation = Quaternion.LookRotation(m_PlayerInfoCanvas.gameObject.transform.position - Camera.main.transform.position);
                // m_PlayerInfoCanvas.transform.forward = Camera.main.transform.forward;
                return;
            }

            if (Input.GetKeyDown(KeyCode.T)) // T for test
            {

                Transform l_SpawnedObject = Instantiate(m_SpawnedObjectPrefab, transform.position, transform.rotation);
                l_SpawnedObject.GetComponent<NetworkObject>().Spawn(true);

                l_RandomNumber.Value = new MyCustomData
                {
                    _int = Random.Range(0, 100),
                    _bool = Random.Range(0, 2) == 0
                };

                TestClientRpc(new ClientRpcParams{ Send = new ClientRpcSendParams{ TargetClientIds = new List<ulong> { 1 } } });
            }

            Vector3 l_MoveDirection = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) l_MoveDirection.z += 1;
            if (Input.GetKey(KeyCode.S)) l_MoveDirection.z -= 1;
            if (Input.GetKey(KeyCode.A)) l_MoveDirection.x -= 1;
            if (Input.GetKey(KeyCode.D)) l_MoveDirection.x += 1;

            float l_Speed = 3f;

            MoveServerRpc(l_MoveDirection * l_Speed * Time.deltaTime);
        }

        [ServerRpc]
        public void MoveServerRpc(Vector3 position)
        {
            transform.position += position;
        }

        [ClientRpc]
        public void TestClientRpc(ClientRpcParams a_ClientRpcParams = default)
        {
            Debug.Log("TestClientRpc");
        }
    }
}