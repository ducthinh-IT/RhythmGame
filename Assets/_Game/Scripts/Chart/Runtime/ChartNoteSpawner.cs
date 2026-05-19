using System.Collections.Generic;
using UnityEngine;

public class ChartNoteSpawner : MonoBehaviour
{
    [Header("Chart")]
    [Tooltip("Tên file JSON chart. Nếu SelectedSongManager có bài đang chọn thì sẽ đọc từ đó thay thế.")]
    [SerializeField] private string chartFileName = "test_chart";

    [Header("Clock")]
    [SerializeField] private ChartPlaybackClock playbackClock;

    [Header("Spawn")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;

    [Header("Gameplay — Required for movement")]
    [Tooltip("NoteManager is required. Notes will not move without it.")]
    [SerializeField] private NoteManager noteManager;

    [Header("Editor Integration")]
    [Tooltip("Preview Note Parent (world space). Sẽ tự động tắt khi runtime spawn bắt đầu để tránh chồng lên runtime notes.")]
    [SerializeField] private GameObject previewNoteParentToDisable;

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 50;

    [Header("Layout")]
    [SerializeField] private float laneSpacing = 160f;

    [Header("Movement")]
    [Tooltip("Y position of the hit line in anchoredPosition space (UI).")]
    [SerializeField] private float hitlineY = -330f;

    [Tooltip("Scroll speed in pixels per second (UI anchoredPosition).")]
    [SerializeField] private float scrollSpeed = 600f;

    [Tooltip("Touch radius in pixels used for input detection.")]
    [SerializeField] private float touchRadius = 120f;

    [Header("Timing")]
    [SerializeField] private float preSpawnTime = 2f;

    [Header("Debug")]

    [SerializeField] private bool logSpawnedNotes = false;

    [Header("Edit Mode")]
    [Tooltip("Bật khi muốn chỉnh chart bằng tay (Load Preview → Play → kéo note → Save).\n" +
             "Khi ON: không spawn runtime notes, không tắt Note Parent preview.\n" +
             "Khi OFF: chế độ bình thường, Note Parent tự tắt, runtime notes spawn.")]
    [SerializeField] private bool editMode = false;

    private List<ChartNoteSpawnData> _spawnDataList;
    private int _nextSpawnIndex;
    private bool _isReady;
    private int _laneCount = 4;

    /// <summary>
    /// Đồng bộ chartFileName với SongData được chọn từ Editor dropdown.
    /// Gọi bởi ChartGeneratorToolEditor khi user đổi bài nhạc.
    /// </summary>
    public void SetChartFileName(string fileName)
    {
        chartFileName = fileName;
    }

    private void Start()
    {
        // ─── EDIT MODE: chỉ xem preview, không spawn runtime notes ───────────
        if (editMode)
        {
            Debug.Log("ChartNoteSpawner: Edit Mode ON — runtime spawn disabled. " +
                      "Note Parent giữ nguyên để editor kéo note.");
            return;
        }
        // ────────────────────────────────────────────────────────────────

        if (noteManager == null)
        {
            Debug.LogError("ChartNoteSpawner: NoteManager is not assigned. " +
                           "Notes will not move. Assign NoteManager in the Inspector.");
            return;
        }

        // Tắt preview Note Parent để runtime notes không bị chồng lên preview notes.
        if (previewNoteParentToDisable != null)
        {
            previewNoteParentToDisable.SetActive(false);
            Debug.Log("ChartNoteSpawner: Preview Note Parent đã tắt để nhường chỗ cho runtime notes.");
        }

        // --- KHỞI TẠO OBJECT POOL ---
        NotePool pool = NotePool.Instance;
        if (pool == null)
        {
            GameObject poolObj = new GameObject("NotePool");
            pool = poolObj.AddComponent<NotePool>();
        }

        NoteBase noteBasePrefab = notePrefab != null ? notePrefab.GetComponent<NoteBase>() : null;
        if (noteBasePrefab != null)
        {
            pool.InitializePool(noteBasePrefab, noteParent, initialPoolSize, noteManager);
        }
        else
        {
            Debug.LogError("ChartNoteSpawner: notePrefab must have a NoteBase component for Object Pooling.");
        }
        // ------------------------------

        // Ưu tiên lấy từ SelectedSongManager (bài đang chọn trong menu).
        // Nếu không có (test trực tiếp trong scene) → dùng Inspector value.
        string fileToLoad = chartFileName;

        SongData selectedSong = SelectedSongManager.Instance != null
            ? SelectedSongManager.Instance.SelectedSong
            : null;

        if (selectedSong != null)
        {
            string computedName = selectedSong.ComputedChartFileName;
            if (!string.IsNullOrEmpty(computedName))
            {
                fileToLoad = computedName;
                Debug.Log($"ChartNoteSpawner: Chart từ SelectedSongManager: '{fileToLoad}'");
            }

            // Đổi AudioClip sang nhạc của bài được chọn
            if (playbackClock != null && selectedSong.audioClip != null)
            {
                playbackClock.SetClip(selectedSong.audioClip);
                Debug.Log($"ChartNoteSpawner: Audio → '{selectedSong.audioClip.name}'");
            }
        }

        if (!ChartSpawnDataProvider.TryGetChartAndSpawnData(
                fileToLoad,
                out ChartData loadedChart,
                out _spawnDataList))
        {
            Debug.LogError("ChartNoteSpawner: Failed to get chart and spawn data.");
            return;
        }

        _laneCount = loadedChart.laneCount > 0 ? loadedChart.laneCount : 4;

        if (playbackClock != null)
        {
            playbackClock.SetOffset(loadedChart.offset);
        }

        Debug.Log($"ChartNoteSpawner: Applied chart offset: {loadedChart.offset}");

        ClearSpawnedNotes();

        _nextSpawnIndex = 0;
        _isReady = true;

        Debug.Log($"ChartNoteSpawner ready. Notes to spawn: {_spawnDataList.Count}");
    }

    private void Update()
    {
        if (!_isReady)
        {
            return;
        }

        if (playbackClock == null)
        {
            Debug.LogError("ChartNoteSpawner: Playback clock is missing.");
            _isReady = false;
            return;
        }

        if (!playbackClock.IsPlaying)
        {
            return;
        }

        float songTime = playbackClock.SongTime;

        // Sync NoteManager time với audio clock để NoteMovement tính đúng vị trí.
        // SetExternalTime() tắt internal clock của NoteManager, tránh drift.
        noteManager.SetExternalTime(songTime);

        SpawnDueNotes(songTime);
    }

    private void SpawnDueNotes(float songTime)
    {
        while (_nextSpawnIndex < _spawnDataList.Count)
        {
            ChartNoteSpawnData data = _spawnDataList[_nextSpawnIndex];

            float spawnTime = data.hitTime - preSpawnTime;

            if (songTime < spawnTime)
            {
                break;
            }

            SpawnNote(data);
            _nextSpawnIndex++;
        }
    }

    private void SpawnNote(ChartNoteSpawnData data)
    {
        if (notePrefab == null)
        {
            Debug.LogError("ChartNoteSpawner: Note prefab is missing.");
            return;
        }

        NoteBase noteBase = NotePool.Instance.GetNote(Vector3.zero, Quaternion.identity);
        
        if (noteBase != null)
        {
            noteBase.name = $"Note_{data.noteId}_{data.noteType}_Lane{data.laneIndex}_Time{data.hitTime:F2}";

            NoteRuntimeData runtimeData = new NoteRuntimeData
            {
                noteId         = data.noteId,
                laneIndex      = data.laneIndex,
                hitTime        = data.hitTime,
                duration       = data.duration,
                anchoredX      = GetCenteredLaneX(data.laneIndex),
                hitlineY       = this.hitlineY,
                scrollSpeed    = this.scrollSpeed,
                touchRadius    = this.touchRadius,
                flickDirection = data.flickDirection,
            };

            // Initialize sẽ set anchoredPosition.x và chuẩn bị NoteMovement.
            // NoteMovement.Tick() sẽ tính Y mỗi frame từ NoteManager.
            noteBase.Initialize(runtimeData);

            noteManager.RegisterNote(noteBase);

            if (logSpawnedNotes)
            {
                Debug.Log($"ChartNoteSpawner: Spawned + initialized note ID {data.noteId} " +
                          $"| Lane {data.laneIndex} | HitTime {data.hitTime:F2}");
            }
        }
        else
        {
            Debug.LogError($"ChartNoteSpawner: NotePool returned null for note ID {data.noteId}.");
        }
    }

    /// <summary>
    /// Tính anchoredPosition X để 4 lane trải đều, căn giữa màn hình (X = 0).
    /// Ví dụ 4 lanes spacing 160: -240, -80, +80, +240
    /// </summary>
    private float GetCenteredLaneX(int laneIndex)
    {
        float totalWidth = (_laneCount - 1) * laneSpacing;
        float startX = -totalWidth / 2f;
        return startX + laneIndex * laneSpacing;
    }

    private void ClearSpawnedNotes()
    {
        if (noteParent == null) return;

        // Trả tất cả nốt đang active về Pool thay vì Destroy
        for (int i = noteParent.childCount - 1; i >= 0; i--)
        {
            Transform child = noteParent.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                NoteBase note = child.GetComponent<NoteBase>();
                if (note != null && NotePool.Instance != null)
                {
                    NotePool.Instance.ReturnNote(note);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}