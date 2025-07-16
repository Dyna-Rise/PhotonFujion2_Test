using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
public class Carryable : NetworkBehaviour
{
    // ===== 現在の運搬者 =====
    // ▼ Fusion2 ではカンマ区切りで追加属性を書く
    [Networked, OnChangedRender(nameof(OnCarrierChanged))]
    public NetworkObject Carrier { get; set; }

    Rigidbody _rb;
    void Awake() => _rb = GetComponent<Rigidbody>();

    // 変更検知コールバック（引数なし）
    void OnCarrierChanged() => SyncState();

    void SyncState()
    {
        bool carried = Carrier != null;

        // 親子付け
        if (carried)
        {
            var anchor = Carrier.GetComponent<Player>().CarryAnchor;
            transform.SetParent(anchor, false);
        }
        else
        {
            transform.SetParent(null, true);
        }

        // 物理切替
        if (_rb)
        {
            _rb.isKinematic = carried;
            if (!carried) _rb.WakeUp();
        }
    }

    public override void FixedUpdateNetwork()
    {
        //nullじゃなければ持たれている
        // 持たれている間はPlayerのアンカーオブジェに追従
        if (Carrier != null)     
        {
            var anchor = Carrier.GetComponent<Player>().CarryAnchor;
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }
    }

    // ── API ─────────────────────
    public void PickedUpBy(Player p) => Carrier = p.Object;   // これだけで全員に同期
    public void Dropped() => Carrier = null;


    //// ピックアップメソッド
    //public void PickedUpBy(Player p)
    //{
    //    if (_rb) _rb.isKinematic = true;

    //    transform.SetParent(p.CarryAnchor, worldPositionStays: false);

    //    Carrier = p.Object;      // Player の NetworkObject を直接代入
    //}

    ////ドロップメソッド
    //public void Dropped()
    //{
    //    transform.SetParent(null, true);

    //    // プレイヤーの前方 0.3 m & 少し下げる 0.1 m
    //    transform.position += transform.forward * 0.3f - Vector3.up * 0.1f;

    //    if (_rb)
    //    {
    //        _rb.isKinematic = false;
    //        _rb.WakeUp();              // 物理エンジンを確実に再開
    //    }
    //    Carrier = null;
    //}
}
