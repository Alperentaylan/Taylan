using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

public static class TaylanWebGLBuildPostprocessor
{
    private const string Isaret = "TAYLAN_RESPONSIVE_WEBGL";

    [PostProcessBuild(1000)]
    public static void BuildSonrasi(BuildTarget hedef, string ciktiYolu)
    {
        if (hedef != BuildTarget.WebGL)
            return;

        string indexYolu = Path.Combine(ciktiYolu, "index.html");
        if (!File.Exists(indexYolu))
            return;

        string html = File.ReadAllText(indexYolu);
        if (html.Contains(Isaret))
            return;

        const string stil = @"
    <style id=""TAYLAN_RESPONSIVE_WEBGL"">
      html, body { width: 100%; height: 100%; overflow: hidden; background: #06080d; }
      #unity-container.unity-desktop { position: fixed; inset: 0; transform: none; }
      #unity-canvas { width: 100% !important; height: 100% !important; display: block; }
      #unity-footer { position: absolute; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,.35); }
    </style>";

        html = html.Replace("</head>", stil + Environment.NewLine + "  </head>");
        html = html.Replace(
            "showBanner: unityShowBanner,",
            "showBanner: unityShowBanner," + Environment.NewLine +
            "        devicePixelRatio: Math.min(window.devicePixelRatio || 1, 1.5),"
        );
        html = html.Replace(
            "canvas.style.width = \"960px\";",
            "canvas.style.width = \"100vw\";"
        );
        html = html.Replace(
            "canvas.style.height = \"600px\";",
            "canvas.style.height = \"100vh\";"
        );

        File.WriteAllText(indexYolu, html);
    }
}
