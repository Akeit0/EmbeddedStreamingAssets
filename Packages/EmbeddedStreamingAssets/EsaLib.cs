#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
using Unity.Collections.LowLevel.Unsafe;

namespace EmbeddedStreamingAssets
{
    public class EsaLib
    {
        [DllImport("__Internal")]
        static extern void EsaLib_Init(Action<string> fnPtr);

        [DllImport("__Internal")]
        static extern void EsaLib_Resolve(IntPtr dataPtr, int length);

        [DllImport("__Internal")]
        static extern void EsaLib_Reject(string message);

        static Action<string> handlerCache;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Init()
        {
            handlerCache = HandleRequestFromJS;
            EsaLib_Init(handlerCache);
        }

        [MonoPInvokeCallback(typeof(Action<string, string>))]
        static void HandleRequestFromJS(string relPath)
        {
            relPath = relPath.Replace("\\", "/");
            try
            {
                var asset = Resources.Load<TextAsset>("EmbeddedSA/"+relPath) ;
                if (asset !=null)
                {
                    var data = asset.GetData<byte>(); 
                    unsafe
                    {
                        var ptr = (IntPtr)data.GetUnsafeReadOnlyPtr();
                        EsaLib_Resolve(ptr, data.Length);
                    }
                    Resources.UnloadAsset(asset);
                }
                else
                {
                    EsaLib_Reject("Not found: " + relPath);
                }
            }
            catch (Exception e)
            {
                EsaLib_Reject(e.Message);
            }
        }
    }
}

#endif