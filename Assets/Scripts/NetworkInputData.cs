using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
    public bool interactPressed;   //荷物をもつフラグ
}
