using UnityEngine;

public class ChartVisualizer : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;
    private ChartData currentChart;

    [Header("Layout")]
    [SerializeField] private float laneSpacing = 1.2f;
    [SerializeField] private float timeSpacing = 1f;

    // Reference to settings to read BPM and Subdivision for snapping
    public ChartGenerationSettings Settings { get; set; }

    public void Draw(ChartData chart)
    {
        Clear();
        currentChart = chart;
        if (chart == null || chart.notes == null)
        {
            Debug.LogWarning("Chart is empty.");
            return;
        }

        int laneCount = chart.laneCount > 0 ? chart.laneCount : 4;

        for (int i = 0; i < chart.notes.Count; i++)
        {
            NoteData note = chart.notes[i];

            // Căn giữa preview giống runtime: startX = -totalWidth/2
            float totalWidth = (laneCount - 1) * laneSpacing;
            float startX = -totalWidth / 2f;

            Vector3 position = new Vector3(
                startX + note.lane * laneSpacing,
                note.time * timeSpacing,
                0f
            );

            GameObject noteObject = Instantiate(notePrefab, position, Quaternion.identity, noteParent);

            ChartPreviewNote previewNote = noteObject.GetComponent<ChartPreviewNote>();

            if (previewNote == null)
            {
                previewNote = noteObject.AddComponent<ChartPreviewNote>();
            }

            previewNote.Initialize(i, note, this);
        }
    }

    public void Clear()
    {
        if (noteParent == null)
        {
            return;
        }

        for (int i = noteParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(noteParent.GetChild(i).gameObject);
        }
    }

    public void UpdateNoteFromPreview(ChartPreviewNote previewNote)
    {
        NoteData note = previewNote.NoteData;

        // Tính lại lane từ vị trí đã căn giữa (giống công thức Draw)
        int laneCount = currentChart != null && currentChart.laneCount > 0
            ? currentChart.laneCount : 4;
        float totalWidth = (laneCount - 1) * laneSpacing;
        float startX = -totalWidth / 2f;

        int lane = Mathf.RoundToInt((previewNote.transform.position.x - startX) / laneSpacing);
        float time = previewNote.transform.position.y / timeSpacing;

        // --- BEAT SNAPPING ---
        if (Settings != null && Settings.bpm > 0 && Settings.subdivision > 0)
        {
            float secondsPerBeat = 60f / Settings.bpm;
            float snapInterval = secondsPerBeat / Settings.subdivision;
            
            // Làm tròn time theo snapInterval
            time = Mathf.Round(time / snapInterval) * snapInterval;
        }
        // ---------------------

        lane = Mathf.Clamp(lane, 0, laneCount - 1);
        time = Mathf.Max(0f, time);

        note.lane = lane;
        note.time = time;

        // Snap về đúng vị trí lane
        previewNote.transform.position = new Vector3(
            startX + note.lane * laneSpacing,
            note.time * timeSpacing,
            0f
        );
    }

    public ChartData GetCurrentChart()
    {
        return currentChart;
    }
}