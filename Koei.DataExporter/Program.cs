﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Cethleann;
using Cethleann.Audio;
using Cethleann.DataTables;
using Cethleann.G1;
using Cethleann.ManagedFS;
using DragonLib.IO;
#if DEBUG
using static Cethleann.Model.Program;

#endif

namespace Koei.DataExporter
{
    [SuppressMessage("ReSharper", "UnusedVariable")]
    public static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: Koei.DataExporter.exe RomFS output [PatchRomFS [DLCRomFS...]]");
                return;
            }

            var romfs = args.First();
            var output = args.ElementAt(1);
            using var cethleann = new Flayn(romfs, GameId.FireEmblemThreeHouses);

            if (args.Length > 3 && Directory.Exists(args.ElementAt(2))) cethleann.AddPatchFS(args.ElementAt(2));

            foreach (var dlcromfs in args.Skip(3)) cethleann.AddDataFS(dlcromfs);
            cethleann.TestDLCSanity();

            cethleann.LoadFileList();

#if DEBUG
            var strings = new StringTable(cethleann.ReadEntry(0x216C).Span);
            var bundle = new DataTable(cethleann.ReadEntry(0xE34).Span);
            var model = new G1Model(bundle.Entries.ElementAt(0).Span);
            var texture = new G1TextureGroup(bundle.Entries.ElementAt(1).Span);
            SaveTextures($@"{output}\mdl\tex", texture);
            SaveModel($@"{output}\mdl\{0xE34:X4}.gltf", model, "tex");
