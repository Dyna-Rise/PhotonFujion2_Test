using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    //NetworkPrefabRef ：インスペクターで登録したプレハブをランタイムへ安全に渡すためのハンドル
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    //Dictionary で PlayerRef → 生成済み NetworkObject を保持し、後で破棄しやすくしている
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    //Photon Cloudとの接続確立・切断、再接続制御
    private NetworkRunner _runner;


    //入室時メソッド
    //引数にどのRunnerかと識別情報を与える
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        //ホストなら反応(スタンドアロンアプリの時)
        //if (runner.IsServer) 

        //WebGLでは各クライアントが自分のアバターだけ生成する
        if (player == runner.LocalPlayer) 
        {
            // プレイヤー生成の度に3ズレる
            Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);

            //runner.Spawn(どのプレハブを、どこに、どの回転で、誰のInput Authorityか)
            NetworkObject networkPlayerObject = runner.Spawn(
                _playerPrefab,
                spawnPosition,
                Quaternion.identity,
                player
                );

            //Dictionary型に誰が操作するアバターなのかを登録
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    //退室時のメソッド
    //引数にどのRunnerかと識別情報を与える
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        //引数に与えた識別がDictionaryリストにあれば
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            //対象のオブジェクトをデスポーン
            runner.Despawn(networkObject);
            //Dictionaryリストから抹消
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogError($"Fusion Shutdown: {shutdownReason}");
    }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }


    //通信の開始
    async void StartGame(GameMode mode)
    {
        // NetWorkRunner型の変数_runnerにNetWorkRunnerコンポーネントを追加して格納
        _runner = gameObject.AddComponent<NetworkRunner>();

        //OnInputを呼び出すためにtrue　コンポーネントに入力を送れるようにする(OnInputのWASD操作ができる)
        _runner.ProvideInput = true;

        // シーンデータをFujionのSceneRef構造体へ変換して変数sceneに格納
        // .buildIndex:GetActiveSceneで獲得したシーン情報がBuildSettings上で何番目かを整数で取得
        SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);


        //StartGameメソッドで部屋を作成
        //await：部屋作成の非同期処理が完了するまで待つ
        //成功すると NetworkRunner が稼働状態 になり、
        //INetworkRunnerCallbacks (例: OnPlayerJoined) が自動で呼ばれる
        StartGameResult result = await _runner.StartGame(

            //NetworkRunnerがシーンの同期を一括管理できるようにする
            new StartGameArgs()
            {
                //通信とトポロジ
                //GameMode = mode, //引数mode：GameMode.Host、GameMode.Client、GameMode.Server、GameMode.Sharedのいずれか
                GameMode = GameMode.Shared, //全員P2PでCloudが仲介（WebGL)
                SessionName = "TestRoom", //HostとServerはこの名前でルーム作成、ClientとSharedはこの名前に参加を試みる
                Scene = scene, //参加する全員が同じシーンを読み込む

                //NetWorkSceneManagerDefaultコンポーネントをオブジェクトに追加
                //オブジェクトがNetworkRunnerからシーンが誰がいつロード/アンロードするかを一任される現場監督になる
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            }
        );

        // StartGameResult型のOkがfalseなら失敗
        if (!result.Ok)
        {
            Debug.LogError($"StartGame failed: {result.ShutdownReason} / {result.ErrorMessage}");
            return;                 // 失敗時はここで抜けておくと安全
        }
 
    }


    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
    }
}

