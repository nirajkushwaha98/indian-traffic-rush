using System;
using System.Collections.Generic;
using UnityEngine;

namespace ITR
{
    /// <summary>Queues callbacks (e.g. from ad SDK threads) onto Unity's main thread.</summary>
    public class MainThread : MonoBehaviour
    {
        static MainThread instance;
        static readonly Queue<Action> queue = new Queue<Action>();
        static readonly object gate = new object();

        public static void Ensure()
        {
            if (instance != null) return;
            var go = new GameObject("MainThread");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MainThread>();
        }

        public static void Run(Action a)
        {
            if (a == null) return;
            lock (gate) queue.Enqueue(a);
        }

        void Update()
        {
            while (true)
            {
                Action a;
                lock (gate)
                {
                    if (queue.Count == 0) break;
                    a = queue.Dequeue();
                }
                try { a(); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
