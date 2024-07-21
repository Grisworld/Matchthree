using UnityEngine.Events;

namespace Events
{
    public class SoundEvents
    {
        public UnityAction<int, float, bool, int> Play;
        public UnityAction Stop;
    }
}