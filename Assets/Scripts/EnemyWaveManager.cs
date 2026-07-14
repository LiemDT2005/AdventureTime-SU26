using UnityEngine;
using UnityEngine.Events;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("Wave Enemies")]
    public EnemyStats[] enemiesInWave;

    [Header("Actions when cleared")]
    public UnityEvent onWaveCleared;
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

    private int activeEnemies;

    void Start()
    {
        activeEnemies = enemiesInWave.Length;

        foreach (var enemy in enemiesInWave)
        {
            if (enemy != null)
            {
                enemy.OnDied += HandleEnemyDied;
            }
        }
    }

    private void HandleEnemyDied()
    {
        activeEnemies--;

        if (activeEnemies <= 0)
        {
            WaveCleared();
        }
    }

    private void WaveCleared()
    {
        onWaveCleared?.Invoke();

        foreach (var obj in objectsToEnable)
        {
            if (obj != null) obj.SetActive(true);
        }

        foreach (var obj in objectsToDisable)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}
