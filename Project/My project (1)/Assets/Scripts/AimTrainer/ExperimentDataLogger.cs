using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

namespace VRAdaptation.AimTrainer
{
    public class ExperimentDataLogger : MonoBehaviour
    {
        public static ExperimentDataLogger Instance { get; private set; }

        private string m_FilePath;
        private List<string> m_LogBuffer = new List<string>();
        private bool m_IsLogging = false;

        [Header("Settings")]
        [SerializeField] string m_FileNamePrefix = "VR_Experiment_Log";

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartLogging(string conditionName)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{m_FileNamePrefix}_{conditionName}_{timestamp}.csv";
            m_FilePath = Path.Combine(Application.persistentDataPath, fileName);

            // Write Header
            string header = "Timestamp,Condition,Event,ReactionTime_ms,TargetPosX,TargetPosY,TargetPosZ,HeadRotX,HeadRotY,HeadRotZ";
            File.WriteAllText(m_FilePath, header + Environment.NewLine);
            
            m_IsLogging = true;
            Debug.Log($"[DataLogger] Started logging to: {m_FilePath}");
        }

        public void StopLogging()
        {
            m_IsLogging = false;
            FlushBuffer();
            Debug.Log("[DataLogger] Logging stopped and saved.");
        }

        public void LogEvent(string condition, string eventName, float reactionTime = 0f, Vector3 targetPos = default, Vector3 headRot = default)
        {
            if (!m_IsLogging) return;

            string logEntry = $"{DateTime.Now:HH:mm:ss.fff},{condition},{eventName},{reactionTime:F2}," +
                              $"{targetPos.x:F2},{targetPos.y:F2},{targetPos.z:F2}," +
                              $"{headRot.x:F2},{headRot.y:F2},{headRot.z:F2}";
            
            m_LogBuffer.Add(logEntry);

            // Flush periodically to avoid data loss on crash
            if (m_LogBuffer.Count >= 10)
            {
                FlushBuffer();
            }
        }

        private void FlushBuffer()
        {
            if (m_LogBuffer.Count == 0) return;

            try
            {
                using (StreamWriter sw = File.AppendText(m_FilePath))
                {
                    foreach (string line in m_LogBuffer)
                    {
                        sw.WriteLine(line);
                    }
                }
                m_LogBuffer.Clear();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataLogger] Failed to write to file: {e.Message}");
            }
        }

        void OnApplicationQuit()
        {
            if (m_IsLogging) StopLogging();
        }
    }
}
