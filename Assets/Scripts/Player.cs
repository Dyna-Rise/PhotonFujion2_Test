using Fusion;
using UnityEngine;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class Player : NetworkBehaviour
{
    [SerializeField] Transform _carryAnchor;         // 手の位置
    public Transform CarryAnchor => _carryAnchor;


    [Networked] NetworkObject _carriedObj { get; set; }

    private NetworkCharacterController _cc;

    private void Awake()
    {
        //生成されたらNetworkCharacterControllerコンポーネントを追加
        _cc = GetComponent<NetworkCharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // ===== 移動 =====
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);

            // ===== Eキー処理 =====
            if (data.interactPressed)
                TryInteract();
        }
    }

    //ピックアップ・ドロップ
    void TryInteract()
    {
        // 1) まだ何も持っていない ⇒ 拾う
        if (_carriedObj == null)
        {
            // 半径1.5m内の Carryable を検索
            var hit = Physics.OverlapSphere(
                transform.position,
                1.5f,
                LayerMask.GetMask("Carryable")
               );

            if (hit.Length == 0) return;

            var carryable = hit[0].GetComponent<Carryable>();
            if (!carryable) return;
            
            var obj = carryable.Object; // NetworkObject
            // Authority を要求、Shared モードでは「奪う」だけ
            obj.RequestStateAuthority();

            // Authority が付与される Tick 以降で安全に状態を変える
            carryable.PickedUpBy(this);
            _carriedObj = obj;  // 自分が保持中として記録
        }
        // 2) 既に持っている ⇒ 置く
        else
        {
            var carried = _carriedObj;         // そのまま参照できる
            carried.GetComponent<Carryable>().Dropped();

            //carried.ReleaseStateAuthority();   // Authority 返却 :contentReference[oaicite:1]{index=1}
            // ↑をせず、Authority は持った人のままにしておく
            //Shared モードでは “Authority を持っている Peer が物理を計算して全員に同期する”ので、落としたあとも 持っていた人が責任を持つ

            _carriedObj = null;
        }
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            var cam = Camera.main.GetComponent<CameraFollow>();
            if (cam)
            {
                cam.SetTarget(transform);
            }
        }
    }
}