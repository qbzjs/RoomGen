<<<<<<< HEAD
using UnityEngine;

public class WeaponColliderScript : MonoBehaviour
{
    public PlayerAtk playerAtk; // PlayerAtk 스크립트의 참조

    private void Awake()
    {
        // playerAtk 컴포넌트를 자동으로 찾아서 할당할 수도 있습니다.
        // 예: playerAtk = FindObjectOfType<PlayerAtk>();
=======
癤퓎sing UnityEngine;

public class WeaponColliderScript : MonoBehaviour
{
    public PlayerAtk playerAtk; // PlayerAtk �뒪�겕由쏀듃�쓽 李몄“

    private void Awake()
    {
        // playerAtk 而댄룷�꼳�듃瑜� �옄�룞�쑝濡� 李얠븘�꽌 �븷�떦�븷 �닔�룄 �엳�뒿�땲�떎.
        // �븯吏�留� FindObjectOfType��� �꼫臾� 鍮꾪슚�쑉�쟻�씠湲� �븯�떎
        //playerAtk = FindObjectOfType<PlayerAtk>();
>>>>>>> 490b48c1d07f9272897a1d5bb968027958be33a4
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Monster"))
        {
            Rigidbody enemyRb = other.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
<<<<<<< HEAD
                // Player가 바라보는 방향으로 힘을 가함
=======
                // Player媛� 諛붾씪蹂대뒗 諛⑺뼢�쑝濡� �옒�쓣 媛��븿
>>>>>>> 490b48c1d07f9272897a1d5bb968027958be33a4
                Vector3 forceDirection = playerAtk.player.transform.forward;
                enemyRb.AddForce(forceDirection.normalized * playerAtk.forceMagnitude, ForceMode.Impulse);
            }
        }
    }
}
