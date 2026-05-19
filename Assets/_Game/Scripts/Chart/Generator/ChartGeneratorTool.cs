using UnityEngine;

public class ChartGeneratorTool : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private ChartGenerationSettings generationSettings;
    [SerializeField] private AudioSource musicSource;

    [Header("Save")]
    [SerializeField] private string saveFileName = "test_chart";

    [Header("Preview Optional")]
    [SerializeField] private ChartVisualizer visualizer;

    public void ApplySongData(SongData song)
    {
        if (song == null) return;

        // --- Audio ---
        if (musicSource != null && song.audioClip != null)
            musicSource.clip = song.audioClip;

        // --- Chart file name ---
        if (!string.IsNullOrEmpty(song.ComputedChartFileName))
            saveFileName = song.ComputedChartFileName;

        // --- Kết nối BPM từ SongData → ChartGenerationSettings ---
        // SongData._bpm là BPM chuẩn của bài nhạc (nhập tay hoặc từ Auto Detect).
        // Nó sẽ pre-fill vào ChartGenerationSettings để không phải nhập lại.
        // Vẫn có thể Override tay trong ChartGenerationSettings sau đó.
        if (generationSettings != null && song._bpm > 0f)
        {
            generationSettings.bpm = song._bpm;
        }

        // --- Kết nối Difficulty từ SongData → ChartGenerationSettings ---
        // SongData._difficulty ("Easy"/"Normal"/"Hard") map vào enum preset.
        if (generationSettings != null && !string.IsNullOrEmpty(song._difficulty))
        {
            generationSettings.difficulty = ParseDifficulty(song._difficulty);
        }

        Debug.Log($"ChartGeneratorTool: Đã chọn '{song._songTitle}' " +
                  $"| BPM={song._bpm} | Difficulty={song._difficulty} " +
                  $"| Chart='{song.ComputedChartFileName}'");
    }

    /// <summary>
    /// Chuyển chuỗi độ khó ("Easy"/"Normal"/"Hard") thành enum ChartDifficultyPreset.
    /// </summary>
    private ChartDifficultyPreset ParseDifficulty(string diff)
    {
        switch (diff.Trim().ToLower())
        {
            case "easy":   return ChartDifficultyPreset.Easy;
            case "hard":   return ChartDifficultyPreset.Hard;
            default:       return ChartDifficultyPreset.Normal;
        }
    }

    public void GenerateAndSave()
    {
        if (generationSettings == null)
        {
            Debug.LogError("Generation settings is missing.");
            return;
        }

        if (musicSource == null || musicSource.clip == null)
        {
            Debug.LogError("Music source or AudioClip is missing.");
            return;
        }

        generationSettings.ApplyPreset();

        string songName = musicSource.clip.name;
        float songLength = musicSource.clip.length;

        ChartData chart = SimpleChartGenerator.Generate(
            songName,
            generationSettings.bpm,
            songLength,
            generationSettings.offset,
            generationSettings.laneCount,
            generationSettings.difficulty
        );

        ChartSaveLoad.Save(chart, saveFileName);

        Debug.Log($"Generated and saved chart: {chart.songName} | Notes: {chart.notes.Count}");

        if (visualizer != null)
        {
            visualizer.Settings = generationSettings;
            visualizer.Draw(chart);
        }
    }

    public void LoadAndPreview()
    {
        if (visualizer == null)
        {
            Debug.LogError("Visualizer is missing.");
            return;
        }

        if (!BeatmapParser.TryLoadChart(saveFileName, out ChartData loadedChart))
        {
            Debug.LogError($"Failed to load chart preview: {saveFileName}");
            return;
        }

        visualizer.Settings = generationSettings;
        visualizer.Draw(loadedChart);

        Debug.Log($"Loaded preview chart: {loadedChart.songName} | Notes: {loadedChart.notes.Count}");
    }

    public void AutoDetectBpm()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            Debug.LogError("ChartGeneratorTool: Music source or AudioClip is missing.");
            return;
        }

        if (generationSettings == null)
        {
            Debug.LogError("ChartGeneratorTool: Generation Settings is missing.");
            return;
        }

        float detectedBpm = BpmDetector.Detect(musicSource.clip);
        generationSettings.bpm = detectedBpm;

        Debug.Log($"ChartGeneratorTool: Auto-detected BPM = {detectedBpm:F1} " +
                  $"và đã điền vào Generation Settings.");
    }

    public void SaveEditedChart()
    {
        if (visualizer == null)
        {
            Debug.LogError("Visualizer is missing.");
            return;
        }

        ChartData chart = visualizer.GetCurrentChart();

        if (chart == null)
        {
            Debug.LogWarning("No chart loaded to save.");
            return;
        }

        ChartSaveLoad.Save(chart, saveFileName);

        Debug.Log($"Saved edited chart: {chart.songName} | Notes: {chart.notes.Count}");
    }
}