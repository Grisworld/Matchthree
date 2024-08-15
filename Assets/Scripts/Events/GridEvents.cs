using System;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
    public class GridEvents
    {
        public UnityAction<Bounds> GridLoaded;
        public UnityAction InputStart;
        public UnityAction InputStop;
        public UnityAction<int, int> MatchGroupDespawn;
        public Func<GameObject, GameObject> InsPrefab;
        public UnityAction PlayerMoved;
    }
}