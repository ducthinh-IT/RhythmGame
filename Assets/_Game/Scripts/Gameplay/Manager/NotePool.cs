using System.Collections.Generic;
using UnityEngine;

public class NotePool : MonoBehaviour
{
    public static NotePool Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private NoteBase notePrefab;
    [SerializeField] private Transform poolParent;
    [SerializeField] private int initialPoolSize = 50;

    private Queue<NoteBase> availableNotes = new Queue<NoteBase>();
    private NoteManager noteManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Khởi tạo Pool. Called bởi ChartNoteSpawner ở hàm Start.
    /// </summary>
    public void InitializePool(NoteBase prefab, Transform parent, int initialSize, NoteManager manager)
    {
        notePrefab = prefab;
        poolParent = parent;
        initialPoolSize = initialSize;
        noteManager = manager;

        // Lắng nghe sự kiện NoteManager để tự động thu hồi nốt khi nó hoàn thành (Hit/Miss)
        if (noteManager != null)
        {
            noteManager.OnNoteFinishedEvent += HandleNoteFinished;
        }

        // Khởi tạo sẵn một lượng nốt vào hàng đợi
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewNoteObject();
        }

        Debug.Log($"NotePool: Khởi tạo thành công {initialPoolSize} nốt nhạc vào Pool.");
    }

    private void OnDisable()
    {
        if (noteManager != null)
        {
            noteManager.OnNoteFinishedEvent -= HandleNoteFinished;
        }
    }

    /// <summary>
    /// Lấy nốt từ Pool ra để sử dụng.
    /// </summary>
    public NoteBase GetNote(Vector3 position, Quaternion rotation)
    {
        if (availableNotes.Count == 0)
        {
            // Nếu hồ hết nốt, bắt buộc phải đẻ thêm (để chống lỗi thiếu nốt khi nhạc quá nhanh/dày)
            CreateNewNoteObject();
            Debug.LogWarning("NotePool: Hết nốt trong hàng đợi! Đang tạo thêm nốt mới.");
        }

        NoteBase note = availableNotes.Dequeue();
        
        note.transform.position = position;
        note.transform.rotation = rotation;
        note.gameObject.SetActive(true);

        return note;
    }

    /// <summary>
    /// Thu hồi nốt về Pool.
    /// </summary>
    public void ReturnNote(NoteBase note)
    {
        if (note == null) return;

        note.gameObject.SetActive(false);
        availableNotes.Enqueue(note);
    }

    /// <summary>
    /// Tự động được gọi khi NoteManager phát sự kiện OnNoteFinishedEvent
    /// </summary>
    private void HandleNoteFinished(NoteBase note, NoteResult result)
    {
        ReturnNote(note);
    }

    private void CreateNewNoteObject()
    {
        if (notePrefab == null) return;

        NoteBase newNote = Instantiate(notePrefab, poolParent);
        newNote.gameObject.SetActive(false); // Ban đầu tắt đi
        availableNotes.Enqueue(newNote);
    }
}
