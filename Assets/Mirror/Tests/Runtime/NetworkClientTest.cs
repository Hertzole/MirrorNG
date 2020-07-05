using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Mirror.Tests
{
    [TestFixture]
    public class NetworkClientTest : HostSetup<MockComponent>
    {
        GameObject playerReplacement;

        [Test]
        public void IsConnectedTest()
        {
            Assert.That(client.IsConnected);
        }

        [Test]
        public void ConnectionTest()
        {
            Assert.That(client.Connection != null);
        }

        [Test]
        public void CurrentTest()
        {
            Assert.That(NetworkClient.Current == null);
        }

        [Test]
        public void ReadyTest()
        {
            client.sceneManager.Ready(client.Connection);
            Assert.That(client.sceneManager.ready);
            Assert.That(client.Connection.IsReady);
        }

        [Test]
        public void ReadyTwiceTest()
        {
            client.sceneManager.Ready(client.Connection);

            Assert.Throws<InvalidOperationException>(() =>
            {
                client.sceneManager.Ready(client.Connection);
            });
        }

        [Test]
        public void ReadyNull()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                client.sceneManager.Ready(null);
            });
        }

        [Test]
        public void RegisterPrefabExceptionTest()
        {
            var gameObject = new GameObject();
            Assert.Throws<InvalidOperationException>(() =>
            {
                client.RegisterPrefab(gameObject);
            });
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void RegisterPrefabGuidExceptionTest()
        {
            var guid = Guid.NewGuid();
            var gameObject = new GameObject();

            Assert.Throws<InvalidOperationException>(() =>
            {
                client.RegisterPrefab(gameObject, guid);
            });
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void OnSpawnAssetSceneIDFailureExceptionTest()
        {
            var msg = new SpawnMessage();
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            {
                client.OnSpawn(msg);
            });

            Assert.That(ex.Message, Is.EqualTo("OnObjSpawn netId: " + msg.netId + " has invalid asset Id"));
        }

        [Test]
        public void UnregisterPrefabExceptionTest()
        {
            var gameObject = new GameObject();
            Assert.Throws<InvalidOperationException>(() =>
            {
                client.UnregisterPrefab(gameObject);
            });
            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator GetPrefabTest()
        {
            var guid = Guid.NewGuid();
            var prefabObject = new GameObject("prefab", typeof(NetworkIdentity));

            client.RegisterPrefab(prefabObject, guid);

            yield return null;

            client.GetPrefab(guid, out GameObject result);

            Assert.That(result, Is.SameAs(prefabObject));

            Object.Destroy(prefabObject);
        }

        [Test]
        public void ReplacePlayerHostTest()
        {
            playerReplacement = new GameObject("replacement", typeof(NetworkIdentity));
            NetworkIdentity replacementIdentity = playerReplacement.GetComponent<NetworkIdentity>();
            replacementIdentity.AssetId = Guid.NewGuid();
            client.RegisterPrefab(playerReplacement);

            server.ReplacePlayerForConnection(server.LocalConnection, client, playerReplacement, true);

            Assert.That(server.LocalClient.Connection.Identity, Is.EqualTo(replacementIdentity));
        }

        int ClientChangeCalled;
        public void ClientChangeScene(string sceneName, SceneOperation sceneOperation, bool customHandling)
        {
            ClientChangeCalled++;
        }

        [Test]
        public void ClientChangeSceneTest()
        {
            client.sceneManager.ClientChangeScene.AddListener(ClientChangeScene);
            client.sceneManager.OnClientChangeScene("", SceneOperation.Normal, false);
            Assert.That(ClientChangeCalled, Is.EqualTo(1));
        }

        int ClientSceneChangedCalled;
        public void ClientSceneChanged(INetworkConnection conn)
        {
            ClientSceneChangedCalled++;
        }

        [Test]
        public void ClientSceneChangedTest()
        {
            client.sceneManager.ClientSceneChanged.AddListener(ClientSceneChanged);
            client.sceneManager.OnClientSceneChanged(client.Connection);
            Assert.That(ClientSceneChangedCalled, Is.EqualTo(1));
        }

        int ClientNotReadyCalled;
        public void ClientNotReady(INetworkConnection conn)
        {
            ClientNotReadyCalled++;
        }

        [Test]
        public void ClientNotReadyTest()
        {
            client.ClientNotReady.AddListener(ClientNotReady);
            client.OnClientNotReady(client.Connection);
            Assert.That(ClientNotReadyCalled, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ObjectHideTest()
        {
            client.OnObjectHide(new ObjectHideMessage
            {
                netId = identity.NetId
            });

            yield return null;

            Assert.That(identity == null);
        }

        [UnityTest]
        public IEnumerator ObjectDestroyTest()
        {
            client.OnObjectDestroy(new ObjectDestroyMessage
            {
                netId = identity.NetId
            });

            yield return null;

            Assert.That(identity == null);
        }
    }
}
