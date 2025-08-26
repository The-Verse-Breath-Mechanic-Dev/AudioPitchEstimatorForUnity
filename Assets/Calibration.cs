using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class Calibration : MonoBehaviour {
	public Button guiBtn;
    public AudioSource audioSource;
    public AudioPitchEstimator estimator;

    private float estimateRate = 30.0f;
    private int record_flag;
    private int record_ct;
    const int spectrumSize = 1024;

	void Start () {
        record_flag = 0;

		Button btn = guiBtn.GetComponent<Button>();
		btn.onClick.AddListener(TaskOnClick);

        for (int i = 0; i < spectrumSize; i++) {
            estimator.noise_spec[i] = 0f;
        }
	}

	void TaskOnClick(){
        //record_flag = (record_flag + 1) % 2;
        if (record_flag == 1) {
            record_flag = 0;
        } else {
            record_flag = 1;
            for (int i = 0; i < spectrumSize; i++) {
                estimator.noise_spec[i] = 0f;
            }
        }
	}

    void Update()
    {
        if (record_flag == 1) {
            record_ct = record_ct + 1;

            // estimate the fundamental frequency
            var frequency = estimator.Estimate(audioSource);
            var spectrum = estimator.Spec;
            for (int i = 0; i < spectrumSize; i++)
            {
                float prev_noise = estimator.noise_spec[i];
                estimator.noise_spec[i] = (prev_noise * (record_ct - 1) + spectrum[i]) / record_ct;
            }
        } else {
            record_ct = 0;
        }
    }
}