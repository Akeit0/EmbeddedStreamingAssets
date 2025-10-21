using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

public class LoadTest : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI _text1;

    [SerializeField]
    TextMeshProUGUI _text2;

    async void Start()
    {
        Application.targetFrameRate = 60;
        await LoadAsync();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async Awaitable LoadAsync()
    {
        try
        {
            using var request = UnityWebRequest.Get("StreamingAssets/sample.txt");
            await request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                _text1.text = "Error: " + request.error;
            }
            else
            {
                _text1.text = request.downloadHandler.text;
            }
        }
        catch (System.Exception ex)
        {
            _text1.text = "Error: " + ex.Message;
        }

        try
        {
            var data = await Addressables.LoadAssetAsync<TextAsset>("test2.txt").Task;
            _text2.text = data?.text ?? "Loaded null";
        }
        catch (System.Exception ex)
        {
            _text2.text = "Error: " + ex.Message;
        }


    }
}