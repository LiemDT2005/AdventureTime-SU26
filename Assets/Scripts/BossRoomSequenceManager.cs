using UnityEngine;
using System.Collections;

public class BossRoomSequenceManager : MonoBehaviour
{
    [Header("References")]
    public BossAI bossAI;
    public Transform bossFocusPoint;   // vị trí camera nhìn vào Boss (thường đặt ngay tại Boss hoặc hơi lệch)
    public Transform playerTransform;
    public Camera mainCamera;
    public MonoBehaviour cameraFollowScript; // script follow player hiện có, để tắt tạm trong lúc intro (kéo vào Inspector)

    [Header("UI")]
    public GameObject bossBannerUI;
    public Animator bossBannerAnimator; // Animator để chạy hiệu ứng ShowBanner/HideBanner
    public GameObject bossHpBarUI;

    [Header("Timing")]
    public float zoomMoveDuration = 1f;
    public float zoomHoldDuration = 1.2f;
    public float bannerDuration = 1.5f;
    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 0.2f;
    public float returnMoveDuration = 1f;

    [Header("SFX (tùy chọn)")]
    public AudioSource audioSource;
    public AudioClip bossAppearSfx;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        
        // Bỏ qua Intro Sequence, kích hoạt Boss đánh luôn
        if (bossHpBarUI != null) bossHpBarUI.SetActive(true);
        if (bossAI != null) bossAI.isFighting = true;
    }

    private IEnumerator BossIntroSequence()
    {
        if (cameraFollowScript != null) cameraFollowScript.enabled = false;

        Vector3 startCamPos = mainCamera.transform.position;
        Vector3 bossCamPos = new Vector3(bossFocusPoint.position.x, bossFocusPoint.position.y, startCamPos.z);

        // Bước: Camera zoom vào Boss
        yield return StartCoroutine(LerpCamera(startCamPos, bossCamPos, zoomMoveDuration));
        yield return new WaitForSeconds(zoomHoldDuration);

        // Banner tên boss
        if (bossBannerUI != null) bossBannerUI.SetActive(true);
        if (bossBannerAnimator != null) bossBannerAnimator.SetTrigger("ShowBanner");

        // Rung nhẹ + âm thanh
        if (audioSource != null && bossAppearSfx != null)
            audioSource.PlayOneShot(bossAppearSfx);
        yield return StartCoroutine(ScreenShake());

        yield return new WaitForSeconds(bannerDuration);
        if (bossBannerAnimator != null) 
        {
            bossBannerAnimator.SetTrigger("HideBanner");
            yield return new WaitForSeconds(0.5f); // Đợi animation ẩn chạy xong
        }
        if (bossBannerUI != null) bossBannerUI.SetActive(false);

        // HP Bar — để trống/disable, làm sau
        if (bossHpBarUI != null) bossHpBarUI.SetActive(true);

        // Camera trả về Player
        Vector3 playerCamPos = new Vector3(playerTransform.position.x, playerTransform.position.y, startCamPos.z);
        yield return StartCoroutine(LerpCamera(mainCamera.transform.position, playerCamPos, returnMoveDuration));

        if (cameraFollowScript != null) cameraFollowScript.enabled = true;

        // Bắt đầu chiến đấu
        if (bossAI != null) bossAI.isFighting = true;
    }

    private IEnumerator LerpCamera(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            mainCamera.transform.position = Vector3.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.position = to;
    }

    private IEnumerator ScreenShake()
    {
        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            mainCamera.transform.position = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }
}