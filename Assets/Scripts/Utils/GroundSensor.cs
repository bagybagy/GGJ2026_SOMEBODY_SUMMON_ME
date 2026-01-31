using UnityEngine;

// 💡 軽量化された接地判定センサー
// ・Raycastによる正確な判定（壁対策）
// ・4フレームに1回の実行頻度調整
// ・Animatorパラメータのハッシュ化
public class GroundSensor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float rayLength = 0.5f;          // 足元からのRayの長さ
    [SerializeField] private Vector3 rayOriginOffset = new Vector3(0, 0.2f, 0); // 足元より少し上から撃つ
    [SerializeField] private LayerMask groundLayer;           // 接地対象レイヤー
    [SerializeField] private int checkInterval = 4;           // 何フレームごとに判定するか

    [Header("Animator Keys")]
    [SerializeField] private string groundedBoolName = "Grounded";
    [SerializeField] private string landTriggerName = "OnLand";

    private Animator animator;
    private int groundedHash;
    private int landHash;

    private bool isGrounded = false;
    private int frameOffset; // 負荷分散用のオフセット

    // 外部から確認用プロパティ
    public bool IsGrounded => isGrounded;

    void Start()
    {
        // 💡 負荷分散：全NPCが同一フレームで計算しないようにランダムにズラす
        frameOffset = Random.Range(0, checkInterval);

        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            // 親や子も探す
            animator = GetComponent<Animator>();
        }

        // AnimatorIDのキャッシュ
        groundedHash = Animator.StringToHash(groundedBoolName);
        landHash = Animator.StringToHash(landTriggerName);

        // GroundLayerが未設定ならデフォルト設定（GroundObject + Default）
        if (groundLayer == 0)
        {
            groundLayer = LayerMask.GetMask("Default", "GroundObject", "Terrain");
        }
    }

    void FixedUpdate()
    {
        // 💡 頻度調整: 指定フレームに1回だけ実行
        if ((Time.frameCount + frameOffset) % checkInterval != 0) return;

        CheckGround();
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + rayOriginOffset;
        
        // Raycast実行 (下向き)
        bool hit = Physics.Raycast(origin, Vector3.down, rayLength, groundLayer);

        // 状態変化チェック
        if (hit != isGrounded)
        {
            isGrounded = hit;
            if (animator != null)
            {
                animator.SetBool(groundedHash, isGrounded);

                // 着地した瞬間だけTriggerを引く
                if (isGrounded)
                {
                    animator.SetTrigger(landHash);
                }
            }
        }
    }

    // デバッグ表示
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 origin = transform.position + rayOriginOffset;
        Gizmos.DrawLine(origin, origin + Vector3.down * rayLength);
    }
}
