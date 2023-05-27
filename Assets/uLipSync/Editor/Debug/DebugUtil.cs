using System;
using System.Text;
using System.IO;

namespace uLipSync.Debugging
{

public static class DebugUtil
{
    public static void DumpMfccData(StringBuilder sb, MfccData data)
    {
        foreach (var mcd in data.mfccCalibrationDataList)
        {
            var array = mcd.array;
            for (int i = 0; i < array.Length; ++i)
            {
                sb.Append(array[i]);
                if (i != array.Length - 1) sb.Append(",");
            }
            sb.AppendLine();
        }
    }

    public static void DumpProfile(StringBuilder sb, Profile profile)
    {
        foreach (var mfccData in profile.mfccs)
        {
            sb.Append($"# {mfccData.name}\n");
            DumpMfccData(sb, mfccData);
        }
    }
    
    public static string GetRelativePath(string fromPath, string toPath)
    {
        // ref: https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        var fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
        var toUri = new Uri(AppendDirectorySeparatorChar(toPath));
        if (fromUri.Scheme != toUri.Scheme) return toPath;

        var relativeUri = fromUri.MakeRelativeUri(toUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath;
    }

    private static string AppendDirectorySeparatorChar(string path)
    {
        if (!Path.HasExtension(path) &&
            !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            return path + Path.DirectorySeparatorChar;
        }

        return path;
    }
}

}