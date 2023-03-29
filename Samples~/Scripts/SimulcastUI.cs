using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Dolby.Millicast;
public class SimulcastUI : MonoBehaviour
{
    public System.Action<SimulcastLayers> onUpdateSimulcastData;
    [SerializeField]private TMP_InputField bit_rate_input_high;
    [SerializeField]private TMP_InputField bit_rate_input_med;
    [SerializeField]private TMP_InputField bit_rate_input_low;

    private SimulcastLayers simulcastLayersData;

    // Start is called before the first frame update
    void Start()
    {
        simulcastLayersData = new SimulcastLayers();
        onUpdateSimulcastData?.Invoke(simulcastLayersData);
    }

    public void UpdateValues()
    {
        if(long.TryParse(bit_rate_input_high.text, out long bitrate_h))
        {
            simulcastLayersData.High.maxBitrateKbps = (ulong)bitrate_h;
        }
        if(long.TryParse(bit_rate_input_med.text, out long bitrate_m))
        {
            simulcastLayersData.Medium.maxBitrateKbps = (ulong)bitrate_m;
        }
        if(long.TryParse(bit_rate_input_low.text, out long bitrate_l))
        {
            simulcastLayersData.Low.maxBitrateKbps = (ulong)bitrate_l;
        }
        onUpdateSimulcastData?.Invoke(simulcastLayersData);

    }   
}
