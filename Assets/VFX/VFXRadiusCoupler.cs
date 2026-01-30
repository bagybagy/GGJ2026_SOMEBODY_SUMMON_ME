using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways] // エディタ編集中も動くようにする
public class VFXRadiusCoupler : MonoBehaviour
{
    [SerializeField] VisualEffect vfx;
    [SerializeField] SphereCollider col;
    [SerializeField] string propertyName = "Radius";

    void Update()
    {
        if (vfx != null && col != null)
        {
            // コライダーの半径をVFXに流し込む
            vfx.SetFloat(propertyName, col.radius);
        }
    }
}