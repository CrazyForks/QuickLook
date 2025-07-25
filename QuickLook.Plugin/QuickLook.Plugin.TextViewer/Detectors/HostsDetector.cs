﻿// Copyright © 2017-2025 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;

namespace QuickLook.Plugin.TextViewer.Detectors;

public sealed class HostsDetector : IFormatDetector
{
    public string Name => "Hosts";

    public string Extension => ".hosts";

    public bool Detect(string path, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var fileName = Path.GetFileName(path).AsSpan();
        return "hosts".AsSpan().Equals(fileName, StringComparison.OrdinalIgnoreCase);
    }
}
