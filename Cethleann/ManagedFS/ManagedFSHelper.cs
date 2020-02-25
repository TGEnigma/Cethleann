﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cethleann.Structure;
using JetBrains.Annotations;

namespace Cethleann.ManagedFS
{
    /// <summary>
    ///     Helper Class for ManagedFS
    /// </summary>
    [PublicAPI]
    public static class ManagedFSHelper
    {
        /// <summary>
        ///     Parse a CSV with N fields
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static string[][] GetFileList(string loc, int fields)
        {
            return !File.Exists(loc) ? new string[0][] : File.ReadAllLines(loc).Select(x => x.Trim()).Where(x => x.Contains(",") && !x.StartsWith(";")).Select(x => x.Split(',', fields).Select(y => y.Trim()).ToArray()).ToArray();
        }

        /// <summary>
        ///     Get a filelist location via DataGame
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="game"></param>
        /// <param name="system"></param>
        /// <returns></returns>
        public static string GetFileListLocation(string filename, DataGame game, string system)
        {
            return GetFileListLocation(filename, game == DataGame.None ? "" : game.ToString(), system);
        }

        /// <summary>
        ///     Get a filelist location via string
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="type"></param>
        /// <param name="system"></param>
        /// <returns></returns>
        public static string GetFileListLocation(string filename, string type, string system)
        {
            if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename)) return Path.GetFullPath(filename);
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), filename ?? $"filelist{(type?.Length == 0 ? "" : $"-{type}")}-{system}.csv");
        }

        /// <summary>
        ///     Get and parse a key/value CSV
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="game"></param>
        /// <param name="system"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetSimpleFileList(string filename, DataGame game, string system)
        {
            var loc = GetFileListLocation(filename, game, system);
            if (!File.Exists(loc)) return new Dictionary<string, string>();
            var csv = GetFileList(loc, 2);
            return csv.ToDictionary(x => x[0], y => y[1]);
        }

        /// <summary>
        ///     Get a simple namespace/key/value CSV
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="game"></param>
        /// <param name="system"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetNamedFileList(string filename, DataGame game, string system)
        {
            var loc = GetFileListLocation(filename, game, system);
            if (!File.Exists(loc)) return new Dictionary<string, string>();
            var csv = GetFileList(loc, 3);
            return csv.ToDictionary(x => x[1], y => y[2]);
        }
    }
}