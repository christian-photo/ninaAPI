#region "copyright"

/*
    Copyright © 2025 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"


using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ninaAPI.Utility
{
    public static class FileSystemHelper
    {
        public static List<string> GetFilesRecursively(string path)
        {
            List<string> files = [.. Directory.GetFiles(path)];
            foreach (string dir in Directory.GetDirectories(path))
            {
                files.AddRange(GetFilesRecursively(dir));
            }
            return files;
        }

        public static string GetCapturePngPath() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"temp-{Environment.ProcessId}.png");
        public static string GetThumbnailFolder() => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"thumbnails-{Environment.ProcessId}");
    }
}
