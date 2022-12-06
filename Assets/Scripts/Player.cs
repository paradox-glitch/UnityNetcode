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

        private Vector3 m_PlayerMovementInput;
        private Vector2 m_PlayerRotationInput;
        [SerializeField] private float m_PlayerMovementSpeed = 5f;
        [SerializeField] private float m_PlayerRotationSensitivity = 3f;
        [SerializeField] private float m_PlayerJumpForce = 10f;

        private Rigidbody m_PlayerRigidbody;

        [SerializeField] private LayerMask m_GroundLayerMask;

        private float m_PlayerXRotation;



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

                Camera.main.transform.SetParent(transform);
                Camera.main.transform.localPosition = new Vector3(0, 1f, 0f);
            
                l_RandomNumber.Value = new MyCustomData
                {
                    _int = Random.Range(0, 100),
                    _bool = Random.Range(0, 2) == 0
                };

                m_PlayerInfoCanvas.gameObject.SetActive(false);

                m_PlayerRigidbody = GetComponent<Rigidbody>();
            }
            else
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
                Vector3 l_LookPosition = Camera.main.transform.position; //* Gets the current camera position
                l_LookPosition = m_PlayerInfoCanvas.gameObject.transform.position - Camera.main.transform.position; //* Fixes it facing the wrong way;
                l_LookPosition.y = 0; //* Resets y height so player info wll stay straight
                m_PlayerInfoCanvas.gameObject.transform.rotation = Quaternion.LookRotation(l_LookPosition);
                return;
            }

            m_PlayerMovementInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            m_PlayerRotationInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            Debug.Log($"[Player] Movement Input: {m_PlayerMovementInput}");

            MovePlayer();
            MoveCamera();


            if (Input.GetKeyDown(KeyCode.T)) // T for test
            {

                SpawnObjectServerRpc(this.transform.position);

                l_RandomNumber.Value = new MyCustomData
                {
                    _int = Random.Range(0, 100),
                    _bool = Random.Range(0, 2) == 0
                };

                TestClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { 1 } } });
            }

            Vector3 l_MoveDirection = Vector3.zero;
            // if (Input.GetKey(KeyCode.W)) l_MoveDirection.z += 1;
            // if (Input.GetKey(KeyCode.S)) l_MoveDirection.z -= 1;
            // if (Input.GetKey(KeyCode.A)) l_MoveDirection.x -= 1;
            // if (Input.GetKey(KeyCode.D)) l_MoveDirection.x += 1;

            float l_Speed = 3f;

            MoveServerRpc(l_MoveDirection * l_Speed * Time.deltaTime);
        }

        private void MovePlayer()
        {
            Vector3 l_MoveDirection = transform.TransformDirection(m_PlayerMovementInput) * m_PlayerMovementSpeed;

            m_PlayerRigidbody.velocity = new Vector3(l_MoveDirection.x, m_PlayerRigidbody.velocity.y, l_MoveDirection.z);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if(Physics.CheckSphere(new Vector3(transform.position.x, transform.position.y - 1, transform.position.z ), 0.1f, m_GroundLayerMask))
                    m_PlayerRigidbody.AddForce(Vector3.up * m_PlayerJumpForce, ForceMode.Impulse);
            }
        }

        private void MoveCamera()
        {
            m_PlayerXRotation -= m_PlayerRotationInput.y * m_PlayerRotationSensitivity;

            transform.Rotate(0, m_PlayerRotationInput.x * m_PlayerRotationSensitivity, 0);
            Camera.main.transform.localRotation = Quaternion.Euler(m_PlayerXRotation, 0, 0);
        }


        [ServerRpc]
        public void MoveServerRpc(Vector3 position)
        {
            transform.position += position;
        }

        [ServerRpc]
        public void SpawnObjectServerRpc(Vector3 a_Position)
        {
            Transform l_SpawnedObject = Instantiate(m_SpawnedObjectPrefab, a_Position, Quaternion.Euler(Vector3.zero));
            l_SpawnedObject.GetComponent<NetworkObject>().Spawn(true);
        }

        [ClientRpc]
        public void TestClientRpc(ClientRpcParams a_ClientRpcParams = default)
        {
            Debug.Log("TestClientRpc");
        }
    }
}