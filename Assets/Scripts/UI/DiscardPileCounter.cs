using UnityEngine;
using TMPro;
using ProjectM.Skills;

namespace ProjectM.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class DiscardPileCounter : MonoBehaviour
    {
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            if (_text != null && SkillHandManager.Instance != null)
            {
                _text.text = SkillHandManager.Instance.DiscardPileCount.ToString();
            }
        }
    }
}
