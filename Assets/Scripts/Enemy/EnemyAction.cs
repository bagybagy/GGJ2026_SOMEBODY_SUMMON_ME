using System.Collections;
using UnityEngine;

// 💡 行動の種類を定義
public enum ActionType
{
    Attack, // 攻撃 (Dash, Watchなど)
    Chase   // 追跡 (移動)
}

// 💡 abstractクラス：これ自体はアタッチできない「抽象的な概念」としてのクラス
public abstract class EnemyAction : MonoBehaviour
{
    public ActionType actionType; 

    // 親AIが呼ぶ共通の命令
    public abstract IEnumerator Execute(); // 行動実行（コルーチン）
    public abstract void Stop();           // 強制停止

    // 💡Step13.1 現在ターゲットの参照
    // 💡 1. 脳みそへの参照をキャッシュする変数
    private EnemyAI _brain;

    // 💡 2. 子クラスが使うための「ターゲット取得プロパティ」
    // 今後、継承されたクラスではbrainに従ってターゲットを決めて、直接利用できるようになる
    protected Transform Target
    {
        get
        {
            // 初回アクセス時にEnemyAIを取得（Lazy Load）
            if (_brain == null) _brain = GetComponent<EnemyAI>();

            // AIが持っているターゲットを返す
            if (_brain != null) return _brain.CurrentTarget;

            return null;
        }
    }
}