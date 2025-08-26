using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PitchVisualizer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioPitchEstimator estimator;
    public LineRenderer lineSRH;
    public LineRenderer lineFrequency;
    public Transform marker;
    public TextMesh textFrequency;
    public TextMesh textMin;
    public TextMesh textMax;
    public Canvas m_Canvas;

    public float estimateRate = 60;
    private int spectrumSize = 1024;
    public Vector3[] lines = new Vector3[1024];

    private int prev_note = -1;
    private int prev_count = 0;
    private int prev_threshold = 2;
    
    private int silence_count = 0;
    private int silence_threshold = 3;

    private int cur_note = -1;
    private int time_count = 0;
    private string filePath = "Assets/Examples/output/test.txt";

    void Start()
    {
        // call at slow intervals (Update() is generally too fast)
        InvokeRepeating(nameof(UpdateVisualizer), 0, 1.0f / estimateRate);

        filePath = "Assets/Examples/output/" + GetTimeDate() + ".txt";
        if (filePath.Length != 0)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("Data start\n");
                writer.Close();
            }
        }
    }

    void UpdateVisualizer()
    {
        // estimate the fundamental frequency
        var frequency = estimator.Estimate(audioSource);

        var spectrum = estimator.Spec;
        for (int i = 0; i < spectrumSize; i++)
        {
            lines[i] = new Vector3(0.15f * Mathf.Log((float)i)-20.5f, 
                    0.02f * (Mathf.Log(spectrum[i]) + 19.5f), -0.1f);
        }

        // check estimates
        textFrequency.text = string.Format("{0}, {1:0.0} Hz", GetNameFromFrequency(frequency), frequency);
        if (filePath.Length != 0)
        {
            using (StreamWriter writer = File.AppendText(filePath))
            {
                writer.WriteLine((++time_count).ToString() + " " + textFrequency.text);
                writer.Close();
            }
        }

        // visualize SRH score
        var srh = estimator.SRH;
        var numPoints = srh.Count;
        var positions = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            var position = (float)i / numPoints - 0.5f;
            var value = srh[i] * 0.005f;
            positions[i].Set(position, value, 0);
        }
        lineSRH.positionCount = numPoints;
        lineSRH.SetPositions(positions);

        // visualize fundamental frequency
        if (float.IsNaN(frequency))
        {
            // hide the line when it does not exist
            lineFrequency.positionCount = 0;

            // indicate the latest frequency with TextMesh
            marker.position = new Vector3(0, 0, 0);
        }
        else
        {
            var min = estimator.frequencyMin;
            var max = estimator.frequencyMax;
            var position = (frequency - min) / (max - min) - 0.5f;

            // indicate the frequency with LineRenderer
            lineFrequency.positionCount = 2;
            lineFrequency.SetPosition(0, new Vector3(position, +1, 0));
            lineFrequency.SetPosition(1, new Vector3(position, -1, 0));

            // indicate the latest frequency with TextMesh
            marker.position = new Vector3(position, 0, 0);
        }

        // visualize lowest/highest frequency
        textMin.text = string.Format("{0} Hz", estimator.frequencyMin);
        textMax.text = string.Format("{0} Hz", estimator.frequencyMax);
    }

    // frequency -> pitch name
    string GetNameFromFrequency(float frequency)
    {
        var new_note = -1;
        if (!float.IsNaN(frequency)) {
            new_note = Mathf.RoundToInt(12 * Mathf.Log(frequency / 440) / Mathf.Log(2) + 69) % 12;
        }

        // Record in history if we currently have a continous note
        if (new_note > -1 && new_note == prev_note) {
            prev_count++;
        } else if (new_note > -1) {
            prev_count = 0;
            prev_note = new_note;
        }

        // Delay silence detection if we had continuous note before
        if (new_note == -1) {
            silence_count++; 
            if (prev_count >= prev_threshold) {
                if (silence_count >= silence_threshold) {
                    cur_note = -1;
                    prev_count = 0;
                    prev_note = -1;
                } else {
                    cur_note = prev_note;
                }
            } else {
                cur_note = -1;
            }
        } else {
            silence_count = 0;
            cur_note = new_note;
        }

        // Return string
        string[] names = {
            "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
        };
        if (cur_note < 0) {
            return "--";
        }
        return names[cur_note];
    }

    string GetTimeDate()
    {
        string dateTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
        return dateTime;
    }

    void OnDrawGizmos()
    {
        //Gizmos.matrix = m_Canvas.transform.localToWorldMatrix;
        
        // Draw raw spectrum
        if (lines.Length > 0)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(lines[i - 1], lines[i]);
            }
        }
    }

}
