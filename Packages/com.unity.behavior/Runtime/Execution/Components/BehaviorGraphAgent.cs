using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Behavior.GraphFramework;
using UnityEngine;

#if NETCODE_FOR_GAMEOBJECTS
using Unity.Netcode;
#endif

namespace Unity.Behavior
{
    [AddComponentMenu("AI/Behavior Agent")]
    public class BehaviorGraphAgent : BehaviorGraphAgentBase, ISerializationCallbackReceiver
    {
        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            StartGraph();
        }

        private void Update()
        {
            UpdateGraph();
        }

        public override void StartGraph()
        {
            if (Graph == null) return;
#if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif

            if (!isActiveAndEnabled)
            {
                if (!IsInitialised)
                {
                    return;
                }

                if (Graph.IsRunning)
                {
                    return;
                }
                Graph.End();
                IsStarted = false;
                return;
            }

            if (!IsInitialised)
            {
                Init();
            }
            if (Graph.IsRunning)
            {
                return;
            }
            Graph.Start();
            IsStarted = true;
        }

        public override void UpdateGraph()
        {
            if (Graph == null)
                return;

#if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif
            
            if (!IsInitialised)
            {
                Init();
            }

            if (!IsStarted)
            {
                Graph.Start();
                IsStarted = true;
            }
            Graph.Tick();
        }

        public override void EndGraph()
        {
            if (Graph == null) return;
#if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif
            Graph.End();
        }

        public override void RestartGraph()
        {
            #if NETCODE_FOR_GAMEOBJECTS
            if (!IsOwner && NetcodeRunOnlyOnOwner) return;
#endif
            if (Graph == null)
            {
                Debug.LogError("Can't restart the agent because no graph has been assigned.", this);
                return;
            }

            if (!isActiveAndEnabled)
            {
                if (IsInitialised)
                {
                    Graph.End();
                }
                IsStarted = false;
                return;
            }

            if (!IsInitialised)
            {
                // The graph needs initialising and then starting. The user asked to do it this frame so we do it here
                // instead of waiting for Update().
                Init();
                Graph.Start();
                IsStarted = true;
                return;
            }
            Graph.Restart();
            IsStarted = true;
        }

        protected override BehaviorGraph GetBehaviorGraphInstance() {
            return ScriptableObject.Instantiate(Graph);
        }
    }
}