using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SetupMap4Boss : EditorWindow
{
    [MenuItem("Tools/Setup Map4 Boss UI & Sequence")]
    public static void SetupBoss()
    {
        // 1. Find or create BossRoomSequenceManager
        BossRoomSequenceManager sequenceManager = Object.FindAnyObjectByType<BossRoomSequenceManager>();
        if (sequenceManager == null)
        {
            GameObject go = new GameObject("BossRoomSequenceManager");
            sequenceManager = go.AddComponent<BossRoomSequenceManager>();
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(5f, 10f); // Default trigger size
            Debug.Log("Created BossRoomSequenceManager object");
        }

        // 2. Find Boss and Player
        BossAI bossAI = Object.FindAnyObjectByType<BossAI>();
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        Camera mainCam = Camera.main;

        if (bossAI == null)
        {
            Debug.LogError("Could not find BossAI in the scene. Please make sure the Boss is present.");
            return;
        }
        if (player == null)
        {
            Debug.LogError("Could not find Player in the scene.");
            return;
        }

        // 3. Setup BossStats & HP Bar
        BossStats bossStats = bossAI.GetComponent<BossStats>();
        if (bossStats == null) bossStats = bossAI.gameObject.AddComponent<BossStats>();

        EnemyHPBar hpBarScript = bossAI.GetComponentInChildren<EnemyHPBar>(true);
        if (hpBarScript == null)
        {
            // Create UI Canvas for Boss
            GameObject canvasObj = new GameObject("Boss_HP_Canvas");
            canvasObj.transform.SetParent(bossAI.transform);
            canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0); // Hiển thị trên đầu Boss
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(3f, 0.4f);
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create Background
            GameObject bgObj = new GameObject("HP_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.31f, 0.31f, 0.31f, 1f); // #4F4F4F
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Create Fill
            GameObject fillObj = new GameObject("HP_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.color = Color.red;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            hpBarScript = canvasObj.AddComponent<EnemyHPBar>();
            hpBarScript.fillImage = fillImg;
            
            canvasObj.SetActive(false); // Sẽ được bật khi Boss intro sequence xong
            Debug.Log("Created Boss HP Bar UI");
        }
        bossStats.hpBar = hpBarScript;

        // 4. Create Boss Banner if missing
        GameObject bannerObj = GameObject.Find("BossBannerUI");
        if (bannerObj == null)
        {
            bannerObj = new GameObject("BossBannerUI");
            Canvas canvas = bannerObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            GameObject textObj = new GameObject("BannerText");
            textObj.transform.SetParent(bannerObj.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = "BOSS: TÊN BOSS";
            txt.fontSize = 50;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.red;
            
            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            bannerObj.AddComponent<Animator>(); // Cần tạo Animator Controller thật trong Editor
            bannerObj.SetActive(false);
            Debug.Log("Created temporary Boss Banner UI");
        }

        // 5. Setup Sequence Manager fields
        sequenceManager.bossAI = bossAI;
        sequenceManager.playerTransform = player.transform;
        sequenceManager.mainCamera = mainCam;
        sequenceManager.bossBannerUI = bannerObj;
        sequenceManager.bossBannerAnimator = bannerObj.GetComponent<Animator>();
        sequenceManager.bossHpBarUI = hpBarScript.gameObject;
        
        MonoBehaviour camFollow = mainCam.GetComponent("CameraMovement") as MonoBehaviour;
        if (camFollow != null)
        {
            sequenceManager.cameraFollowScript = camFollow;
        }

        // Focus point
        Transform focusPoint = bossAI.transform.Find("BossFocusPoint");
        if (focusPoint == null)
        {
            GameObject fp = new GameObject("BossFocusPoint");
            fp.transform.SetParent(bossAI.transform);
            fp.transform.localPosition = Vector3.zero;
            focusPoint = fp.transform;
        }
        sequenceManager.bossFocusPoint = focusPoint;

        EditorUtility.SetDirty(sequenceManager);
        EditorUtility.SetDirty(bossStats);
        EditorUtility.SetDirty(bossAI);
        
        Debug.Log("Successfully setup Map4 Boss & Sequence Manager! Please create an Animator Controller for BossBannerUI with 'ShowBanner' and 'HideBanner' triggers.");
    }
}
