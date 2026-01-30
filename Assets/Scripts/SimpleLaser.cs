using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLaser : MonoBehaviour
{
    Vector3 acceleration; // レーザーの加速度
    Vector3 velocity; // レーザーの速度
    Vector3 position; // レーザーの位置
    Transform target; // レーザーのターゲット

    [SerializeField][Tooltip("着弾時間")] float period = 1f; // レーザーがターゲットに到達する時間
    [SerializeField][Tooltip("着弾時差")] float deltaPeriod = 0.5f; // 着弾時間のランダムな変動範囲
    [SerializeField][Tooltip("x軸初速")] float x_initial_v = 10f; // X軸方向の初速度
    [SerializeField][Tooltip("y軸初速")] float y_initial_v = 10f; // Y軸方向の初速度
    [SerializeField][Tooltip("z軸初速")] float z_initial_v = 10f; // Z軸方向の初速度

    [SerializeField][Tooltip("タゲロス時の飛行時間")] float lingerTime = 2f; // ターゲットがいなくなってからの惰性飛行時間
    private bool targetLost = false; // ターゲットが失われたかどうかのフラグ

    void Start()
    {
        // GameObject.FindWithTag("Enemy") の結果がnullかどうかを先にチェックする
        GameObject enemyObject = GameObject.FindWithTag("Enemy");

        // ターゲットが見つからなかった場合、オブジェクトを破棄
        if (enemyObject == null)
        {
            Destroy(gameObject);
            return;
        }
        if (target == null)
        {
            // Enemyオブジェクトが見つかった場合のみ、Transformを取得
            target = enemyObject.GetComponent<Transform>();
        }
        // レーザーの初期位置を設定
        position = transform.position;

        // レーザーの初期速度をランダムに設定、地面めり込み禁止でy軸のみ0以上に
        velocity = new Vector3(Random.Range(-x_initial_v, x_initial_v),
                                Random.Range(0, y_initial_v),
                                Random.Range(-z_initial_v, z_initial_v));

        // 着弾時間をランダムに変動させる
        period += Random.Range(-deltaPeriod, deltaPeriod);
    }

    // 💡 外部からターゲットを指定するメソッド
    public void SetTarget(Transform newTarget)
    {
        this.target = newTarget;
    }

    void Update()
    {
        // ターゲットが存在する場合、追従する
        if (target != null)
        {
            //運動方程式：t秒間に進む距離(diff) = (初速度(v) * t) ＋ (1/2 *加速度(a) * t^2)
            //変形すると
            //運動方程式：加速度(a) = 2*(diff - (v * t)) / t^2 
            //なので、「速度vの物体がt秒後にdiff進むための加速度a」が算出できる
            //GameObjectのvは取得できるし、tも取得できる
            //なら、レーザーがperiod秒後に到着（diffが0）するために必要なaが算出できる

            acceleration = Vector3.zero; // 初期加速度を0に設定

            Vector3 diff = target.position - position; // ターゲットとの距離を計算

            // 必要な加速度を計算
            acceleration += (diff - velocity * period) * 2f / (period * period);

            period -= Time.deltaTime; // 残りの期間を減少させる

            // periodが0未満になった場合、オブジェクトを破壊
            if (period < 0f)
            {
                Destroy(gameObject);
                return;
            }

        }
        else if (!targetLost)
        {
            // ターゲットが失われた場合、フラグを立てて惰性のためのメソッドを呼び出す
            targetLost = true;
            // ターゲットが失われた場合、lingerTime秒後にオブジェクトを破壊
            StartCoroutine(DestroyLaser(lingerTime));
        }

        // 現在の速度を更新
        velocity += acceleration * Time.deltaTime;

        // 現在の位置を更新
        position += velocity * Time.deltaTime;

        // オブジェクトの位置を更新
        transform.position = position;

    }
    IEnumerator DestroyLaser(float delay)
    {
        yield return new WaitForSeconds(delay); // 指定時間待機
        Destroy(gameObject); // 待機後に破壊
    }
}