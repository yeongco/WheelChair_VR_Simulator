using UnityEngine;

namespace Obi.Samples
{
    public class TangledPegSlot : MonoBehaviour
    {
        public TangledPeg currentPeg;
        public Color tintColor;

        /// <summary>
        /// TangledPeg이 이 슬롯에 붙어있는지 확인하는 변수
        /// </summary>
        public bool HasPegAttached => currentPeg != null;

        private Material instance;
        private Color normalColor;

        public void Awake()
        {
            instance = GetComponent<Renderer>().material;
            normalColor = instance.color;
        }

        public void Tint()
        {
            instance.color = tintColor;
        }

        public void ResetColor()
        {
            instance.color = normalColor;
        }

        public void OnDestroy()
        {
            Destroy(instance);
        }
    }
}