#endif
            ExtractAll(output, cethleann);
        }

        private static void ExtractAll(string romfs, Flayn cethleann)
        {
            if (!Directory.Exists($@"{romfs}\romfs")) Directory.CreateDirectory($@"{romfs}\romfs");
            for (var index = 0; index < cethleann.EntryCount; index++)
            {
                var data = cethleann.ReadEntry(index);
                var dt = data.Span.GetDataType();
                var ext = GetExtension(data.Span);
                var pathBase = $@"{romfs}\romfs\{cethleann.GetFilename(index, ext, dt)}";
                TryExtractBlob(pathBase, data, false, false);
            }
        }

        public static void TryExtractBlobs(string pathBase, List<Memory<byte>> blobs, bool allTypes, bool writeZero)
        {
            for (var index = 0; index < blobs.Count; index++)
            {
                var datablob = blobs[index];
                TryExtractBlob($@"{pathBase}\{index:X4}.{GetExtension(datablob.Span)}", datablob, allTypes, writeZero);
            }
        }

        public static int TryExtractBlob(string blobBase, Memory<byte> datablob, bool allTypes, bool writeZero)
        {
            if (datablob.Length == 0 && !writeZero)
            {
                Console.WriteLine($"{blobBase} is zero!");
                return 0;
            }

            if (allTypes)
            {
                if (!datablob.Span.IsKnown() && datablob.Span.IsDataTable())
                {
                    if (TryExtractDataTable(blobBase, datablob, writeZero))
                        return 1;
                }
                if (datablob.Span.GetDataType() == DataType.SCEN)
                {
                    if (TryExtractSCEN(blobBase, datablob, writeZero))
                        return 1;
                }
                if (datablob.Span.IsBundle())
                {
                    if (TryExtractBundle(blobBase, datablob, writeZero))
                        return 1;
                }
                if (datablob.Span.GetDataType() == DataType.KLDM)
                {
                    if (TryExtractKLDM(blobBase, datablob, writeZero))
                        return 1;
                }
                if (datablob.Span.GetDataType() == DataType.KTSR)
                {
                    if (TryExtractKTSR(blobBase, datablob, writeZero))
                        return 1;
                }
                if (datablob.Span.GetDataType() == DataType.Model)
                {
                    if (TryExtractG1M(blobBase, datablob, writeZero))
                        return 1;
                }
                if (datablob.Span.GetDataType() == DataType.TextLocalization19)
                {
                    if (TryExtractLX(blobBase, datablob, writeZero))
                        return 1;
                }
            }

            Console.WriteLine($@"{blobBase}");

            var basedir = Path.GetDirectoryName(blobBase);
            if (!Directory.Exists(basedir)) Directory.CreateDirectory(basedir);

            File.WriteAllBytes($@"{blobBase}", datablob.ToArray());
            return 2;
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        private static string GetExtension(Span<byte> data)
        {
            var dt = data.GetDataType();
            if (!data.IsKnown() && data.IsDataTable()) return "datatable";
            if (dt == DataType.SCEN) return "scene";
            if (data.IsBundle()) return "bundle";
            if (dt == DataType.KLDM) return "kldm";
            return dt.GetExtension();
        }

        private static bool TryExtractDataTable(string pathBase, Memory<byte> data, bool writeZero)
        {
            try
            {
                var blobs = new DataTable(data.Span);
                if (blobs.Entries.Count == 0) return true;

                TryExtractBlobs(pathBase, blobs.Entries, false, writeZero);
            }
            catch (Exception e)
            {
                Logger.Error("DTBL", $"Failed unpacking DataTable, {e}");
                if (Directory.Exists(pathBase)) Directory.Delete(pathBase, true);

                return false;
            }

            return true;
        }

        private static bool TryExtractSCEN(string pathBase, Memory<byte> data, bool writeZero)
        {
            try
            {
                var blobs = new SCEN(data.Span);
                if (blobs.Entries.Count == 0) return true;

                TryExtractBlobs(pathBase, blobs.Entries, false, writeZero);
            }
            catch (Exception e)
            {
                Logger.Error("SCEN", $"Failed unpacking SCEN, {e}");
                if (Directory.Exists(pathBase)) Directory.Delete(pathBase, true);

                return false;
            }

            return true;
        }

        private static bool TryExtractBundle(string pathBase, Memory<byte> data, bool writeZero)
        {
            try
            {
                var blobs = new Bundle(data.Span);
                if (blobs.Entries.Count == 0) return true;

                TryExtractBlobs(pathBase, blobs.Entries, false, writeZero);
            }
            catch (Exception e)
            {
                Logger.Error("BUN", $"Failed unpacking Bundle, {e}");
                if (Directory.Exists(pathBase)) Directory.Delete(pathBase, true);

                return false;
            }

            return true;
        }

        private static bool TryExtractKLDM(string pathBase, Memory<byte> data, bool writeZero)
        {
            try
            {
                var blobs = new KLDM(data.Span);
                if (blobs.Entries.Count == 0) return true;

                TryExtractBlobs(pathBase, blobs.Entries, false, writeZero);
            }
            catch (Exception e)
            {
                Logger.Error("KLDM", $"Failed unpacking KLDM, {e}");
                if (Directory.Exists(pathBase)) Directory.Delete(pathBase, true);

                return false;
            }

            return true;
        }

        private static bool TryExtractLX(string pathBase, Memory<byte> data, bool writeZero)
        {
            try
            {
                var blobs = new TextLocalization(data.Span);
                if (blobs.Entries.Count == 0) return true;

                var ft = pathBase + ".txt";
                var lines = string.Join(Environment.NewLine, blobs.Entries.SelectMany((x, i) => x.Select((y, j) =>  $"{i},{j} = " + y.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r"))));
                File.WriteAllText(ft, lines);
            }
            catch (Exception e)
            {
                Logger.Error("KLDM", $"Failed unpacking KLDM, {e}");
                if (Directory.Exists(pathBase)) Directory.Delete(pathBase, true);

                return false;
            }

            return true;
        }

        private static bool TryExtractG1M(string pathBase, Memory<byte> data, bool writeZero)
        {
            try
            {
                var blobs = new G1Model(data.Span, true, false);
                if (blobs.SectionRoot.Sections.Count == 0) return true;

                for (var index = 0; index < blobs.SectionRoot.Sections.Count; index++)
                {
                    var sectionData = blobs.SectionRoot.Sections[index];
                    var magic = MemoryMarshal.Read<DataType>(sectionData.Span);
                    TryExtractBlob($@"{pathBase}\{index}.{string.Join("", magic.ToFourCC(true).Reverse())}", sectionData, false, writeZero);
                }
            }
            catch (Exception e)
            {
                Logger.Error("G1M_", $"Failed unpacking G1M, {e}");
                if (Directory.Exists(pathBase)) Directory.Delete(pathBase, true);

                return false;
            }

            return true;
        }

        private static bool TryExtractKTSR(string pathBase, Memory<byte> data, bool writeZero)
        {
            try
            {
                var blobs = new SoundResource(data.Span);
                if (blobs.Entries.Count == 0) return true;

                foreach (var datablob in blobs.Entries)
                {
                    var buffer = datablob switch
                    {
                        SoundResourceSample sample => sample.Data.FullBuffer,
                        SoundUnknown unknown => unknown.Data,
                        _ => Memory<byte>.Empty
                    };
                    TryExtractBlob($@"{pathBase}\{datablob.Base.Id:X8}.{GetExtension(buffer.Span)}", buffer, false, writeZero);
                }
            }
            catch (Exception e)
            {
                Logger.Error("KTSR", $"Failed unpacking KTSR, {e}");
                if (Directory.Exists(pathBase)) Directory.Delete(pathBase, true);

                return false;
            }

            return true;
        }
    }
}