using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class Map4ComprehensiveSetup : EditorWindow
{
    [MenuItem("Tools/Run Map4 Comprehensive Setup")]
    public static void RunSetup()
    {
        Debug.Log("--- STARTING COMPREHENSIVE SETUP ---");

        int bossLayer = LayerMask.NameToLayer("Boss") != -1 ? LayerMask.NameToLayer("Boss") : 7;
        int enemyLayer = bossLayer; // Gộp chung Enemy vào Boss Layer theo yêu cầu
        int groundLayer = LayerMask.NameToLayer("Ground") != -1 ? LayerMask.NameToLayer("Ground") : 3;
        int playerLayer = LayerMask.NameToLayer("Player") != -1 ? LayerMask.NameToLayer("Player") : 8;

        // Bước 1 & 2: Player Setup
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            MeleeWeapon melee = player.GetComponent<MeleeWeapon>();
            if (melee != null)
            {
                // Fix weaponComponent reference
                SerializedObject so = new SerializedObject(player);
                so.Update();
                so.FindProperty("weaponComponent").objectReferenceValue = melee;
                so.ApplyModifiedProperties();

                // Set targetLayer to Enemy + Boss
                melee.targetLayer = (1 << enemyLayer) | (1 << bossLayer);
                Debug.Log("Bước 1: Đã sửa Player MeleeWeapon targetLayer và reference.");
            }

            player.enemyBounceLayer = (1 << enemyLayer) | (1 << bossLayer);
            player.groundLayer = 1 << groundLayer; // Set chuẩn Ground layer để tránh jump spam
            EditorUtility.SetDirty(player);
        }

        // Bước 2 (tiếp): AI LayerMasks
        GoblinAI[] goblins = FindObjectsByType<GoblinAI>(FindObjectsSortMode.None);
        foreach (var goblin in goblins)
        {
            goblin.wallLayer = 1 << groundLayer;
            goblin.groundLayer = 1 << groundLayer;
            EditorUtility.SetDirty(goblin);
        }

        BatAI[] bats = FindObjectsByType<BatAI>(FindObjectsSortMode.None);
        foreach (var bat in bats)
        {
            bat.obstacleLayer = 1 << groundLayer;
            bat.wallLayer = 1 << groundLayer;
            EditorUtility.SetDirty(bat);
        }
        Debug.Log("Bước 2: Đã set LayerMask cho Player, Goblin, Bat.");
        
        // Bước 3: Boss Setup
        GameObject bossObj = GameObject.Find("Boss");
        if (bossObj == null) bossObj = GameObject.FindGameObjectWithTag("Boss");
        BossAI bossAI = null;
        BossStats bossStats = null;

        if (bossObj != null)
        {
            bossObj.layer = bossLayer;
            bossObj.tag = "Boss";

            bossAI = bossObj.GetComponent<BossAI>();
            if (bossAI == null) bossAI = bossObj.AddComponent<BossAI>();

            bossStats = bossObj.GetComponent<BossStats>();
            if (bossStats == null) bossStats = bossObj.AddComponent<BossStats>();

            // Hit points
            Transform hitPoint = bossObj.transform.Find("HitPoint");
            if (hitPoint == null)
            {
                hitPoint = new GameObject("HitPoint").transform;
                hitPoint.SetParent(bossObj.transform);
                hitPoint.localPosition = new Vector3(2f, 0f, 0f);
            }

            Transform headCheck = bossObj.transform.Find("HeadCheckPoint");
            if (headCheck == null)
            {
                headCheck = new GameObject("HeadCheckPoint").transform;
                headCheck.SetParent(bossObj.transform);
                headCheck.localPosition = new Vector3(0f, 1.5f, 0f);
            }

            bossAI.meleeHitPoint = hitPoint;
            bossAI.headCheckPoint = headCheck;
            bossAI.animator = bossObj.GetComponent<Animator>();
            if (bossAI.animator == null) bossAI.animator = bossObj.AddComponent<Animator>();

            bossAI.targetLayer = 1 << playerLayer;
            bossAI.obstacleLayer = 1 << groundLayer;

            EditorUtility.SetDirty(bossObj);
            Debug.Log("Bước 3: Đã setup hoàn chỉnh Boss (BossAI, BossStats, Transform HitPoint).");
        }

        // Bước 4: Hand Spell Prefab
        GameObject handPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Hand.prefab");
        if (handPrefab == null)
        {
            GameObject handInScene = GameObject.Find("Hand");
            if (handInScene == null) handInScene = new GameObject("Hand");
            
            if (handInScene.GetComponent<SpriteRenderer>() == null) handInScene.AddComponent<SpriteRenderer>();
            if (handInScene.GetComponent<Animator>() == null) handInScene.AddComponent<Animator>();
            if (handInScene.GetComponent<BossHandSpell>() == null) handInScene.AddComponent<BossHandSpell>();

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                
            handPrefab = PrefabUtility.SaveAsPrefabAsset(handInScene, "Assets/Prefabs/Hand.prefab");
            DestroyImmediate(handInScene);
            Debug.Log("Bước 4: Đã tạo Assets/Prefabs/Hand.prefab thật.");
        }
        
        if (bossAI != null && handPrefab != null)
        {
            bossAI.handSpellPrefab = handPrefab;
            EditorUtility.SetDirty(bossAI);
        }

        // Bước 6: HP Bars
        SetupHPBarFor(player?.gameObject, Color.green, new Vector2(1f, 0.2f), new Vector3(0, 0.5f, 0));
        foreach (var goblin in goblins) SetupHPBarFor(goblin.gameObject, Color.red, new Vector2(1f, 0.2f), new Vector3(0, 0.5f, 0));
        foreach (var bat in bats) SetupHPBarFor(bat.gameObject, Color.red, new Vector2(1f, 0.2f), new Vector3(0, 0.5f, 0));
        if (bossObj != null) SetupHPBarFor(bossObj, Color.red, new Vector2(3f, 0.4f), new Vector3(0, 1f, 0));

        // Assign HitPoint for Player
        if (player != null)
        {
            MeleeWeapon melee = player.GetComponent<MeleeWeapon>();
            if (melee != null)
            {
                Transform hp = player.transform.Find("HitPoint");
                if (hp == null)
                {
                    hp = new GameObject("HitPoint").transform;
                    hp.SetParent(player.transform);
                    hp.localPosition = new Vector3(1f, 0f, 0f);
                }
                melee.hitPoint = hp;
                EditorUtility.SetDirty(melee);
            }
        }

        // Assign HitPoint for Goblins
        foreach (var goblin in goblins)
        {
            Transform hp = goblin.transform.Find("HitPoint");
            if (hp == null)
            {
                hp = new GameObject("HitPoint").transform;
                hp.SetParent(goblin.transform);
                hp.localPosition = new Vector3(1f, 0f, 0f);
            }
            goblin.hitPoint = hp;
            goblin.targetLayer = 1 << playerLayer;
            EditorUtility.SetDirty(goblin);
        }

        // Assign HitPoint for Bats
        foreach (var bat in bats)
        {
            Transform hp = bat.transform.Find("HitPoint");
            if (hp == null)
            {
                hp = new GameObject("HitPoint").transform;
                hp.SetParent(bat.transform);
                hp.localPosition = new Vector3(1f, 0f, 0f);
            }
            bat.hitPoint = hp;
            bat.targetLayer = 1 << playerLayer;
            EditorUtility.SetDirty(bat);
        }

        Debug.Log("Bước 6: Đã tạo và setup HP Bar UI và HitPoint cho toàn bộ sinh vật.");

        // Bước 7: Boss Banner UI
        GameObject bannerObj = GameObject.Find("BossBannerUI");
        if (bannerObj == null)
        {
            bannerObj = new GameObject("BossBannerUI");
            Canvas canvas = bannerObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            GameObject textObj = new GameObject("BannerText");
            textObj.transform.SetParent(bannerObj.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = "BOSS";
            txt.fontSize = 50;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.red;
            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            bannerObj.AddComponent<Animator>(); 
            bannerObj.SetActive(false);
            Debug.Log("Bước 7: Đã tạo Boss Banner UI.");
        }

        // Bước 5: BossRoomSequenceManager
        BossRoomSequenceManager seqMgr = FindAnyObjectByType<BossRoomSequenceManager>();
        if (seqMgr == null)
        {
            GameObject seqGo = new GameObject("BossRoomSequenceManager");
            seqMgr = seqGo.AddComponent<BossRoomSequenceManager>();
            PolygonCollider2D col = seqGo.AddComponent<PolygonCollider2D>();
            col.isTrigger = true;
            Debug.Log("Bước 5: Đã tạo mới BossRoomSequenceManager (dùng PolygonCollider2D).");
        }
        else
        {
            PolygonCollider2D col = seqMgr.GetComponent<PolygonCollider2D>();
            if (col == null)
            {
                // Xóa BoxCollider cũ nếu có
                BoxCollider2D oldBox = seqMgr.GetComponent<BoxCollider2D>();
                if (oldBox != null) DestroyImmediate(oldBox);
                
                col = seqMgr.gameObject.AddComponent<PolygonCollider2D>();
            }
            if (col != null) col.isTrigger = true;
        }

        seqMgr.bossAI = bossAI;
        seqMgr.playerTransform = player?.transform;
        seqMgr.mainCamera = Camera.main;
        seqMgr.bossBannerUI = bannerObj;
        seqMgr.bossBannerAnimator = bannerObj?.GetComponent<Animator>();
        seqMgr.bossHpBarUI = bossObj?.GetComponentInChildren<EnemyHPBar>(true)?.gameObject;

        if (bossObj != null)
        {
            Transform fp = bossObj.transform.Find("BossFocusPoint");
            if (fp == null)
            {
                fp = new GameObject("BossFocusPoint").transform;
                fp.SetParent(bossObj.transform);
                fp.localPosition = Vector3.zero;
            }
            seqMgr.bossFocusPoint = fp;
        }
        
        if (Camera.main != null)
        {
            seqMgr.cameraFollowScript = Camera.main.GetComponent("CameraMovement") as MonoBehaviour;
        }
        EditorUtility.SetDirty(seqMgr);
        Debug.Log("Bước 5: Đã setup xong BossRoomSequenceManager (gán đủ 8 field, IsTrigger = true).");

        // Bước 8: EnemyWaveManager
        EnemyWaveManager waveMgr = FindAnyObjectByType<EnemyWaveManager>();
        if (waveMgr == null)
        {
            GameObject waveGo = new GameObject("EnemyWaveManager");
            waveMgr = waveGo.AddComponent<EnemyWaveManager>();
        }

        EnemyStats[] allEnemies = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);
        waveMgr.enemiesInWave = allEnemies; 
        
        GameObject door = GameObject.Find("BossDoor");
        if (door == null)
        {
            door = new GameObject("BossDoor");
            door.AddComponent<BoxCollider2D>();
        }
        waveMgr.objectsToDisable = new GameObject[] { door };
        EditorUtility.SetDirty(waveMgr);
        Debug.Log("Bước 8: Đã setup EnemyWaveManager và đưa toàn bộ quái vào Wave, gán BossDoor.");

        // Bước 9: Dọn dẹp
        foreach (var goblin in goblins)
        {
            goblin.gameObject.layer = enemyLayer;
            EnemyStats[] stats = goblin.GetComponents<EnemyStats>();
            if (stats.Length > 1)
            {
                for (int i = 1; i < stats.Length; i++) DestroyImmediate(stats[i]);
                Debug.Log("Bước 9: Đã xóa EnemyStats bị duplicate trên Goblin.");
            }
        }
        foreach (var bat in bats)
        {
            bat.gameObject.layer = enemyLayer;
        }

        Debug.Log("--- COMPREHENSIVE SETUP COMPLETED SUCCESSFULLY ---");
    }

    private static void SetupHPBarFor(GameObject target, Color color, Vector2 size, Vector3 localPos)
    {
        if (target == null) return;

        EnemyHPBar hpBarScript = target.GetComponentInChildren<EnemyHPBar>(true);
        if (hpBarScript == null)
        {
            GameObject canvasObj = new GameObject("HP_Canvas");
            canvasObj.transform.SetParent(target.transform);
            canvasObj.transform.localPosition = localPos;
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = size;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject bgObj = new GameObject("HP_Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.31f, 0.31f, 0.31f, 1f); 
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("HP_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.color = color;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            hpBarScript = canvasObj.AddComponent<EnemyHPBar>();
            hpBarScript.fillImage = fillImg;
            EditorUtility.SetDirty(hpBarScript);
            EditorUtility.SetDirty(canvasObj);

            if (target.CompareTag("Boss")) canvasObj.SetActive(false);
        }

        PlayerStats pStats = target.GetComponent<PlayerStats>();
        if (pStats != null) { pStats.hpBar = hpBarScript; EditorUtility.SetDirty(pStats); }

        EnemyStats eStats = target.GetComponent<EnemyStats>();
        if (eStats != null) { eStats.hpBar = hpBarScript; EditorUtility.SetDirty(eStats); }

        BossStats bStats = target.GetComponent<BossStats>();
        if (bStats != null) { bStats.hpBar = hpBarScript; EditorUtility.SetDirty(bStats); }
    }

    private static void CreateLayerIfNeeded(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        
        // Kiểm tra xem layer đã tồn tại chưa
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (layerSP.stringValue == layerName) return; // Đã có layer này
        }

        // Tìm slot trống và thêm layer
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layerSP = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"Đã tự động tạo Layer: {layerName} tại index {i}");
                return;
            }
        }
    }
}
