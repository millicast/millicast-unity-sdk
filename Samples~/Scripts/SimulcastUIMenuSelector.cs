using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;
using System;
namespace Dolby.Millicast
{

    /// <summary>
    /// Routinely fetch the layer info of the selected subscriber
    /// 
    /// </summary>
    internal class SimulcastUIMenuSelector : MonoBehaviour
    {
        internal class TextMenu<T>
        {
            public readonly TMP_Dropdown itemDropdown;
            private List<KeyValuePair<string, T>> _items = new List<KeyValuePair<string, T>>();

            public TextMenu(TMP_Dropdown menu)
            {
                itemDropdown = menu;
            }

            public void Add(T item, string name)
            {
                // Search for it before
                var index = _items.FindIndex(i => i.Key == name);
                if (index >= 0)
                {
                    _items[index] = new KeyValuePair<string, T>(name, item);
                    return;
                }

                _items.Add(new KeyValuePair<string, T>(name, item));
                itemDropdown.options = new List<TMP_Dropdown.OptionData>();
                foreach(var pair in _items)
                {
                    itemDropdown.options.Add(new TMP_Dropdown.OptionData(pair.Key));
                }
                itemDropdown.value = -1; // optional
                itemDropdown.Select(); // optional
                itemDropdown.RefreshShownValue(); // this is the key
            }

            public void SetCurrentValue(T item)
            {    
                itemDropdown.SetValueWithoutNotify(_items.FindIndex(i => i.Value.Equals(item)));
            }

            public T At(int index)
            {
                return _items[index].Value;
            }

            public void Reset()
            {
                _items.Clear();
                itemDropdown.ClearOptions();
            }
        }


        [SerializeField]
        private TMP_Dropdown _subscriberDropdown;

        [SerializeField]
        private TMP_Dropdown _layerDropdown;

        private TextMenu<McSubscriber> _subscriberTextMenu;
        private TextMenu<Layer> _layerTextMenu;

        private McSubscriber _currentSubscriber;
        private void PopulateSubscribers()
        {
            var ss = FindObjectsOfType<McSubscriber>();
            if (ss == null) return;
            _currentSubscriber = ss[0];
            foreach (McSubscriber s in ss)
            {
                _subscriberTextMenu.Add(s, "Subscriber: " + s.gameObject.name);
            }

            _subscriberDropdown.onValueChanged.AddListener(ChangedSubscriberSelection);
            _layerDropdown.onValueChanged.AddListener(ChangedLayerSelection);
        }

        void RefreshLayerInfo(Layer[] layers)
        {
            if (layers == null) return;
            bool alreadyHaveHigh = false;
            Dictionary<string, Layer> selectedLayers = new Dictionary<string, Layer>();
            foreach (var layer in layers)
            {
                string layerName = ParseSimulcastLayerId(layer.EncodingId);
                if (layerName == "High") alreadyHaveHigh = true;
                if (alreadyHaveHigh && layerName != "High")
                    layerName = ParseSimulcastLayerId(layer.EncodingId, true);

                if (!selectedLayers.ContainsKey(layerName) || (layer.Bitrate > selectedLayers[layerName].Bitrate))
                {
                    selectedLayers[layerName] = layer;
                }
            }

            foreach(var pair in selectedLayers)
            {
                _layerTextMenu.Add(pair.Value, "Simulcast Layer: " + pair.Key);
            }

        }

        /// <summary>
        /// Get Meaningful layer name
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        string ParseSimulcastLayerId(string layer, bool alreadyHaveHigh = false)
        {
            string lowerCaseLayer = layer.ToLower();
            switch (lowerCaseLayer)
            {
                case "l":
                    return alreadyHaveHigh ? "Low" : "High";

                case "2":
                case "h":
                case "high":
                case "large":
                    if (alreadyHaveHigh && lowerCaseLayer == "l") return layer;
                    return "High";

                case "1":
                case "m":
                case "medium":
                case "mid":
                    return "Medium";

                case "0":
                case "s":
                case "low":
                case "small":
                    return "Low";

                default:
                    return layer;
            }
        }

        void ChangedSubscriberSelection(int index)
        {
            _currentSubscriber = _subscriberTextMenu.At(index);
            _layerTextMenu.Reset();
        }

        void ChangedLayerSelection(int index)
        {
            var layer = _layerTextMenu.At(index);
            _currentSubscriber.SetSimulcastLayer(layer);
        }

        // Start is called before the first frame update
        void Start()
        {
            _subscriberDropdown.ClearOptions();
            _layerDropdown.ClearOptions();
            _subscriberTextMenu = new TextMenu<McSubscriber>(_subscriberDropdown);
            _layerTextMenu = new TextMenu<Layer>(_layerDropdown);
            PopulateSubscribers();
        }

        // Update is called once per frame
        void Update()
        {
            // Update the simulcast layer information of the current
            // subscriber
            if (_currentSubscriber != null)
            {
                var layers = _currentSubscriber.GetSimulcastLayers();
                if (layers == null) return;
                RefreshLayerInfo(layers);
            }
        }
    }
}
