using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace MeraBrand.Expo.Core
{
    // Evaluated by the Addressables Remote.LoadPath profile variable at runtime.
    // The generated RemoteContent folder is deployed beside the WebGL index page.
    [Preserve]
    public static class RemoteExhibitionContentUrl
    {
        public static string BaseUrl
        {
            [Preserve]
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                string pageUrl = Application.absoluteURL;
                Uri.TryCreate(pageUrl, UriKind.Absolute, out Uri pageUri);

                string streamingAssetsPath = Application.streamingAssetsPath;
                if (!string.IsNullOrEmpty(streamingAssetsPath))
                {
                    Uri streamingUri = null;
                    if (!Uri.TryCreate(streamingAssetsPath, UriKind.Absolute, out streamingUri) && pageUri != null)
                        Uri.TryCreate(pageUri, streamingAssetsPath, out streamingUri);

                    if (streamingUri != null)
                    {
                        var streamingDirectory = new Uri(streamingUri.AbsoluteUri.TrimEnd('/') + "/");
                        return new Uri(streamingDirectory, "../RemoteContent/WebGL/").AbsoluteUri;
                    }
                }

                if (pageUri != null)
                    return new Uri(pageUri, "RemoteContent/WebGL/").AbsoluteUri;
#endif
                return "RemoteContent/WebGL/";
            }
        }
    }
}
