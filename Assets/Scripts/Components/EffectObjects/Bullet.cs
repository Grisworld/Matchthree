using UnityEngine;

namespace Components.EffectObjects
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        public SpriteRenderer GetSprite()
        {
            return _spriteRenderer;
        }

    }
}