﻿//
// GitVersion.cs
//
// Author:
//       Zach Deibert <zachdeibert@gmail.com>
//
// Copyright (c) 2016 Zach Deibert
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using GitSharp;
using log4net;
using Com.Latipium.DevTools.Git;

namespace Com.Latipium.DevTools.Versioning {
    /// <summary>
    /// Gets the version from the git repository.
    /// </summary>
    public class GitVersion {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GitVersion));
        /// <summary>
        /// The version.
        /// </summary>
        public readonly Version Version;

        private Version FindVersion(Commit commit, out int distance) {
            Tag tag = commit.Repository.Tags.Values.FirstOrDefault(t => t.Target.Hash == commit.Hash);
            if (tag != null) {
                try {
                    try {
                        Version raw = new Version(tag.Name);
                        return new Version(Math.Max(0, raw.Major), Math.Max(0, raw.Minor), Math.Max(0, raw.Build), Math.Max(0, raw.Revision));
                    } finally {
                        distance = 0;
                    }
                } catch (Exception) {
                }
            }
            Version version = new Version(0, 0);
            int minDistance = int.MaxValue >> 1;
            bool isRoot = true;
            foreach (Commit parent in commit.Parents) {
                int tmp;
                Version ver = FindVersion(parent, out tmp);
                if (tmp < minDistance) {
                    version = ver;
                    minDistance = tmp;
                    isRoot = false;
                }
            }
            distance = isRoot ? 0 : minDistance + 1;
            return new Version(version.Major, version.Minor, version.Build + 1, 0);
        }

        private Version GetVersion(Repository repo) {
            Commit commit = repo.Head.CurrentCommit;
            int tmp;
            Version version = FindVersion(commit, out tmp);
            return new Version(version.Major, version.Minor, version.Build, (int) Math.Round((DateTimeOffset.Now - commit.AuthorDate).TotalMinutes) % 65536);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Com.Latipium.DevTools.Versioning.GitVersion"/> class.
        /// </summary>
        /// <param name="gitDir">The git directory.</param>
        public GitVersion(string gitDir) {
            Version = GetVersion(gitDir.OpenRepository());
            Log.DebugFormat("Detected version {0}", Version);
        }
    }
}

