#region "copyright"

/*
    Copyright © 2024 Christian Palm (christian@palm-family.de)
    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.IO;

public static class FileSystemHelper
{
    public static string[] GetFilesRecursively(string path)
    {
        string[] files = Directory.GetFiles(path);
        foreach (string dirPath in Directory.GetDirectories(path))
        {
            files = [.. files, .. GetFilesRecursively(dirPath)];
        }

        return files;
    }
}