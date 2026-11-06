using System.Collections.Generic;
using System.Linq;
using ObjectTags;
using Player.Core;
using UnityEngine;

namespace UISystem.HUD
{
    public class NoddCountHUD : MonoBehaviour
    {
        [SerializeField] private Dictionary<ENoddTypes, int> m_noddCountDict;

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
            if (m_noddCountDict.ContainsKey(noddType)) m_noddCountDict[noddType] += 1;
            else m_noddCountDict[noddType] = 1;
        }

        private void RemoveNodd(ENoddTypes noddType)
        {
            m_noddCountDict[noddType] -= 1;
        }
    }
}
