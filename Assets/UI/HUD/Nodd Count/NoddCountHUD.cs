using System.Collections.Generic;
using System.Linq;
using ObjectTags;
using Player.Core;
using UnityEngine;

namespace UISystem.HUD
{
    public class NoddCountHUD : MonoBehaviour
    {
        [SerializeField] private NoddCountElement m_noddCountElementObj;
        
        [SerializeField] private Dictionary<ENoddTypes, int> m_noddCountDict;
        [SerializeField] private Dictionary<ENoddTypes, NoddCountElement> m_noddCountElements;

        private PlayerFollowerController m_playerFollowerController;

        private void Start()
        {
            m_playerFollowerController = FindObjectsByType<PlayerFollowerController>(FindObjectsInactive.Include)
                .FirstOrDefault();

            m_playerFollowerController.OnFollowerAdd += AddNodd;
            m_playerFollowerController.OnFollowerRemove += RemoveNodd;
        }

        private void AddNodd(ENoddTypes noddType)
        {
            if (m_noddCountDict.ContainsKey(noddType))
            {
                if (m_noddCountDict[noddType] == 0) m_noddCountElements[noddType].gameObject.SetActive(true);
                m_noddCountDict[noddType] += 1;
                m_noddCountElements[noddType].UpdateCountText(m_noddCountDict[noddType]);
            }
            else
            {
                NoddCountElement element = Instantiate(m_noddCountElementObj, m_noddCountElementObj.transform.parent);
                element.gameObject.SetActive(true);
                element.UpdateCountText(1);
                element.UpdateBackImage(noddType);

                m_noddCountElements[noddType] = element;
                m_noddCountDict[noddType] = 1;
            }
        }

        private void RemoveNodd(ENoddTypes noddType)
        {
            m_noddCountDict[noddType] -= 1;
            m_noddCountElements[noddType].UpdateCountText(m_noddCountDict[noddType]);
            if (m_noddCountDict[noddType] == 0) m_noddCountElements[noddType].gameObject.SetActive(false);
        }
    }
}
