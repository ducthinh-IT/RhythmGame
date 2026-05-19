#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChartGeneratorTool))]
public class ChartGeneratorToolEditor : Editor
{
    // Cache danh sách SongData để không load lại mỗi frame
    private SongData[] _allSongs;
    private string[]   _songDisplayNames;
    private int        _selectedIndex = 0; // 0 = "-- Chọn bài --"

    private void OnEnable()
    {
        RefreshSongList();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChartGeneratorTool tool = (ChartGeneratorTool)target;

        GUILayout.Space(12);
        EditorGUILayout.LabelField("── Song Selection ──────────────────", EditorStyles.boldLabel);

        // Nút refresh nếu vừa thêm bài mới
        if (GUILayout.Button("↻ Làm mới danh sách bài nhạc", GUILayout.Height(22)))
        {
            RefreshSongList();
        }

        if (_allSongs == null || _allSongs.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Chưa có SongData nào.\nRight click Project → Create → Data → SongData",
                MessageType.Info);
        }
        else
        {
            // Dropdown chọn bài
            int newIndex = EditorGUILayout.Popup("Chọn bài nhạc", _selectedIndex, _songDisplayNames);

            if (newIndex != _selectedIndex && newIndex > 0)
            {
                SongData chosenSong = _allSongs[newIndex - 1]; // -1 vì index 0 là "-- Chọn bài --"

                // Hiện cảnh báo xác nhận trước khi đổi (tránh mất chart chưa lưu)
                bool confirmed = EditorUtility.DisplayDialog(
                    "Đổi bài nhạc?",
                    $"Chuyển sang \"{chosenSong._songTitle}\".\n\n" +
                    "Nếu chưa bấm 'Save Edited Chart', các chỉnh sửa trên chart hiện tại sẽ bị mất.",
                    "Đổi bài",
                    "Hủy");

                if (confirmed)
                {
                    _selectedIndex = newIndex;
                    Undo.RecordObject(tool, "Select Song");
                    tool.ApplySongData(chosenSong);
                    EditorUtility.SetDirty(tool);

                    // Đồng bộ ChartNoteSpawner.chartFileName để Play mode load đúng JSON.
                    ChartNoteSpawner spawner = FindObjectOfType<ChartNoteSpawner>();
                    if (spawner != null)
                    {
                        Undo.RecordObject(spawner, "Select Song - Sync Spawner");
                        spawner.SetChartFileName(chosenSong.ComputedChartFileName);
                        EditorUtility.SetDirty(spawner);
                        Debug.Log($"ChartGeneratorToolEditor: Đã đồng bộ ChartNoteSpawner → '{chosenSong.ComputedChartFileName}'");
                    }
                }
            }
            else
            {
                _selectedIndex = newIndex;
            }

            // Preview chart filename sẽ được tạo
            if (_selectedIndex > 0)
            {
                SongData currentSong = _allSongs[_selectedIndex - 1];
                string preview = string.IsNullOrEmpty(currentSong.ComputedChartFileName)
                    ? "(Chưa gán AudioClip vào SongData)"
                    : $"→ Sẽ lưu: {currentSong.ComputedChartFileName}.json";

                EditorGUILayout.HelpBox(preview, MessageType.None);
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("── Actions ─────────────────────────", EditorStyles.boldLabel);

        if (GUILayout.Button("Auto Detect BPM"))      tool.AutoDetectBpm();

        GUILayout.Space(4);

        if (GUILayout.Button("Generate And Save Chart")) tool.GenerateAndSave();
        if (GUILayout.Button("Load Preview Chart"))      tool.LoadAndPreview();
        if (GUILayout.Button("Save Edited Chart"))       tool.SaveEditedChart();
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    private void RefreshSongList()
    {
        string[] guids = AssetDatabase.FindAssets("t:SongData");

        _allSongs = new SongData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            _allSongs[i] = AssetDatabase.LoadAssetAtPath<SongData>(path);
        }

        // Index 0 là placeholder "-- Chọn bài --"
        _songDisplayNames = new string[_allSongs.Length + 1];
        _songDisplayNames[0] = "-- Chọn bài nhạc --";
        for (int i = 0; i < _allSongs.Length; i++)
        {
            string title  = _allSongs[i]._songTitle;
            string file   = _allSongs[i].ComputedChartFileName;
            string label  = string.IsNullOrEmpty(title) ? _allSongs[i].name : title;
            string suffix = string.IsNullOrEmpty(file)  ? " (chưa gán AudioClip)" : $"  [{file}]";
            _songDisplayNames[i + 1] = label + suffix;
        }

        _selectedIndex = 0;
    }
}
#endif