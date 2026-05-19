using UnityEngine;

/// <summary>
/// Tạo chart note dựa trên BPM và cấu hình độ khó.
///
/// Cải tiến mới: Sử dụng Deterministic Pattern thay vì Random.
/// - Easy: Cứ 2 nhịp đẻ 1 nốt (tỉ lệ nhỏ ra double).
/// - Normal: Cứ mỗi nhịp đẻ 1 nốt, mỗi 4 nhịp ra double.
/// - Hard: Pattern nhịp điệu (Double -> Single -> Offbeat -> Double...).
/// </summary>
public static class SimpleChartGenerator
{

    public static ChartData Generate(
        string songName,
        float bpm,
        float songLength,
        float offset,
        int laneCount,
        ChartDifficultyPreset difficulty)
    {
        ChartData chart = new ChartData
        {
            songName = songName,
            bpm = bpm,
            offset = offset,
            laneCount = laneCount
        };

        if (bpm <= 0f || songLength <= 0f || laneCount <= 0)
        {
            Debug.LogError("SimpleChartGenerator: Invalid chart generation settings.");
            return chart;
        }

        float beatDuration = 60f / bpm;
        // Mỗi step là nửa nhịp (1/2 beat - 8th notes) để hỗ trợ off-beat
        float stepDuration = beatDuration / 2f; 

        int previousLane = -1;
        float previousNoteTime = -999f;
        float minSameLaneInterval = beatDuration;

        int stepIndex = 0;

        while (true)
        {
            float time = stepIndex * stepDuration;
            if (time >= songLength) break;

            bool shouldSpawn = false;
            bool isDouble = false;

            switch (difficulty)
            {
                case ChartDifficultyPreset.Easy:
                    // Easy: Cứ 2 nhịp đẻ 1 nốt (tương đương 4 steps)
                    if (stepIndex % 4 == 0)
                    {
                        shouldSpawn = true;
                        // 15% ra double note (theo yêu cầu)
                        if (Random.value < 0.15f) isDouble = true; 
                    }
                    break;

                case ChartDifficultyPreset.Normal:
                    // Normal: Mỗi 1 nhịp đẻ 1 nốt (2 steps)
                    if (stepIndex % 2 == 0)
                    {
                        shouldSpawn = true;
                        // Mỗi 4 nhịp (8 steps) ra double note
                        if (stepIndex % 8 == 0) isDouble = true;
                    }
                    break;

                case ChartDifficultyPreset.Hard:
                    // Hard: 4-beat rhythm pattern (8 steps)
                    int patternStep = stepIndex % 8;
                    // Step 0: Double (Beat 1)
                    // Step 2: Single (Beat 2)
                    // Step 3: Single (Beat 2.5 - offbeat)
                    // Step 4: Double (Beat 3)
                    // Step 6: Single (Beat 4)
                    // Step 7: Single (Beat 4.5 - offbeat)
                    if (patternStep == 0 || patternStep == 4)
                    {
                        shouldSpawn = true;
                        isDouble = true;
                    }
                    else if (patternStep == 2 || patternStep == 3 || patternStep == 6 || patternStep == 7)
                    {
                        shouldSpawn = true;
                        isDouble = false;
                    }
                    break;
            }

            if (shouldSpawn)
            {
                int lane1 = PickLane(laneCount, previousLane, time, previousNoteTime, minSameLaneInterval);
                AddNoteToChart(chart, time, lane1);

                previousLane = lane1;
                previousNoteTime = time;

                if (isDouble && laneCount > 1)
                {
                    // Khoảng cách same-lane = 0 vì cùng xuất hiện 1 thời điểm
                    int lane2 = PickLane(laneCount, previousLane, time, previousNoteTime, 0f);
                    AddNoteToChart(chart, time, lane2);
                }
            }

            stepIndex++;
        }

        return chart;
    }

    private static void AddNoteToChart(ChartData chart, float time, int lane)
    {
        NoteData note = new NoteData
        {
            time           = time,
            lane           = lane,
            type           = NoteType.Tap,
            duration       = 0f,
            flickDirection = FlickDirection.Any,
            slidePath      = null
        };
        chart.notes.Add(note);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static int PickLane(
        int laneCount,
        int previousLane,
        float currentTime,
        float previousNoteTime,
        float minSameLaneInterval)
    {
        if (laneCount <= 1)
            return 0;

        int lane = Random.Range(0, laneCount);

        bool tooSoonSameLane =
            lane == previousLane &&
            currentTime - previousNoteTime < minSameLaneInterval;

        if (!tooSoonSameLane)
            return lane;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            lane = Random.Range(0, laneCount);
            if (lane != previousLane)
                return lane;
        }

        return (previousLane + 1) % laneCount;
    }
}