using System;
using ObjectTags;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem.HUD
{
    public class NoddCountElement : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_noddCountText;
        [SerializeField] private Image m_noddCountImage;

        [SerializeField] private Sprite[] m_noddCountImageSprites = new Sprite[0];
        
        private void Awake()
        {
            if (m_noddCountText == null) m_noddCountText = GetComponentInChildren<TMP_Text>();
            if (m_noddCountImage == null) m_noddCountImage = GetComponentInChildren<Image>();
        }

        public void UpdateBackImage(ENoddTypes noddType)
        {
            m_noddCountImage.sprite = m_noddCountImageSprites[(int) noddType];
        }

        public void UpdateCountText(int num)
        {
            m_noddCountText.text = num.ToString();
        }
    }
}
