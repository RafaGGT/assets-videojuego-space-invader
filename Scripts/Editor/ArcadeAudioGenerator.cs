using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpaceInvaders.Editor
{
    public static class ArcadeAudioGenerator
    {
        const int Rate = 22050;
        public static void Generate()
        {
            Directory.CreateDirectory("Assets/Audio");
            Write("BGM", 16, (t, i) =>
            {
                int[] melody = { 64, 67, 71, 67, 62, 67, 69, 67, 60, 64, 67, 64, 62, 66, 69, 66,
                    64, 71, 76, 71, 62, 69, 74, 69, 60, 67, 72, 67, 59, 66, 71, 66 };
                double beat = t % 0.5;
                double lead = Square(t, Note(melody[(int)(t / 0.5) % melody.Length])) * Math.Exp(-beat * 9) * 0.18;
                int[] bass = { 40, 38, 36, 35 };
                double low = Math.Sin(2 * Math.PI * Note(bass[(int)(t / 4)]) * t) * 0.22 * Math.Min(1, beat * 80) * Math.Exp(-beat * 4);
                double kick = Math.Sin(2 * Math.PI * (65 * beat + 9 * (1 - Math.Exp(-beat * 35)))) * Math.Exp(-beat * 25) * 0.16;
                return (lead + low + kick) * Math.Min(1, t * 150) * Math.Min(1, (16 - t) * 150);
            });
            Write("Shot", 0.15, (t, i) => SquarePhase(1000 * t - 2100 * t * t) * Math.Exp(-t * 24) * 0.4);
            var random = new System.Random(2317);
            Write("Explosion", 0.32, (t, i) => ((random.NextDouble() * 2 - 1) * 0.65 + Math.Sin(t * 480) * 0.25) * Math.Exp(-t * 13) * 0.55);
            Write("Damage", 0.4, (t, i) => SquarePhase(170 * t - 130 * t * t) * Math.Exp(-t * 8) * 0.4);
            Write("Victory", 1.6, (t, i) => Chord(t, new[] { 64, 67, 71, 76, 79, 83, 88, 88 }));
            Write("Defeat", 1.6, (t, i) => Chord(t, new[] { 64, 62, 60, 59, 55, 52, 47, 40 }));
            AssetDatabase.Refresh();
            foreach (string path in Directory.GetFiles("Assets/Audio", "*.wav"))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }
        static double Note(int midi) => 440 * Math.Pow(2, (midi - 69) / 12.0);
        static double Square(double t, double f) => SquarePhase(t * f);
        static double SquarePhase(double phase) => Math.Sin(2 * Math.PI * phase) >= 0 ? 1 : -1;
        static double Chord(double t, int[] notes) => Square(t, Note(notes[Math.Min(7, (int)(t / 0.2))])) * Math.Exp(-(t % 0.2) * 13) * 0.3;
        static void Write(string name, double seconds, Func<double, int, double> sample)
        {
            int count = (int)(Rate * seconds);
            using (var writer = new BinaryWriter(File.Create($"Assets/Audio/{name}.wav")))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(Rate); writer.Write(Rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                for (int i = 0; i < count; i++)
                {
                    double t = (double)i / Rate;
                    double envelope = Math.Min(1, t * 1000) * Math.Min(1, (seconds - t) * 1000);
                    writer.Write((short)(Math.Clamp(sample(t, i) * envelope, -0.95, 0.95) * short.MaxValue));
                }
            }
        }
    }
}
